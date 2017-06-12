module MSBuildHelper

open System
open System.IO

open System.Text
open System.Text.RegularExpressions
open Microsoft.Build.Execution
open Microsoft.Build.Evaluation

open System.Reflection
open RuleBase

// Match starts with
let (|Prefix|_|) (p:string) (s:string) =
    if s.StartsWith(p) then
        Some(s.Substring(p.Length))
    else
        None

let toMap dictionary = 
    (dictionary :> seq<_>)
    |> Seq.map (|KeyValue|)
    |> Map.ofSeq

let LoadChecksFromPath(path : string) =
    let mutable checks : RuleBase List = List.Empty
    let assembly = Assembly.LoadFrom(path)
    let types = assembly.GetExportedTypes()
    for typedata in types do
        try
            let data =  (Activator.CreateInstance(typedata) :?> RuleBase)
            checks <- checks @ [data]
        with
        | ex -> ()

    checks

let mutable BuildScripts = Map.empty
let mutable projectData = List.Empty
let CreateSolutionData(solution : string) =
    let solutionData = new ProjectTypes.Solution()
    solutionData.Name <- "SLN:" + Path.GetFileNameWithoutExtension(solution)
    solutionData.Path <- solution
    let content = File.ReadAllLines(solution)

    projectData <- List.Empty
    let handleProject(data : string List, project : ProjectTypes.Project) = 
        
        for line in data do
            
            if line.Contains("Project(") then
                let elements = data.[0].Split(',')
                let guidRaw = elements.[2].Replace("\"", "")
                let pathRaw = elements.[1].Replace("\"", "")
                let rawName = elements.[0].Split('=').[1].Trim()

                project.Name <- rawName.Replace("\"", "")
                project.Path <- Path.Combine(Path.GetDirectoryName(solution), pathRaw.Trim())
                project.Guid <- new Guid(guidRaw)
                                
                if solutionData.Projects.ContainsKey(project.Guid) then
                    let element = solutionData.Projects.[project.Guid]
                    element.Name <- project.Name
                    element.Guid <- project.Guid
                    element.Path <- project.Path
                else
                    solutionData.Projects.Add(project.Guid, project)
            else
                try
                    if line.Contains("=") && not(line.Contains("postProject")) then
                        let projeRef = new ProjectTypes.Project()
                        let rawid = line.Trim().Split('=').[0].Trim()
                        let id = rawid.Replace("\"", "")

                        if not(solutionData.Projects.ContainsKey(new Guid(id))) then
                            let projectRef = new ProjectTypes.Project()
                            projectRef.Guid <- new Guid(id)
                            if not(projectRef.Name = "") then
                                solutionData.Projects.Add(projectRef.Guid, projectRef)
                                projectRef.Visible <- true
                                project.Visible <- true
                                project.SolutionInternalBuildDepencies.Add(projectRef.Guid, projectRef)
                        else
                            let projectRef= solutionData.Projects.[new Guid(id)]
                            projectRef.Visible <- true
                            project.Visible <- true
                            project.SolutionInternalBuildDepencies.Add(projectRef.Guid, projectRef)
                        
                    
                with
                | ex -> ()

    let handleLine (line : string) =
        if line.TrimStart().StartsWith("Project(") then
            projectData <- List.Empty
            projectData <- projectData @ [line]

        elif line.TrimStart().StartsWith("EndProject") then
            projectData <- projectData @ [line]
            handleProject(projectData, new ProjectTypes.Project())
        else
            projectData <- projectData @ [line]
                            
    content |> Seq.iter (fun c -> handleLine(c))

    solutionData




let PopulateLinkLibsDepedencies(item : ProjectItemDefinition, project : ProjectTypes.Project, packagesBase : string) = 

    try
        let systemLibs = ["ole32.lib";"user32.lib";"comdlg32.lib"; "advapi32.lib"; 
                          "uuid.lib";"OldNames.lib";"shell32.lib"; "oleaut32.lib";
                          "ws2_32.lib";"Advapi32.lib";"odbc32.lib"; "odbccp32.lib"; "msvcrt.lib";
                          "wsock32.lib";"kernel32.lib"; "Iphlpapi.lib"; "Rpcrt4.lib"; "dbghelp.lib"; "msvcprt.lib";"psapi.lib";
                          "gdi32.lib"; "winspool.lib" ; "version.lib"; "delayimp.lib"; "Shlwapi.lib"; "comsuppw.lib"; "mscoree.lib"; "msvcmrt.lib"; "rpcns4.lib"]

        let AddDepFile(c:string) =
            if not((systemLibs |> Seq.tryFind (fun lib -> lib.ToLower().Equals(c.ToLower()))).IsSome) && not(c = "") then
                if File.Exists(c) then
                    let searchPattern = Path.GetFileName(c);
                    let basePath = Path.GetDirectoryName(c);

                    if basePath <> "" then
                        for file in Directory.EnumerateFiles(basePath, searchPattern) do
                            if not(project.DependentLibs.Contains(file)) then
                                project.DependentLibs.Add(file) |> ignore
                    else
                        if not(project.DependentLibs.Contains(c)) then
                            project.DependentLibs.Add(c) |> ignore
                else 
                    if not(project.DependentLibs.Contains(c)) then
                        project.DependentLibs.Add(c) |> ignore

        let AddLibIncludeDir(c:string) =
            let path = 
                if Path.IsPathRooted(c) then
                    c
                else
                    Path.GetFullPath(Path.Combine(Directory.GetParent(project.Path).ToString(), c))

            if not(project.DependentLibDirectories.Contains(path)) then
                project.DependentLibDirectories.Add(path) |> ignore

        item.Metadata
            |> List.ofSeq
            |> Array.ofList
            |> Array.Parallel.iter (fun item ->
                if (item.Name.Equals("AdditionalLibraryDirectories")) then
                    item.EvaluatedValue.Split(';') |> Seq.iter (fun c -> AddLibIncludeDir(c))
                if (item.Name.Equals("AdditionalDependencies")) then
                    item.EvaluatedValue.Split(';') |> Seq.iter (fun c -> AddDepFile(c.Trim()))
            )
    with
    | ex -> raise ex

