namespace ProjectCheckerTask

open MSBuildHelper
open System.IO
open RuleBase
open ProjectTypes

type IncludeFolderNotUsedDuringInCompilation() = 
    inherit RuleBase()
    let IncludeFolderNotUsedDuringInCompilation =
        new Rule(Key = "IncludeFolderNotUsedDuringInCompilation",
                 Description = "Additional include folder used during compilation can be removed as it is not used")

    let IncludeFileNotFound =
        new Rule(Key = "IncludeFileNotFound",
                 Description = "Additional include folder used during compilation can be removed as it is not used")

    override this.SupportsProject(path) =
        path.EndsWith(".vcxproj")

    override this.ExecuteCheckMsbuild(project, path, lines, solution, outputPath, includefolderstoignore) =

        try
            let mutable additionalIncludeDirectories : Set<string> = Set.empty
            let mutable includes : Set<string> = Set.empty

            for item in project.Items do
                try            
                    PopulateHeaders(&additionalIncludeDirectories, &includes, item, path)
                with
                | ex -> this.SaveIssue(path, 1, "Include Not Found in Disk " + item.EvaluatedInclude + " can be removed, its not used", IncludeFileNotFound)

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

            additionalIncludeDirectories |> Seq.iter (fun c ->  
                    if not(CheckFileExists(c)) then 
                        let foundInIgnore = includefolderstoignore |> Seq.tryFind ( fun d -> c.ToLower().Contains(d.ToLower()))
                        match foundInIgnore with
                        | Some(d) -> ()
                        | _ -> this.SaveIssue(path, 1, "Additional Include Path " + c + " can be removed, its not used", IncludeFolderNotUsedDuringInCompilation)
                    else
                        ())

        with
        | ex -> ()

                                
    override this.GetRules =
        [IncludeFolderNotUsedDuringInCompilation;IncludeFileNotFound]