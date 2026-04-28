module MSbuildPackageDiagramCreator 
open System
open System.IO

open Microsoft.Build.Locator

[<EntryPoint>]
let main argv = 
    try
        MSBuildLocator.RegisterDefaults() |> ignore
        let arguments = CommandLine.parseArgs(argv)
        let mutable solutionList = List.Empty
        let executionFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
        let outputFile = 
            if arguments.ContainsKey("o")  then
                (arguments.["o"] |> Seq.head) + ".dgml"
            else
                Path.Combine(Environment.CurrentDirectory, "packages.dgml")

        if arguments.ContainsKey("h")  then
            CommandLine.ShowHelp()
        else
            let toolsVersion =
                if arguments.ContainsKey("v")  then
                    (arguments.["v"] |> Seq.head)
                else
                    "Current"

            let config =
                if arguments.ContainsKey("i") then
                    let input = arguments.["i"] |> Seq.head
                    ProjectTypes.ConfigurationXml.Parse(File.ReadAllText(input))
                else               
                    try
                        ProjectTypes.ConfigurationXml.Parse(File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "msbuildpackagediagramcreator.xml")))
                    with                
                    | _ -> ProjectTypes.ConfigurationXml.Parse(ProjectTypes.DefaultConfigXml)

            if arguments.ContainsKey("s") then
                let file = arguments.["s"] |> Seq.head
                if file.ToLower().EndsWith(".sln") then
                    let solution = MSBuildHelper.PreProcessSolution(config.IgnoreNugetPackages, config.PackageBasePath, file, config.CheckRedundantIncludes, true, toolsVersion)
                    solutionList <- solutionList @ [solution]
                    MSBuildHelper.GenerateHeaderDependencies(solutionList, config.PlotHeaderDependency, config.IgnoreIncludeFolders, config.PlotHeaderDependencFilter, config.PlotHeaderDependencyInsideProject)

                    printfn "Creating %s \n" outputFile
                    DgmlHelper.WriteDgmlSolutionDocument(outputFile, solutionList, config)

                    for warning in Helpers.warnings do
                        printfn "Warning: %s : %s\n" warning.Path warning.Data

            elif arguments.ContainsKey("m") then
                let file = arguments.["m"] |> Seq.head
                let target = arguments.["t"] |> Seq.head

                let targets = MSBuildHelper.CreateTargetTree(file, 
                                                             target,
                                                             config.PackageBasePath,
                                                             config.IgnoreNugetPackages,
                                                             config.PlotHeaderDependency,
                                                             config.IgnoreIncludeFolders,
                                                             config.PlotHeaderDependencFilter,
                                                             config.PlotHeaderDependencyInsideProject,
                                                             toolsVersion,
                                                             null)

                Helpers.warnings |> Seq.iter (fun c-> (printf "%s => %s\n" c.Path c.Data))
                DgmlHelper.WriteDgmlTargetDocument(outputFile, targets, config)

            elif arguments.ContainsKey("d") then
                let directory = arguments.["d"] |> Seq.head

                for file in  Directory.EnumerateFiles(directory, "*.sln", SearchOption.AllDirectories) do
                    let solution = MSBuildHelper.PreProcessSolution(config.IgnoreNugetPackages, config.PackageBasePath, file, config.CheckRedundantIncludes, true, toolsVersion)
                    solutionList <- solutionList @ [solution]

                if not(solutionList.IsEmpty) then
                    MSBuildHelper.GenerateHeaderDependencies(solutionList, config.PlotHeaderDependency, config.IgnoreIncludeFolders, config.PlotHeaderDependencFilter, config.PlotHeaderDependencyInsideProject)

                if config.ExportSolutionInfoToCsv then
                    let csvfile = Path.Combine(Environment.CurrentDirectory, "solutioninfo.csv")
                    CsvHelper.WriteSolutionInfoToCsv(csvfile, solutionList)
                else 
                    DgmlHelper.WriteDgmlSolutionDocument(outputFile, solutionList, config)
            else
                CommandLine.ShowHelp()

        0
    with
    | ex ->
        eprintfn "MSbuildPackageDiagramCreator failed: %s" ex.Message
        eprintfn "Stack trace: %s" ex.StackTrace
        if ex.InnerException <> null then
            eprintfn "Inner exception: %s" ex.InnerException.Message
        1