// create builds information for project, include paths and additional options
// it also follows includes to find cyclic dependencies
let GenerateBuildCppBuildInformation(additionalIncludeDirectories : byref<Set<string>>,
                                     includes : byref<Set<string>>, 
                                     item : ProjectItem,
                                     projectToProcess : ProjectTypes.Project,
                                     packagesBase : string,
                                     detectCycles : bool) = 

    let mutable compileCommand = "cl.exe "
    let mutable compileCommandClang = "clang++ "
    let mutable compileCommandGcc = "gcc "
    let projectPath = Path.GetFullPath(Path.GetDirectoryName(projectToProcess.Path)).ToString()
    let pathEvaluateInclude = 
        if Path.IsPathRooted(item.EvaluatedInclude) then
            item.EvaluatedInclude
        else
            Path.Combine(projectPath, item.EvaluatedInclude)

    // add project path to include folders
    if not(projectToProcess.AdditionalIncludeDirectories.Contains(projectPath)) then
        projectToProcess.AdditionalIncludeDirectories.Add(projectPath) |> ignore
    if not(additionalIncludeDirectories.Contains(projectPath)) then
        additionalIncludeDirectories <- additionalIncludeDirectories.Add(projectPath)

    // process additional include dirs
    let metadataelemsdirs = Seq.toList item.Metadata |> List.tryFind (fun c -> c.Name.Equals("AdditionalIncludeDirectories"))

    match metadataelemsdirs with
    | Some value -> 
        let includeDirs = value.EvaluatedValue.Split([|';'; '\n'; '\r'; '\t'|], StringSplitOptions.RemoveEmptyEntries)
        for dir in includeDirs do
            let path = dir.Trim().ToLower().Replace("\\", "/")
            let fullPath = 
                if Path.IsPathRooted(path) then
                    let data = Path.GetFullPath(path)
                    compileCommand <- compileCommand + " -I" + data.Replace("\\", "/") + " "
                    compileCommandClang <- compileCommandClang + " -I" + data.Replace("\\", "/") + " "
                    compileCommandGcc <- compileCommandGcc + " -I" + data.Replace("\\", "/") + " "
                    data
                else
                    let basePath = Directory.GetParent(projectToProcess.Path).ToString()
                    let pathData = Path.GetFullPath(Path.Combine(basePath, path))
                    compileCommand <- compileCommand + "-I" + pathData.Replace("\\", "/") + " "
                    compileCommandClang <- compileCommandClang + "-I" + pathData.Replace("\\", "/") + " "
                    compileCommandGcc <- compileCommandGcc + "-I" + pathData.Replace("\\", "/") + " "
                    pathData

            if not(path.Equals("")) && (String.IsNullOrEmpty(packagesBase) || not(path.Contains(packagesBase))) && not(additionalIncludeDirectories.Contains(fullPath)) then
                if Path.IsPathRooted(path) then
                    additionalIncludeDirectories <- additionalIncludeDirectories.Add(fullPath)
                else
                    additionalIncludeDirectories <- additionalIncludeDirectories.Add(fullPath)

                if not(projectToProcess.AdditionalIncludeDirectories.Contains(fullPath)) then
                    projectToProcess.AdditionalIncludeDirectories.Add(fullPath) |> ignore
    | _ -> ()

    // detect cyclic headers
    if detectCycles then
        let dirs = (Seq.toList additionalIncludeDirectories)
        for fileinclude in Helpers.GetIncludePathsForFile(pathEvaluateInclude, dirs, projectToProcess.Path) do
            if not(includes.Contains(fileinclude)) then
                includes <- includes.Add(fileinclude)

    // retrieve additional options for compilation
    let metadataelemsoptions = Seq.toList item.Metadata |> List.tryFind (fun c -> c.Name.Equals("AdditionalOptions"))

    match metadataelemsoptions with
    | Some value -> 
        let additionalOptions = value.EvaluatedValue.Split([|';'; '\n'; ' '; '\r'; '\t'|], StringSplitOptions.RemoveEmptyEntries)
        for trimmedData in additionalOptions do
            let option = trimmedData.Trim()
            compileCommand <- compileCommand + option + " "

            if not(option.Equals("")) && not(projectToProcess.AdditionalOptions.Contains(option)) then
                projectToProcess.AdditionalOptions.Add(option) |> ignore
    | _ -> ()

    // retrive defines
    let metadataelemsdef = Seq.toList item.Metadata |> List.tryFind (fun c -> c.Name.Equals("PreprocessorDefinitions"))

    match metadataelemsdef with
    | Some value -> 
        let defines = value.EvaluatedValue.Split([|';'; '\n'; '\r'; '\t'|], StringSplitOptions.RemoveEmptyEntries)
        for define in defines do
            if define.Contains(" ") then 
                let defineAndValue = define.Split('=')
                compileCommandClang <- compileCommandClang + " -D" + defineAndValue.[0] + "=\"" + defineAndValue.[1] + "\""
                compileCommandGcc <- compileCommandGcc + " -D" + defineAndValue.[0] + "=\"" + defineAndValue.[1] + "\""
                compileCommand <- compileCommand + " /D \"" + define + "\" "
            else
                compileCommandClang <- compileCommandClang + " -D" + define + " "
                compileCommandGcc <- compileCommandGcc + " -D" + define + " "
                compileCommand <- compileCommand + " /D " + define + " "

            if not(projectToProcess.Defines.Contains(define)) then
                projectToProcess.Defines.Add(define) |> ignore
    | _ -> ()

    // create compilation unit
    let unit = new ProjectTypes.CompileUnit()
    unit.File <- item.EvaluatedInclude
    unit.Directory <- item.Project.DirectoryPath
    unit.VcCommand <- compileCommand  + unit.File
    unit.ClangCommand <- compileCommandClang  + unit.File
    unit.GccCommand <- compileCommandGcc + unit.File

    projectToProcess.CompileUnits.Add(unit)

