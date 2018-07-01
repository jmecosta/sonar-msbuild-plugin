// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.
open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Xml.Linq
open FSharp.Data
open ProjectCheckerTask
open FSharp.Collections.ParallelSeq

type InputXml = XmlProvider<""" 
<AnalysisInput>
  <Settings>
    <Setting>
      <Key>sonar.msbuild.projectchecker.customrules</Key>
      <Value>assemblydata1.dll;assemblydata2.dll;</Value>
    </Setting>
    <Setting>
      <Key>sonar.msbuild.projectchecker.customrules2</Key>
      <Value>assemblydata1.dll;assemblydata2.dll;</Value>
    </Setting>
  </Settings>
  <Rules>
    <Rule>
      <Key>FileLoc</Key>
      <Parameters>
        <Parameter>
          <Key>maximumFileLocThreshold</Key>
          <Value>1500</Value>
        </Parameter>
        <Parameter>
          <Key>maximumFileLocThreshold</Key>
          <Value>1500</Value>
        </Parameter>
      </Parameters>
    </Rule>
    <Rule>
      <Key>SwitchWithoutDefault</Key>
    </Rule>
    <Rule>
      <Key>LineLength</Key>
      <Parameters>
        <Parameter>
          <Key>maximumLineLength</Key>
          <Value>200</Value>
        </Parameter>
      </Parameters>
    </Rule>
  </Rules>
  <Files>
    <File>adsadsda.cs</File>
    <File>asdfsad.xaml.cs</File>
    <File>xcvxcv.cs</File>
  </Files>
</AnalysisInput>
""">

// parse command using regex
// if matched, return (command name, command value) as a tuple
let (|Command|_|) (s:string) =
    let r = new Regex(@"^(?:-{1,2}|\/)(?<command>\w+)[=:]*(?<value>.*)$",RegexOptions.IgnoreCase)
    let m = r.Match(s)
    if m.Success then 
        Some(m.Groups.["command"].Value.ToLower(), m.Groups.["value"].Value)
    else
        None

// take a sequence of argument values
// map them into a (name,value) tuple
// scan the tuple sequence and put command name into all subsequent tuples without name
// discard the initial ("","") tuple
// group tuples by name 
// convert the tuple sequence into a map of (name,value seq)
let parseArgs (args:string seq) =
    args 
    |> Seq.map (fun i -> 
                        match i with
                        | Command (n,v) -> (n,v) // command
                        | _ -> ("",i)            // data
                       )
    |> Seq.scan (fun (sn,_) (n,v) -> if n.Length>0 then (n,v) else (sn,v)) ("","")
    |> Seq.skip 1
    |> Seq.groupBy (fun (n,_) -> n)
    |> Seq.map (fun (n,s) -> (n, s |> Seq.map (fun (_,v) -> v) |> Seq.filter (fun i -> i.Length>0)))
    |> Map.ofSeq

let ShowHelp () =
        Console.WriteLine ("Usage: FsSonarRunner [OPTIONS]")
        Console.WriteLine ("Collects results for Sonar Analsyis using MSBuild Scanner")
        Console.WriteLine ()
        Console.WriteLine ("Options:")
        Console.WriteLine ("    /I|/i:<input xml>")
        Console.WriteLine ("    /F|/f:<analyse single file>")
        Console.WriteLine ("    /U|/u:<username>")
        Console.WriteLine ("    /P|/p:<password>")
        Console.WriteLine ("    /H|/h:<host>")
        Console.WriteLine ("    /O|/o:<output xml file>")
        Console.WriteLine ("    /D|/d:<directory to analyse>")
        Console.WriteLine ("    /displayrules")

[<EntryPoint>]
let main argv = 
    let arguments = parseArgs(argv)
    let mutable failed = false
    
    if arguments.Count = 0 then
        ShowHelp()
    elif arguments.ContainsKey("f") then
        let mutable input = arguments.["f"] |> Seq.head
        let metrics = new SQAnalyser()
        if not(Path.IsPathRooted(input)) then
            input <- Path.Combine(Environment.CurrentDirectory, input)

        if File.Exists(input) then
            try
                metrics.RunAnalysesForOutput(input)
            with
            | ex -> printf "    Failed: %A" ex
        else
            printf "    Failed: File Not Found %A" input
        // todo print to output
    elif arguments.ContainsKey("i") then
        if not(arguments.ContainsKey("o")) then
            Console.WriteLine ("    Mission /O")
            ShowHelp()
        else
            try
                let projectKey =
                    if arguments.ContainsKey("k") then
                        arguments.["k"] |> Seq.head
                    else
                        ""

                let username =
                    if arguments.ContainsKey("u") then
                        arguments.["u"] |> Seq.head
                    else
                        ""
                let password = 
                    if arguments.ContainsKey("p") then
                        arguments.["p"] |> Seq.head
                    else
                        ""

                let hostname =
                    if arguments.ContainsKey("h") then
                        arguments.["h"] |> Seq.head
                    else
                        "http://localhost:9000"

                let input = arguments.["i"] |> Seq.head
                let output = arguments.["o"] |> Seq.head
                let options = InputXml.Parse(File.ReadAllText(input))

                let analyser = new SQAnalyser()
                let basePath = (options.Settings |> Seq.find (fun c -> c.Key.Equals("ProjectRoot"))).Value
                let ingoreFolders = (options.Settings |> Seq.find (fun c -> c.Key.Equals("sonar.msbuild.include.folder.ignores"))).Value
                let rules = options.Settings |> Seq.find (fun c -> c.Key.Equals("sonar.msbuild.projectchecker.customrules"))

                if rules.Value <> "" then
                    for dllPath in rules.Value.Split(';') do
                        printf "    External Analysers: %A\n" dllPath
                        if Path.IsPathRooted(dllPath) then
                            analyser.AddExternalAnalyser(dllPath, hostname, username, password, projectKey)
                        else 
                            analyser.AddExternalAnalyser(Path.Combine(basePath, dllPath), hostname, username, password, projectKey)

                if ingoreFolders <> "" then
                    for folder in ingoreFolders.Split(';') do
                        if Path.IsPathRooted(folder) then
                            analyser.AddIgnoreIncludeFolder(folder)
                        else 
                            analyser.AddIgnoreIncludeFolder(Path.GetFullPath(Path.Combine(basePath, folder)))

                                
                options.Files |> PSeq.iter  (fun c -> if File.Exists(c) then analyser.RunAnalyses(c))
                
                analyser.WriteXmlToDisk(output)
            with
            | ex -> printf "    Failed: %A \r\n %A" ex.Message ex.StackTrace
        ()
    elif arguments.ContainsKey("d") then 
        try
            let directory = arguments.["d"] |> Seq.head

            let csfiles = Directory.GetFiles(directory, "*.csproj", SearchOption.AllDirectories)
            let cppfiles = Directory.GetFiles(directory, "*.vcxproj", SearchOption.AllDirectories)
            let fsfiles = Directory.GetFiles(directory, "*.vcxproj", SearchOption.AllDirectories)

            let metrics = new SQAnalyser()

            csfiles  |> Seq.iter (fun c -> metrics.RunAnalyses(c))
            cppfiles |> Seq.iter (fun c -> metrics.RunAnalyses(c))
            fsfiles  |> Seq.iter (fun c -> metrics.RunAnalyses(c))

            failed <-
                if argv.Length > 1 then
                    metrics.WriteXmlToDisk(argv.[1])
                    false
                else
                    metrics.ReportWarnings()

            
        with
        | ex -> printf "    Failed: %A" ex
    else
        ShowHelp()

    if failed then
        1
    else
        0

