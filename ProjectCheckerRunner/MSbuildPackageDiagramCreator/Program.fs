open System
open System.IO
open System.Linq
open System.Text
open System.Text.RegularExpressions

open Microsoft.Build
open Microsoft.Build.Evaluation
open Microsoft.Build.BuildEngine

[<EntryPoint>]
let main argv = 
    let arguments = CommandLine.parseArgs(argv)
    let mutable solutionList = List.Empty
    let executionFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", ""))
    let outputFile = 
        if arguments.ContainsKey("o")  then
            (arguments.["o"] |> Seq.head) + ".dgml"            
        else
            Path.Combine(Environment.CurrentDirectory, "packages.dgml")


    if arguments.ContainsKey("h")  then
        CommandLine.ShowHelp()
    else        
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
                let solution = MSBuildHelper.PreProcessSolution(config.IgnoreNugetPackages, config.PackageBasePath, file, config.CheckRedundantIncludes, true)
                solutionList <- solutionList @ [solution]
                MSBuildHelper.GenerateHeaderDependencies(solutionList, config.PlotHeaderDependency, config.IgnoreIncludeFolders, config.PlotHeaderDependencFilter, config.PlotHeaderDependencyInsideProject)

                printfn "Creating %s \n" outputFile
                DgmlHelper.WriteDgmlSolutionDocument(outputFile, solutionList, config)

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
                                                         config.PlotHeaderDependencyInsideProject)

            Helpers.warnings |> Seq.iter (fun c-> (printf "%s => %s\n" c.Path c.Data))
            DgmlHelper.WriteDgmlTargetDocument(outputFile, targets, config)

                                     
        elif arguments.ContainsKey("d") then
            let supportedExtensions = Set.ofList [".sln"]
            let directory = arguments.["d"] |> Seq.head
            for file in  Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories) do
                let extension = Path.GetExtension(file).ToLower()
                if supportedExtensions.Contains(extension) then 
                    let includeSolutionsSet = (config.PlotSolutionNodeFilter.Split([|';'; '\n'; ' '|], StringSplitOptions.RemoveEmptyEntries) |> Set.ofSeq)

                    if includeSolutionsSet.IsEmpty then
                        let solution = MSBuildHelper.PreProcessSolution(config.IgnoreNugetPackages, config.PackageBasePath, file, config.CheckRedundantIncludes, true)
                        if not(includeSolutionsSet.Contains(solution.Name)) then
                            solutionList <- solutionList @ [solution]
                            MSBuildHelper.GenerateHeaderDependencies(solutionList, config.PlotHeaderDependency, config.IgnoreIncludeFolders, config.PlotHeaderDependencFilter, config.PlotHeaderDependencyInsideProject)

            DgmlHelper.WriteDgmlSolutionDocument(outputFile, solutionList, config)
        else
            CommandLine.ShowHelp()

    0 // return an integer exit code