let PopulateProjectReferences(item : ProjectItem, project : ProjectTypes.Project, solution : ProjectTypes.Solution) =
    if item.ItemType.Equals("ProjectReference") then
        let path = 
            if Path.IsPathRooted(item.EvaluatedInclude) then
                item.EvaluatedInclude
            else
                Path.Combine(Path.GetDirectoryName(project.Path), item.EvaluatedInclude)

        let metadataelems = Seq.toList item.Metadata 
                        
        let guidmatch = metadataelems |> List.tryFind (fun c -> c.Name.Equals("Project"))
        match guidmatch with
        | Some value -> 
            let guid = value.EvaluatedValue
            let projectRef = 
                if solution.Projects.ContainsKey(new Guid(guid)) then
                    solution.Projects.[new Guid(guid)]
                else
                    let projectRef = new ProjectTypes.Project()
                    projectRef.Guid <- new Guid(guid)
                    projectRef.Path <- path
                                    
                    printfn "Invalid project reference to external project %s -> %s" project.Path path
                    projectRef

            if not(project.ProjectReferences.ContainsKey(projectRef.Guid)) then
                project.ProjectReferences.Add(projectRef.Guid, projectRef)
                project.Visible <- true
            else
                let data = sprintf "Project contains same reference multiple times: %A %A" projectRef.Guid projectRef
                Helpers.AddWarning(project.Path, data)
        | _ -> ()

let CheckAdditionalIncludeDirectories(additionalIncludeDirectories : Set<string>, includes : Set<string>, project : ProjectTypes.Project) =

    let VerifyPathExists(c, m) =
        try
            File.Exists(Path.Combine(c, m))
        with
        | ex -> false
    
    let CheckFileExists(c) = 
        let found = includes |> Seq.tryFind(fun m ->  VerifyPathExists(c, m))
        match found with
        | Some value -> true
        | _ -> false

    additionalIncludeDirectories
        |> Seq.iter
            (fun c -> 
                if not(CheckFileExists(c)) then
                    Helpers.AddWarning(project.Path, (sprintf "Additional Include Path not In Use: %s" c))
                    )

let mutable additionalIncludeDirectories : Set<string> = Set.empty
let mutable includes : Set<string> = Set.empty

let HandleCppProjecItems(projectEvaluated : Project,
                         project : ProjectTypes.Project,
                         solution : ProjectTypes.Solution,
                         packagesBase : string,
                         processIncludes : bool,
                         detectHeaderCycles : bool) = 
    additionalIncludeDirectories <- Set.empty
    includes <- Set.empty

    projectEvaluated.Items
        |> List.ofSeq
        |> Array.ofList
        |> Array.iter (fun item ->
            if processIncludes && item.ItemType.Equals("ClCompile") then
                GenerateBuildCppBuildInformation(&additionalIncludeDirectories, &includes, item, project, packagesBase, detectHeaderCycles)
            PopulateProjectReferences(item, project, solution))

    projectEvaluated.ItemDefinitions
        |> List.ofSeq
        |> Array.ofList
        |> Array.Parallel.iter (fun item ->
            if (item.Key.Equals("Link")) then
                PopulateLinkLibsDepedencies(item.Value, project, packagesBase))

    CheckAdditionalIncludeDirectories(additionalIncludeDirectories, includes, project)

