// Learn more about F# at http://fsharp.net

namespace ProjectCheckerTask
#if INTERACTIVE
#r "Microsoft.Build.Framework.dll";;
#r "Microsoft.Build.Utilities.v4.0.dll";;
#endif

open System
open System.Diagnostics
open Microsoft.Build
open Microsoft.Build.Framework
open Microsoft.Build.Logging
open Microsoft.Build.Utilities
open Microsoft.Win32
open Microsoft.FSharp.Collections
open System.Xml.Linq
open FSharp.Data
open System.IO
open VSSolutionUtils
open RuleBase
open ProjectTypes

type ProjectCheckerTask() as this =
    inherit Task()

    let checks : RuleBase list = [  new CannotReadProjectCheck();
                                    new ReferencesCheck();
                                    new ReadabilityIssuesChecks();
                                    new CSharpStaticAnalysersChecks()]

    let checksMsbuild : RuleBase list = [  new IncludeFolderNotUsedDuringInCompilation()]

    let mutable issues : SonarIssue list = List.Empty
    let mutable checksExtenral = List.Empty

    let _log : TaskLoggingHelper = new TaskLoggingHelper(this)
    let getTemporaryDirectory =
        let dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName())
        let info = System.IO.Directory.CreateDirectory(dir)
        info.FullName

    do
        ()

    // input properties
    member val BreakBuild = false with get, set
    member val SourceDir = "" with get, set
    member val ProjectPath = "" with get, set
    member val OutXmlPath = "" with get, set
    member val PreprocessProjectFiles = false with get, set
    member val PathReplacementStrings = "" with get, set
    member val ExcludeFolders = "" with get, set
    member val ExternalDlls : string List = List.Empty  with get, set
    member val IncludeFoldersToIgnore : string List = List.Empty  with get, set
    
    member x.GetChecks() = checks
                    
    member x.CheckBothProjectAndSolutionCannotEmpty =
        lazy(
            if String.IsNullOrWhiteSpace(x.SourceDir) && String.IsNullOrWhiteSpace(x.ProjectPath) then
                _log.LogError("SourceDir Or Project Cannot Be Empty")
            )
                
    member x.CheckBothProjectAndSolutionCannotBeDefinedAtSameTime =
        lazy(
            let mutable validConfig = false
            if not(String.IsNullOrWhiteSpace(x.SourceDir)) && not(String.IsNullOrWhiteSpace(x.ProjectPath)) then
                _log.LogError("SourceDir Cannot Be Empty and Project File Cannot Be specified at the same time")
            )

    member x.VerifySolutionOrProjectExists =
        lazy(

            if not(System.IO.Directory.Exists(x.SourceDir)) then 
                _log.LogError(sprintf "Given Project or SourceDir Has not been found %s" x.SourceDir)
            )


    member x.ExecuteAnalysisOnProjectFile(path : string, solution : string) =
        let mutable isExcluded = false
        if not(String.IsNullOrEmpty(x.ExcludeFolders)) then
            for path in x.ExcludeFolders.Split(';') do
                if not(String.IsNullOrEmpty(path)) then
                    if Path.GetFullPath(solution).ToLower().StartsWith(Path.GetFullPath(path).ToLower()) then
                        isExcluded <- true
             
        if not(isExcluded) then
            try
                let project = ProjType.Parse(File.ReadAllText(path))
                let lines = File.ReadAllLines(path)
                try
                    _log.LogMessage(sprintf "Execute Analysis On Project: %s" path)
                with
                | ex -> ()

                for check in checks do
                    if check.SupportsProject(path) then                    
                        check.ExecuteCheck(project, path, lines, solution, x.OutXmlPath)

                // load and  run third party checks
                for dllPath in x.ExternalDlls do
                    checksExtenral <- checksExtenral @ [MSBuildHelper.LoadChecksFromPath(dllPath)]
                                   
                for externalcheck in checksExtenral do
                    for check in externalcheck do
                        if check.SupportsProject(path) then
                            check.ExecuteCheck(project, path, lines, solution, x.OutXmlPath)
                                                                
                try
                    let msbuildproject = new Microsoft.Build.Evaluation.Project(path)
                    for check in checksMsbuild do
                        if check.SupportsProject(path) then                    
                            check.ExecuteCheckMsbuild(msbuildproject, path, lines, solution, x.OutXmlPath, x.IncludeFoldersToIgnore)

                    for externalcheck in checksExtenral do
                        for check in externalcheck do
                            if check.SupportsProject(path) then
                                check.ExecuteCheckMsbuild(msbuildproject, path, lines, solution, x.OutXmlPath, x.IncludeFoldersToIgnore)

                with
                | ex -> 
                    issues <- issues @ [new SonarIssue(Component = path,
                                                        Line = 1, 
                                                        Message= "Cannot Read Project File : " + ex.Message,
                                                        Rule = "CannotReadProjectError")]                                   
            with
            | ex -> ()


    member x.GetAllIssues() =
        
        for check in checks do
            for issue in check.GetIssues() do
                issues <- issues @ [issue]

        for check in checksMsbuild do
            for issue in check.GetIssues() do
                issues <- issues @ [issue]

        for externalcheck in checksExtenral do
            for check in externalcheck do
                for issue in check.GetIssues() do
                    issues <- issues @ [issue]

        issues


    member x.PrintResults() = 

        for check in checks do
            for issue in check.GetIssues() do
                let error = sprintf "%s : %i : %s : %s \r\n" issue.Component issue.Line issue.Rule issue.Message

                printf "%A"  error

    member x.AnalyseResults(filename : string) = 

        let addLine (line:string) =                  
            use wr = new StreamWriter(filename, true)
            wr.WriteLine(line)

        if not(String.IsNullOrEmpty(x.OutXmlPath)) then
            if not(Directory.Exists(x.OutXmlPath)) then
                Directory.CreateDirectory(x.OutXmlPath) |> ignore

            if File.Exists(filename) then
                File.Delete(filename)

            addLine("""<?xml version="1.0" encoding="UTF-8"?>""")
            addLine("""<results>""")

        for check in checks do
            for issue in check.GetIssues() do
                if not(String.IsNullOrEmpty(x.OutXmlPath)) then
                    let error = sprintf """<error file="%s" line="%i" id="%s" severity="MINOR" msg="%s"/>""" issue.Component issue.Line issue.Rule issue.Message
                    addLine(error)    

                if x.BreakBuild then
                    _log.LogError("", "", "", issue.Component, issue.Line, 0, issue.Line, 0, issue.Message)
                else
                    _log.LogWarning("", "", "", issue.Component, issue.Line, 0, issue.Line, 0, issue.Message)

        if not(String.IsNullOrEmpty(x.OutXmlPath)) then
                addLine("""</results>""")
        
    member x.PreprocessSolutionFilesForAssemblies(solutionFile : string) = 
            let projectUtils = new VSProjectUtils()
            let solUtils = new VSSolutionUtils()

            let outputFile = Path.Combine(x.OutXmlPath, "assemblyLookup.txt")

            let addLine (line:string, filename : string) =                  
                use wr = new StreamWriter(filename, true)
                wr.WriteLine(line)

            let iterateOverFiles (file : string) (projectPath : string) =
                if file.EndsWith("AssemblyInfo.cs") && File.Exists(file) then
                    let lines = File.ReadAllLines(file)

                    let mutable assemblyProduct = ""
                    let mutable assemblyVersion = ""
                    let chars = [|' '; '"'|]
                    for line in lines do
                        if line.Contains("[assembly: AssemblyProduct(") then
                            assemblyProduct <- line.Replace("[assembly: AssemblyProduct(", "").Replace(")]", "").Trim(chars)
                        if line.Contains("[assembly: AssemblyVersion(") then
                            assemblyVersion <- line.Replace("[assembly: AssemblyVersion(", "").Replace(")]", "").Trim(chars)

                    if not(assemblyProduct = "") && not(assemblyVersion = "") then
                        addLine(assemblyProduct + ":" + assemblyVersion, outputFile)
                   
            let iterateOverProjectFiles(projectFile : ProjectFiles) =
                try 
                    projectUtils.GetCompilationFiles(projectFile.path, "", x.PathReplacementStrings)  |> Seq.iter (fun x -> iterateOverFiles x projectFile.path)
                with
                    | ex -> _log.LogWarning(sprintf "Cannot Read %s : %s" projectFile.path ex.Message)

            solUtils.GetProjectFilesFromSolutions(solutionFile) |> Seq.iter (fun x -> iterateOverProjectFiles x)
                
                                    
    override x.Execute() =
        _log.LogMessage(sprintf "=====================================")
        _log.LogMessage(sprintf "ProjectChecker Properties")
        _log.LogMessage(sprintf "SourceDir: %s" x.SourceDir)
        _log.LogMessage(sprintf "ProjectToAnalyse: %s" x.ProjectPath)
        _log.LogMessage(sprintf "=====================================")
        _log.LogMessage(sprintf "")

        if x.PreprocessProjectFiles then
            _log.LogMessage(sprintf "Preprocess Prject File")
            let outputFile = Path.Combine(x.OutXmlPath, "assemblyLookup.txt")
            if File.Exists(outputFile) then
                File.Delete(outputFile)

            let solutions = Directory.GetFiles(x.SourceDir, "*.sln", SearchOption.AllDirectories)
            for solution in solutions do
                x.PreprocessSolutionFilesForAssemblies(solution)
            true
        else
            _log.LogMessage(sprintf "Analyse Solution")
            this.CheckBothProjectAndSolutionCannotEmpty.Force()
            this.CheckBothProjectAndSolutionCannotBeDefinedAtSameTime.Force()
            this.VerifySolutionOrProjectExists.Force()


            let mutable result = not(_log.HasLoggedErrors)
            if result then
                if not(String.IsNullOrEmpty(x.SourceDir)) then
                    let solutionHelper = new VSSolutionUtils()
                    let solutions = Directory.GetFiles(x.SourceDir, "*.sln", SearchOption.AllDirectories)
                    for solution in solutions do
                        _log.LogMessage(sprintf "Execute Analysis On Solution: %s" solution)                    
                        solutionHelper.GetProjectFilesFromSolutions(solution) |> Seq.iter (fun arg -> this.ExecuteAnalysisOnProjectFile(arg.path, solution))
                else
                    let projectFile = new ProjectFiles("user", x.ProjectPath)
                    this.ExecuteAnalysisOnProjectFile(projectFile.path, "")

            this.AnalyseResults(Path.Combine(x.OutXmlPath, "project-checker-results.xml"))

            result

    interface ICancelableTask with
        member this.Cancel() =
            Environment.Exit(0)
            ()