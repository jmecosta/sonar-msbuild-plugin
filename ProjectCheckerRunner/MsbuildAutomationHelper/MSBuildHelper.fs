module MSBuildHelper

open System
open System.IO

open System.Text
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
                        let projectRef =                         
                            if not(solutionData.Projects.ContainsKey(new Guid(id))) then
                                let projectRef = new ProjectTypes.Project()
                                projectRef.Guid <- new Guid(id)
                                if projectRef.Name = "" then
                                    raise(ProjectTypes.IncorrectNameForProject("Post Project Incorrectly Named"))

                                solutionData.Projects.Add(projectRef.Guid, projectRef)
                                projectRef
                            else
                                solutionData.Projects.[new Guid(id)]
                        
                        projectRef.Visible <- true
                        project.Visible <- true
                        project.BuildDepencies.Add(projectRef.Guid, projectRef)                    
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

let PopulateHeaderMatrix(additionalIncludeDirectories : byref<Set<string>>, includes : byref<Set<string>>, item : ProjectItem, project : ProjectTypes.Project, packagesBase : string) = 
    if item.ItemType.Equals("ClCompile") then
        let projectPath = Path.GetFullPath(Path.GetDirectoryName(project.Path)).ToString()
        let path = 
            if Path.IsPathRooted(item.EvaluatedInclude) then
                item.EvaluatedInclude
            else
                Path.Combine(projectPath, item.EvaluatedInclude)

        let metadataelems = Seq.toList item.Metadata |> List.tryFind (fun c -> c.Name.Equals("AdditionalIncludeDirectories"))

        match metadataelems with
        | Some value -> 
            let includeDirs = value.EvaluatedValue.Split([|';'; '\n'; ' '; '\r'; '\t'|], StringSplitOptions.RemoveEmptyEntries)
            for path in includeDirs do
                let dir = path.ToLower().Replace("\\", "/")
                if not(dir.Contains(packagesBase)) then
                    if not(additionalIncludeDirectories.Contains(path)) then
                        if Path.IsPathRooted(path) then
                            additionalIncludeDirectories <- additionalIncludeDirectories.Add(path)
                        else
                            let basePath = Directory.GetParent(project.Path).ToString()
                            additionalIncludeDirectories <- additionalIncludeDirectories.Add(Path.GetFullPath(Path.Combine(basePath, path)))
                    if not(project.DependentDirectories.Contains(path)) then
                        project.DependentDirectories.Add(path) |> ignore
        | _ -> ()

        additionalIncludeDirectories <- additionalIncludeDirectories.Add(projectPath)
        let dirs = (Seq.toList additionalIncludeDirectories)
        for fileinclude in Helpers.GetIncludePathsForFile(path, dirs, project.Path) do
                if not(includes.Contains(fileinclude)) then
                    includes <- includes.Add(fileinclude)




let PopulateHeaders(additionalIncludeDirectories : byref<Set<string>>, includes : byref<Set<string>>, item : ProjectItem, projectPath : string) = 
    if item.ItemType.Equals("ClCompile")  ||  item.ItemType.Equals("ClInclude")  then

        let projectPathDir = Path.GetFullPath(Path.GetDirectoryName(projectPath)).ToString()
        let path = 
            if Path.IsPathRooted(item.EvaluatedInclude) then
                item.EvaluatedInclude
            else
                Path.Combine(projectPathDir, item.EvaluatedInclude)

        // get all additional includes
        let metadataelems = Seq.toList item.Metadata
        match metadataelems |> List.tryFind (fun c -> c.Name.Equals("AdditionalIncludeDirectories")) with
        | Some value ->
            let includeDirs = value.EvaluatedValue.Split([|';'; '\n'; ' '; '\r'; '\t'|], StringSplitOptions.RemoveEmptyEntries)
            for path in includeDirs do
                if not(additionalIncludeDirectories.Contains(path)) then
                    if Path.IsPathRooted(path) then
                        additionalIncludeDirectories <- additionalIncludeDirectories.Add(path)
                    else
                        let basePath = Directory.GetParent(projectPath).ToString()
                        additionalIncludeDirectories <- additionalIncludeDirectories.Add(Path.GetFullPath(Path.Combine(basePath, path)))
        | _ -> ()

        additionalIncludeDirectories <- additionalIncludeDirectories.Add(projectPath)
        let dirs = (Seq.toList additionalIncludeDirectories)
        for fileinclude in Helpers.GetIncludePathsForFile(path, dirs, projectPath) do
                if not(includes.Contains(fileinclude)) then
                    includes <- includes.Add(fileinclude)



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

            try
                project.ProjectReferences.Add(projectRef.Guid, projectRef)
                project.Visible <- true
            with
            | ex -> 
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