// collects data from msbuild
// does some static checking in data
let PreProcessSolution(nugetIgnorePackages : string,
                       packagesBasePath : string,
                       solutionPath : string,
                       processIncludes : bool,
                       detectHeaderCycles : bool,
                       toolsVersion : string) =

    let ignoredPackages = (nugetIgnorePackages.Split([|';'; '\n'; ' '|], StringSplitOptions.RemoveEmptyEntries) |> Set.ofSeq)

    let solution = CreateSolutionData(solutionPath)

    for project in solution.Projects do
        let extension = Path.GetExtension(project.Value.Path).ToLower()

        try
            let msbuildproject =
                let projects = ProjectCollection.GlobalProjectCollection.GetLoadedProjects(project.Value.Path)
    
                if projects.Count <> 0 then
                    let data = projects.GetEnumerator();
                    data.MoveNext() |> ignore
                    data.Current
                else
                    if toolsVersion.StartsWith("14.0") || toolsVersion.StartsWith("15.0") then
                        new Microsoft.Build.Evaluation.Project(project.Value.Path, null, "14.0")
                    elif toolsVersion.StartsWith("12.0") then
                        new Microsoft.Build.Evaluation.Project(project.Value.Path, null, "12.0")
                    else
                        new Microsoft.Build.Evaluation.Project(project.Value.Path, null, "4.0")

            if extension = ".csproj" then
                let outputType = match (msbuildproject.Properties |> Seq.tryFind (fun c -> c.Name.Equals("OutputType"))) with | Some value -> value.EvaluatedValue | _ -> ""
                let name = match (msbuildproject.Properties |> Seq.tryFind (fun c -> c.Name.Equals("AssemblyName"))) with | Some value -> value.EvaluatedValue | _ -> ""

                if outputType.ToLower() = "library" then
                    project.Value.OutputPath <- name + ".dll"
                else
                    project.Value.OutputPath <- name + ".exe"
                
                for evaluatedType in msbuildproject.AllEvaluatedItems do 
                    if evaluatedType.ItemType = "Reference" then
                        let hintPath = match (evaluatedType.DirectMetadata |> Seq.tryFind (fun c -> c.Name.Equals("HintPath"))) with | Some value -> value.EvaluatedValue | _ -> ""
                        if hintPath <> "" && not(hintPath.StartsWith(packagesBasePath)) then
                            project.Value.DllReferences.Add(hintPath) |> ignore
                        ()


                project.Value.Type <- "cs"

            if extension = ".vcxproj" then
                printf "Handle %A \n" project.Value.Path
                project.Value.Type <- "cpp"

                project.Value.ImportLib <- match (msbuildproject.AllEvaluatedItemDefinitionMetadata |> Seq.tryFind (fun c -> c.Name.Equals("ImportLibrary"))) with | Some value -> value.EvaluatedValue | _ -> ""

                project.Value.Keyword <- match (msbuildproject.Properties |> Seq.tryFind (fun c -> c.Name.Equals("Keyword"))) with | Some value -> value.EvaluatedValue | _ -> ""
                project.Value.ConfigurationType <- match (msbuildproject.Properties |> Seq.tryFind (fun c -> c.Name.Equals("ConfigurationType"))) with | Some value -> value.EvaluatedValue | _ -> ""
                project.Value.CLRSupport <- match (msbuildproject.Properties |> Seq.tryFind (fun c -> c.Name.Equals("CLRSupport"))) with | Some value -> value.EvaluatedValue | _ -> ""

                if project.Value.CLRSupport <> "" then
                    for evaluatedType in msbuildproject.AllEvaluatedItems do 
                        if evaluatedType.ItemType = "Reference" then
                            let hintPath = match (evaluatedType.DirectMetadata |> Seq.tryFind (fun c -> c.Name.Equals("HintPath"))) with | Some value -> value.EvaluatedValue | _ -> ""
                            if hintPath <> "" && not(hintPath.StartsWith(packagesBasePath)) then
                                project.Value.DllReferences.Add(hintPath) |> ignore

                project.Value.TargetPath <- match (msbuildproject.Properties |> Seq.tryFind (fun c -> c.Name.Equals("TargetPath"))) with | Some value -> value.EvaluatedValue | _ -> ""

                project.Value.PlatformToolset <- match (msbuildproject.Properties |> Seq.tryFind (fun c -> c.Name.Equals("PlatformToolset"))) with | Some value -> value.EvaluatedValue | _ -> "v120"
                project.Value.Platform <- match (msbuildproject.Properties |> Seq.tryFind (fun c -> c.Name.Equals("Platform"))) with | Some value -> value.EvaluatedValue | _ -> "Win32"

                let VC_IncludePath = match (msbuildproject.Properties |> Seq.tryFind (fun c -> c.Name.Equals("VC_IncludePath"))) with | Some value -> value.EvaluatedValue | _ -> ""
                let WindowsSDK_IncludePath = match (msbuildproject.Properties |> Seq.tryFind (fun c -> c.Name.Equals("WindowsSDK_IncludePath"))) with | Some value -> value.EvaluatedValue | _ -> ""

                // figure out a way of getting those
                // WindowsSDK_IncludePath
                // VC_IncludePath

                for includeData in VC_IncludePath.Split([|';'; '\n'; '\r'; '\t'|], StringSplitOptions.RemoveEmptyEntries) do
                    if not(project.Value.SystemIncludeDirs.Contains(includeData)) then
                        project.Value.SystemIncludeDirs.Add(includeData) |> ignore

                for includeData in WindowsSDK_IncludePath.Split([|';'; '\n'; '\r'; '\t'|], StringSplitOptions.RemoveEmptyEntries) do
                    if not(project.Value.SystemIncludeDirs.Contains(includeData)) then
                        project.Value.SystemIncludeDirs.Add(includeData) |> ignore

                if not(Path.GetFileNameWithoutExtension(project.Value.Path).ToLower().Equals(project.Value.Name.ToLower())) then
                    Helpers.AddWarning(project.Value.Path, "Project Path Does Not Match Output Path")
                //    raise(ProjectTypes.IncorrectNameForProject("Post Project Incorrectly Named"))

                let props = msbuildproject.AllEvaluatedProperties
                for importProj in msbuildproject.Imports do
                    let importProj = importProj.ImportedProject.FullPath.ToLower().Replace("\\", "/")
                    if importProj.StartsWith(packagesBasePath) then
                        try
                            let packageId = importProj.Replace(packagesBasePath + "/", "").Split('/').[0]
                            let isFound = ignoredPackages |> Seq.tryFind (fun c -> packageId.ToLower().Contains(c.ToLower()))
                            match isFound with
                            | Some c -> ()
                            | _ ->
                                if not(project.Value.NugetReferences.Contains(packageId)) then
                                        
                                    project.Value.NugetReferences <- project.Value.NugetReferences.Add(packageId)
                                    project.Value.Visible <- true
                        with
                        | ex -> ()

                HandleCppProjecItems(msbuildproject, project.Value, solution, packagesBasePath, processIncludes, detectHeaderCycles)

                ProjectCollection.GlobalProjectCollection.UnloadProject(msbuildproject)

        with
        | ex ->  printf "Failed to create node: %s %s %s\n" project.Value.Path ex.Message  ex.StackTrace

    solution