let HandleCppProjecItems(collectionOfItem : Collections.Generic.ICollection<ProjectItem>, project : ProjectTypes.Project, solution : ProjectTypes.Solution, packagesBase : string, checkRedundantIncludes : bool) =     
    let mutable additionalIncludeDirectories : Set<string> = Set.empty
    let mutable includes : Set<string> = Set.empty

    let itemsCollections = collectionOfItem
                            |> List.ofSeq
                            |> Array.ofList

    itemsCollections
        |> Array.iter (fun item ->
            PopulateHeaderMatrix(&additionalIncludeDirectories, &includes, item, project, packagesBase)
            PopulateProjectReferences(item, project, solution)
        ) // parallel

    if checkRedundantIncludes then
        CheckAdditionalIncludeDirectories(additionalIncludeDirectories, includes, project)


let CreateProjecNodesAndLinks(ignoredPackages : Set<string>, packagesBasePath : string, solution : ProjectTypes.Solution, checkRedundantIncludes : bool) =

    let supportedExtensions = Set.ofList [".vcxproj"]

    for project in solution.Projects do
        let extension = Path.GetExtension(project.Value.Path).ToLower()

        if supportedExtensions.Contains(extension) then
            try
                if extension = ".vcxproj" then
                    printf "Handle %A \n" project.Value.Path
                    let msbuildproject = new Microsoft.Build.Evaluation.Project(project.Value.Path)
                    let id = (msbuildproject.Properties |> Seq.find (fun c -> c.Name.Equals("RootNamespace"))).EvaluatedValue

                    if not(Path.GetFileNameWithoutExtension(project.Value.Path).ToLower().Equals(id.ToLower())) then
                        raise(ProjectTypes.IncorrectNameForProject("Post Project Incorrectly Named"))

                    project.Value.Name <- id

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

                    HandleCppProjecItems(msbuildproject.Items, project.Value, solution, packagesBasePath, checkRedundantIncludes)


                elif extension = ".csproj" then
                    raise (ProjectTypes.CannotFindIdForProject("extension not supported"))
                else
                    raise (ProjectTypes.CannotFindIdForProject("extension not supported"))
                         
            with
            | ex -> printf "Failed to create node: %s %s %s\n" project.Value.Path ex.Message  ex.StackTrace


let rec HandleTarget(projectInstance : ProjectTargetInstance,
                     msbuildproject : Microsoft.Build.Evaluation.Project,
                     target : string,
                     targets : byref<ProjectTypes.MsbuildTarget List>,
                     nugetPackageBase : string,
                     nugetIgnorePackages : string,
                     checkRedundantIncludes : bool) =
    
    let targetdata = msbuildproject.Targets.[target]
    let newTarget = new ProjectTypes.MsbuildTarget()
    newTarget.Name <- "MSB:" + target

    printf "Handle MSBuild TARGET : %A\n" newTarget.Name

    for children in projectInstance.Children do
        try
            let instance = children :?> ProjectTaskInstance
            if instance.Name.Equals("MSBuild") then
                let projects = instance.Parameters.["Projects"]
                let fullElements = projects.Split([|';'; '\n'; ' '; '\r';|], StringSplitOptions.RemoveEmptyEntries)

                let ProcessProjectForDeps(projectSolution:string) = 
                    if projectSolution.ToLower().EndsWith(".sln") then
                        printf "Handle Solution : %A\n" projectSolution
                        let solution = CreateSolutionData(projectSolution)
                        CreateProjecNodesAndLinks((nugetIgnorePackages.Split([|';'; '\n'; ' '|], StringSplitOptions.RemoveEmptyEntries) |> Set.ofSeq), nugetPackageBase, solution, checkRedundantIncludes)
                        newTarget.Children <- newTarget.Children.Add(solution.Name, solution)

                for project in projects.Split([|';'; '\n'; ' '; '\r'; '$'; '('; ')'; '@'; '%'|], StringSplitOptions.RemoveEmptyEntries) do
                    let value = (msbuildproject.GetPropertyValue(project))

                    if value <> "" then
                        ProcessProjectForDeps(value)
                    else
                        // not able to evaluate data, search in file for property
                        let xmlData = 
                            if not(BuildScripts.ContainsKey(children.FullPath)) then
                                BuildScripts <- BuildScripts.Add(children.FullPath, ProjectTypes.ProjType.Parse(File.ReadAllText(children.FullPath)))

                            BuildScripts.[children.FullPath]

                        let processXmlElements(topLevel:Xml.Linq.XElement) = 

                            let ProcessAttribute(attrib:Xml.Linq.XAttribute) = 
                                if attrib.Name.LocalName.ToLower().Equals("include") then
                                    for includeProj in attrib.Value.Split([|';'; '\n'; ' '; '\r'; '$'; '('; ')'|], StringSplitOptions.RemoveEmptyEntries) do
                                        if Path.IsPathRooted(includeProj) then
                                            ProcessProjectForDeps(includeProj)
                                        else
                                            let value = (msbuildproject.GetPropertyValue(includeProj))
                                            if value <> "" then
                                                ProcessProjectForDeps(value)

                            let ProcessElement(elem:Xml.Linq.XElement) = 
                                let localName = elem.Name.LocalName
                                if localName.Equals(project) then
                                    elem.Attributes() |> Seq.iter (fun x -> ProcessAttribute(x))

                            topLevel.Elements() |> Seq.iter (fun elem -> ProcessElement(elem))

                        let targetSubData = xmlData.Targets |> Seq.tryFind(fun x -> x.Name.Equals(target))
                        match targetSubData with
                        | Some target -> target.ItemGroups |> Seq.iter (fun x -> processXmlElements(x.XElement))
                        | _ -> ()
                        ()

        with
        | ex -> ()
        

    for dep in projectInstance.DependsOnTargets.Split([|';'; '\n'; ' '; '\r'|], StringSplitOptions.RemoveEmptyEntries) do
        if dep <> "" then
            let data = msbuildproject.Targets.[dep]            
            let name, depTarget = HandleTarget(data, msbuildproject, dep, &targets, nugetPackageBase, nugetIgnorePackages, checkRedundantIncludes)            
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
                               plotHeaderDependencFilter : string, plotHeaderDependencyInsideProject : bool) =

    if plotHeaderDependency then
        let ignoreSet = (ignoreIncludeFolders.Split([|';'; '\n'; ' '|], StringSplitOptions.RemoveEmptyEntries) |> Set.ofSeq)
        let includeSolutionsSet = (plotHeaderDependencFilter.Split([|';'; '\n'; ' '|], StringSplitOptions.RemoveEmptyEntries) |> Set.ofSeq)
        for solution in solutionList do
            if includeSolutionsSet.IsEmpty || includeSolutionsSet.Contains(solution.Name) then
                for project in solution.Projects do
                    for directory in project.Value.DependentDirectories do
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