let mutable childSolutionsFound = List.empty
// preporcess targets in msbuild files
let rec HandleTarget(projectInstance : ProjectTargetInstance,
                     msbuildproject : Microsoft.Build.Evaluation.Project,
                     target : string,
                     targets : byref<ProjectTypes.MsbuildTarget List>,
                     nugetPackageBase : string,
                     nugetIgnorePackages : string,
                     processIncludes : bool,
                     toolsVersion : string) =
    
    let targetdata = msbuildproject.Targets.[target]
    let newTarget = new ProjectTypes.MsbuildTarget()
    childSolutionsFound <- List.empty
    newTarget.Name <- "MSB:" + target

    printf "Handle MSBuild TARGET : %A\n" newTarget.Name

    let ProcessProjectForDeps(projectSolution:string) = 
        if projectSolution.ToLower().EndsWith(".sln") then
            printf "Handle Solution : %A\n" projectSolution
            let solutionData = PreProcessSolution(nugetIgnorePackages, nugetPackageBase, projectSolution, processIncludes, true, toolsVersion)
            if not(newTarget.Children.ContainsKey(solutionData.Name)) then
                childSolutionsFound <- childSolutionsFound @ [solutionData]
                newTarget.Children <- newTarget.Children.Add(solutionData.Name, solutionData)

    let processXmlElements(topLevel:Xml.Linq.XElement, solution:string) = 
        let ProcessElement(elem:Xml.Linq.XElement) = 
            let ProcessAttribute(attrib:Xml.Linq.XAttribute) = 
                if attrib.Name.LocalName.ToLower().Equals("include") then
                    let mutable solDataM = null
                    let GetSolutionDataFromEvalData(mmm:string) = 
                        if Path.IsPathRooted(mmm) then
                            ProcessProjectForDeps(mmm)
                        else
                            let value = (msbuildproject.GetPropertyValue(mmm))
                            if value <> "" then
                                ProcessProjectForDeps(value)

                    let dataValues = attrib.Value.Split([|';'; '\n'; ' '; '\r'; '$'; '('; ')'|], StringSplitOptions.RemoveEmptyEntries)
                    dataValues |> Seq.iter (fun c -> GetSolutionDataFromEvalData(c))

            let localName = elem.Name.LocalName
            if localName.Equals(solution) then
                elem.Attributes() |> Seq.iter (fun x -> ProcessAttribute(x))

        topLevel.Elements() |> Seq.iter (fun elem -> ProcessElement(elem))

    let ProcessSolution(solution:string, children:ProjectTaskInstance) = 
        let value = (msbuildproject.GetPropertyValue(solution))

        if value <> "" then
            ProcessProjectForDeps(value)
        else
            // not able to evaluate data, search in file for property
            let xmlData = 
                if not(BuildScripts.ContainsKey(children.FullPath)) then
                    BuildScripts <- BuildScripts.Add(children.FullPath, ProjectTypes.ProjType.Parse(File.ReadAllText(children.FullPath)))

                BuildScripts.[children.FullPath]

            let targetSubData = xmlData.Targets |> Seq.tryFind(fun x -> x.Name.Equals(target))
            match targetSubData with
            | Some target -> target.ItemGroups |> Seq.iter (fun x -> processXmlElements(x.XElement, solution))
            | _ -> ()


    let ProcessChild(children:ProjectTaskInstance) = 
        let msbuildProjects = children.Parameters.["Projects"]
        let elements = msbuildProjects.Split([|';'; '\n'; ' '; '\r'; '$'; '('; ')'; '@'; '%'|], StringSplitOptions.RemoveEmptyEntries)
        elements
            |> Array.ofSeq
            |> Array.iter (fun solutionName -> ProcessSolution(solutionName, children))

    // handle children inside target
    projectInstance.Children
        |> Array.ofSeq
        |> Array.iter (fun c-> 
            try
                let children = c :?> ProjectTaskInstance
                if children.Name.Equals("MSBuild") then ProcessChild children
            with | ex -> ())

    // handle dependencies
    for dep in projectInstance.DependsOnTargets.Split([|';'; '\n'; ' '; '\r'; '\t'|], StringSplitOptions.RemoveEmptyEntries) do
        if dep <> "" then
            let data = msbuildproject.Targets.[dep]
            let name, depTarget = HandleTarget(data, msbuildproject, dep, &targets, nugetPackageBase, nugetIgnorePackages, processIncludes, toolsVersion)
            newTarget.MsbuildTargetDependencies <- newTarget.MsbuildTargetDependencies.Add(name, depTarget)
    
    targets <- targets @ [newTarget]
    newTarget.Name, newTarget 

let FindDependencyInSolution(directory : string, solution : ProjectTypes.Solution) =
    
    let CheckArrayElement(c) = 
        let (a : Guid, b : ProjectTypes.Project) = c
        let directoryOfProject = Path.GetFullPath(Path.GetDirectoryName(b.Path.ToLower())).Replace("\\", "/")
        directory.Contains(directoryOfProject) 

    let mutable projectToAdd : ProjectTypes.Project = null
    Map.toArray (toMap solution.Projects) |> Array.tryFind (fun c -> (CheckArrayElement(c)))
    

let GenerateHeaderDependencies(solutionList : ProjectTypes.Solution List,
                               plotHeaderDependency : bool,
                               ignoreIncludeFolders : string,
                               plotHeaderDependencFilter : string,
                               plotHeaderDependencyInsideProject : bool) =

    if plotHeaderDependency then
        let ignoreSet = (ignoreIncludeFolders.Split([|';'; '\n'; ' '|], StringSplitOptions.RemoveEmptyEntries) |> Set.ofSeq)
        let includeSolutionsSet = (plotHeaderDependencFilter.Split([|';'; '\n'; ' '|], StringSplitOptions.RemoveEmptyEntries) |> Set.ofSeq)
        for solution in solutionList do
            if includeSolutionsSet.IsEmpty || includeSolutionsSet.Contains(solution.Name) then
                for project in solution.Projects do
                    for directory in project.Value.AdditionalIncludeDirectories do
                        let directoryAbs = Path.GetFullPath(directory).ToLower().Replace("\\", "/")

                        if not(ignoreSet.Contains(directoryAbs)) then
                            let projectOption = FindDependencyInSolution(directoryAbs, solution)

                            match projectOption with
                            | Some (guid, projectFound) ->  
                                    if not(guid.Equals(project.Value.Guid)) && plotHeaderDependencyInsideProject then
                                        if not(project.Value.HeaderReferences.ContainsKey(guid)) then
                                            project.Value.HeaderReferences.Add(guid, projectFound)
                                            project.Value.Visible <- true
                            | _  -> 
                                    try
                                        for solution2 in solutionList do
                                            if not(solution2.Name.Equals(solution.Name)) then
                                                let projectOption = FindDependencyInSolution(directoryAbs, solution2)
                                                match projectOption with
                                                | Some (guid, projectFound) ->  
                                                        if not(guid.Equals(project.Value.Guid)) then
                                                            project.Value.HeaderReferences.Add(guid, projectFound)
                                                            project.Value.Visible <- true
                                                            raise(ProjectTypes.FoundElementException("found"))
                                                | _  -> ()
                                    with
                                    | ex -> ()



let mutable solutionsThatGenerateLib = Map.empty
let mutable listOfLibsLink : string Set = Set.empty
let mutable listOfDependentReferences : string Set = Set.empty
let mutable allowedSearchFolders : string Set = Set.empty