let GenerateHeaderDependenciesForTargets(targets : ProjectTypes.MsbuildTarget List, plotHeaderDependency : bool, ignoreIncludeFolders : string, plotHeaderDependencFilter : string, plotHeaderDependencyInsideProject : bool) =
    
    if plotHeaderDependency then
        let ignoreSet = (ignoreIncludeFolders.Split([|';'; '\n'; ' '|], StringSplitOptions.RemoveEmptyEntries) |> Set.ofSeq)
        let includeSolutionsSet = (plotHeaderDependencFilter.Split([|';'; '\n'; ' '|], StringSplitOptions.RemoveEmptyEntries) |> Set.ofSeq)

        for target in targets do
            for solution in target.Children do
                if includeSolutionsSet.IsEmpty || includeSolutionsSet.Contains(solution.Key) then
            
                    let data = solution
                    for project in solution.Value.Projects do
                
                        for directory in project.Value.DependentDirectories do

                            let directoryAbs = Path.GetFullPath(directory).ToLower().Replace("\\", "/")

                            if not(ignoreSet.Contains(directoryAbs)) then
                                let projectOption = FindDependencyInSolution(directoryAbs, solution.Value)

                                match projectOption with
                                | Some (guid, projectFound) ->  
                                        if not(guid.Equals(project.Value.Guid)) && plotHeaderDependencyInsideProject then
                                            if not(project.Value.HeaderReferences.ContainsKey(guid)) then
                                                project.Value.HeaderReferences.Add(guid, projectFound)
                                                project.Value.Visible <- true
                                | _  -> 
                                    try
                                        for target2 in targets do
                                            for solution2 in target2.Children do
                                                if not(solution2.Key.Equals(solution.Key)) then
                                                    let projectOption = FindDependencyInSolution(directoryAbs, solution2.Value)
                                                    match projectOption with
                                                    | Some (guid, projectFound) ->  
                                                            if not(guid.Equals(project.Value.Guid)) then
                                                                project.Value.HeaderReferences.Add(guid, projectFound)
                                                                project.Value.Visible <- true
                                                                raise(ProjectTypes.FoundElementException("found"))
                                                    | _  -> ()
                                    with
                                    | ex -> ()


let CreateTargetTree(path : string, target : string,
                     nugetPackageBase : string,
                     nugetIgnorePackages : string,
                     checkRedundantIncludes : bool,
                     plotHeaderDependency : bool,
                     ignoreIncludeFolders : string,
                     plotHeaderDependencFilter : string,
                     plotHeaderDependencyInsideProject : bool) = 
    let mutable targets : ProjectTypes.MsbuildTarget List = List.Empty
    let msbuildproject = new Microsoft.Build.Evaluation.Project(path)
    let data = msbuildproject.Targets.[target]
    HandleTarget(data, msbuildproject, target, &targets, nugetPackageBase, nugetIgnorePackages, checkRedundantIncludes) |> ignore
    GenerateHeaderDependenciesForTargets(targets, plotHeaderDependency, ignoreIncludeFolders, plotHeaderDependencFilter, plotHeaderDependencyInsideProject)
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