let GenerateExternalBuildDependenciesForSolutions(targets : ProjectTypes.MsbuildTarget List) = 


    let GenerateExternalDepForSolution(solutionIn : ProjectTypes.Solution, targets : ProjectTypes.MsbuildTarget List) = 

        let GetPotencialProjectsThatGenerateReference(lib:string) = 
            let datalib = 
                if Path.IsPathRooted(lib) then
                    Path.GetFileName(lib).ToLower()
                else
                    lib.ToLower()
            solutionsThatGenerateLib <- Map.empty

            targets
                |> Array.ofSeq
                |> Array.iter
                    (fun target ->
                        target.Children
                            |> Array.ofSeq
                            |> Array.iter (fun solution -> 
                                solution.Value.Projects
                                    |> Array.ofSeq
                                    |> Array.iter (fun project ->
                                                        if project.Value.OutputPath <> "" then
                                                            let name = Path.GetFileName(datalib)
                                                            if project.Value.OutputPath.ToLower() = name.ToLower() then
                                                                if not(solutionsThatGenerateLib.ContainsKey(solution.Value.Name)) && solution.Value.Name <> solutionIn.Name then
                                                                    solutionsThatGenerateLib <- solutionsThatGenerateLib.Add(solution.Value.Name, solution.Value)
                                                )
                                        )
                                    )

            solutionsThatGenerateLib

        let GetPotencialProjectsThatGenerateLib(lib:string) = 
            let datalib = 
                if Path.IsPathRooted(lib) then
                    Path.GetFileName(lib).ToLower()
                else
                    lib.ToLower()

            solutionsThatGenerateLib <- Map.empty

            targets
                |> Array.ofSeq
                |> Array.iter
                    (fun target ->
                        target.Children
                            |> Array.ofSeq
                            |> Array.iter (fun solution -> 
                                solution.Value.Projects
                                    |> Array.ofSeq
                                    |> Array.iter (fun project ->
                                                        if project.Value.ImportLib <> "" then
                                                            if Path.GetFullPath(project.Value.ImportLib).ToLower().EndsWith("\\" + datalib) then
                                                                if not(solutionsThatGenerateLib.ContainsKey(solution.Value.Name)) && solution.Value.Name <> solutionIn.Name then
                                                                    solutionsThatGenerateLib <- solutionsThatGenerateLib.Add(solution.Value.Name, solution.Value)
                                                )
                                        )
                                    )

            solutionsThatGenerateLib

        listOfLibsLink <- Set.empty
        allowedSearchFolders <- Set.empty
        listOfDependentReferences <- Set.empty

        // lets collect all link information
        solutionIn.Projects
            |> Array.ofSeq
            |> Array.iter (fun b ->
                b.Value.DependentLibs |> Seq.iter (fun c ->
                    (if not(listOfLibsLink.Contains(c)) then listOfLibsLink <- listOfLibsLink.Add(c)))
                b.Value.DllReferences |> Seq.iter (fun c ->
                    (if not(listOfDependentReferences.Contains(c)) then listOfDependentReferences <- listOfDependentReferences.Add(c)))
                b.Value.AdditionalIncludeDirectories |> Seq.iter (fun c ->
                    (if not(allowedSearchFolders.Contains(c)) then allowedSearchFolders <- allowedSearchFolders.Add(c))))

        listOfDependentReferences
            |> Array.ofSeq
            |> Array.iter (fun lib -> 
                // try find all projects that potencially can create this lib. With Absolute Paths
                let potentialCandidates = GetPotencialProjectsThatGenerateReference lib

                if potentialCandidates.Count > 1 then
                    Helpers.AddWarning(solutionIn.Name, sprintf "More than one solution is found that can used to link to this project %s %A\n" lib potentialCandidates)

                for candidate in potentialCandidates do
                    if not(solutionIn.SolutionExternalBuildDepencies.ContainsKey(candidate.Key)) then
                        solutionIn.SolutionExternalBuildDepencies.Add(candidate.Key, candidate.Value)
                )

        listOfLibsLink
            |> Array.ofSeq
            |> Array.iter (fun lib -> 
                // try find all projects that potencially can create this lib. With Absolute Paths
                let potentialCandidates = GetPotencialProjectsThatGenerateLib lib

                if potentialCandidates.Count > 1 then
                    Helpers.AddWarning(solutionIn.Name, sprintf "More than one solution is found that can used to link to this project %s %A\n" lib potentialCandidates)

                for candidate in potentialCandidates do
                    if not(solutionIn.SolutionExternalBuildDepencies.ContainsKey(candidate.Key)) then
                        solutionIn.SolutionExternalBuildDepencies.Add(candidate.Key, candidate.Value)
                )

    targets
        |> Array.ofSeq
        |> Array.iter (fun target ->
        target.Children
            |> Array.ofSeq
            |> Array.iter (fun solution -> 
                GenerateExternalDepForSolution(solution.Value, targets)
        ))
    ()

// evaluates projects and generate all needed dependencies for project
// implemented:
//    header
//    libs
let GenerateDependenciesForTargets(targets : ProjectTypes.MsbuildTarget List, plotHeaderDependency : bool, ignoreIncludeFolders : string, plotHeaderDependencFilter : string, plotHeaderDependencyInsideProject : bool) =
    
    if plotHeaderDependency then
        let ignoreSet = (ignoreIncludeFolders.Split([|';'; '\n'; ' '|], StringSplitOptions.RemoveEmptyEntries) |> Set.ofSeq)
        let includeSolutionsSet = (plotHeaderDependencFilter.Split([|';'; '\n'; ' '|], StringSplitOptions.RemoveEmptyEntries) |> Set.ofSeq)

        for target in targets do
            for solutionMain in target.Children do
                if includeSolutionsSet.IsEmpty || includeSolutionsSet.Contains(solutionMain.Key) then
            
                    let data = solutionMain
                    for project in solutionMain.Value.Projects do
                
                        for directory in project.Value.AdditionalIncludeDirectories do
                            try

                                let directoryAbs = Path.GetFullPath(directory).ToLower().Replace("\\", "/")

                                if not(ignoreSet.Contains(directoryAbs)) then
                                    let projectOption = FindDependencyInSolution(directoryAbs, solutionMain.Value)

                                    match projectOption with
                                    | Some (guid, projectFound) ->  
                                            if not(guid.Equals(project.Value.Guid))
                                                && plotHeaderDependencyInsideProject 
                                                && not(project.Value.HeaderReferences.ContainsKey(guid)) then
                                                    project.Value.HeaderReferences.Add(guid, projectFound)
                                                    project.Value.Visible <- true
                                    | _  ->

                                        let ProcessChildren(key:string, solutionIn:ProjectTypes.Solution) =
                                            let mutable returnValue = false
                                            if not(key.Equals(solutionMain.Key)) then
                                                let projectOption = FindDependencyInSolution(directoryAbs, solutionIn)

                                                match projectOption with
                                                | Some (guid, projectFound) ->  
                                                        if not(guid.Equals(project.Value.Guid))
                                                            && not(project.Value.HeaderReferences.ContainsKey(guid)) then
                                                                project.Value.HeaderReferences.Add(guid, projectFound)
                                                                project.Value.Visible <- true
                                                                returnValue <- true
                                                | _  -> ()

                                            returnValue

                                        let ProcessTarget(target:ProjectTypes.MsbuildTarget) =
                                            target.Children
                                                |> Seq.tryFind(fun child -> ProcessChildren(child.Key, child.Value))

                                        targets
                                            |> Seq.tryFind(fun target -> ProcessTarget(target).IsSome) |> ignore
                                    with
                                    | ex -> 
                                        let warning = sprintf "Invalid additional include Path : %s <=> %s" directory ex.Message
                                        Helpers.AddWarning(project.Value.Path, warning)

let CreateTargetTree(path : string, target : string,
                     nugetPackageBase : string, 
                     nugetIgnorePackages : string,
                     plotHeaderDependency : bool,
                     ignoreIncludeFolders : string,
                     plotHeaderDependencFilter : string,
                     plotHeaderDependencyInsideProject : bool,
                     toolsVersion : string) = 
    let mutable targets : ProjectTypes.MsbuildTarget List = List.Empty
    let msbuildproject = new Microsoft.Build.Evaluation.Project(path)
    let data = msbuildproject.Targets.[target]

    // collect pre processing information
    HandleTarget(data, msbuildproject, target, &targets, nugetPackageBase, nugetIgnorePackages, plotHeaderDependency, toolsVersion) |> ignore

    // generate build deps between existing solutions
    GenerateExternalBuildDependenciesForSolutions(targets)

    // generate build deps between existing targets
    GenerateDependenciesForTargets(targets, plotHeaderDependency, ignoreIncludeFolders, plotHeaderDependencFilter, plotHeaderDependencyInsideProject)

    ProjectCollection.GlobalProjectCollection.UnloadProject(msbuildproject)

    targets

let GetProjectFilePathForFile(projectPath : string, fileName : string) =

    let msbuildproject =
        let projects = ProjectCollection.GlobalProjectCollection.GetLoadedProjects(projectPath)
    
        if projects.Count <> 0 then
            let data = projects.GetEnumerator();
            data.MoveNext() |> ignore
            data.Current
        else
             new Microsoft.Build.Evaluation.Project(projectPath)

    let element = msbuildproject.Items |> Seq.tryFind (fun c -> fileName.ToLower().EndsWith(c.EvaluatedInclude.ToLower()))
    match element with
    | Some(c) -> c.EvaluatedInclude
    | _ -> ""



let GetIncludeGraphForFile(pathInput : string, project : ProjectTypes.Project) =
    let fileNameLower = Path.GetFileName(pathInput).ToLower()
    Helpers.ClearWarnings()
    let rec CheckFile(fileToCheck:string, stackFile:ProjectTypes.CallGraphFile, stackRecursive:Helpers.ImmutableStack<string>) = 

        let CheckLine(line:string, originalFile:string, nodeIn:ProjectTypes.CallGraphFile) = 
            let AddHeader(matchdata:Match) =
                let fileFound = matchdata.Groups.[1].Value
                let fileNameLowerOriginal = Path.GetFileName(originalFile).ToLower()
                if fileNameLower = fileFound.ToLower()  || fileNameLowerOriginal = fileFound.ToLower() then
                    let warning = sprintf "Recursive inclusion of same file : %s <=> %s" pathInput fileFound
                    Helpers.AddWarning(pathInput, warning)
                else
                    let ignored = Helpers.ignoreHeaders |> List.tryFind(fun c -> fileFound.StartsWith(c + ".") || fileFound.StartsWith(c))
                    match ignored with
                    | Some data -> ()
                    | _ ->

                        let DataFound = project.AdditionalIncludeDirectories |> Seq.tryFind (fun c -> (File.Exists(Path.Combine(c, fileFound))))
                        match DataFound with
                        | Some data -> 
                            let abspath = Path.GetFullPath(Path.Combine(data, fileFound))
                            let nodeElem = new ProjectTypes.CallGraphFile(abspath)
                            nodeIn.Node <- nodeIn.Node @ [nodeElem]

                            let stackData = stackRecursive.All()
                            let foundInStack = stackData |> Seq.tryFind(fun c -> c.Equals(abspath))
                            match foundInStack with
                            | Some value ->
                                let warning = sprintf "Cyclic include headers : %A" (stackRecursive.Push abspath)
                                Helpers.AddWarning(pathInput, warning)
                            | _ -> 
                                //printf "STACK %A \n" stackToCheck
                                //printf "Recursion Follow %A from First Entry %A and Previous Entry %A\n" abspath pathInput originalFile
                                CheckFile(abspath, nodeElem, stackRecursive.Push abspath)
                        | _ -> ()

            (Regex.Matches(line, "[ ]*\#include[ ]+[\"<]([^\"]*)[\">]"))
                |> Seq.cast
                |> Seq.iter (fun matchdata -> AddHeader matchdata)

        File.ReadAllLines(fileToCheck) |> Array.iter (fun line -> CheckLine(line, fileToCheck, stackFile)) // parallel

    let recursiveCallDep = Helpers.ImmutableStack.Empty.Push pathInput
    let callStack = new ProjectTypes.CallGraphFile(pathInput)
    CheckFile(pathInput, callStack, recursiveCallDep)
    callStack
