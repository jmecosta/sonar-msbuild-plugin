namespace ProjectCheckerTask

open RuleBase
open ProjectTypes

type CSharpStaticAnalysersChecks() = 
    inherit RuleBase()
    let MicrosoftCSharpShouldBeIncludeAlways =
        new Rule(Key = "MicrosoftCSharpShouldBeIncludeAlways",
                 Description = "In C# projects, also import the Microsoft.CSharp after all project settings. In the end of the file")

    override this.SupportsProject(path) =
        path.EndsWith(".csproj")       

    override this.ExecuteCheck(project, path, lines, solution, outputPath) =

        let imports = project.Imports
        let importIndexCharpTargets = imports |> Array.findIndex(fun x -> x.Project.EndsWith("Microsoft.CSharp.targets", System.StringComparison.InvariantCultureIgnoreCase))
        if importIndexCharpTargets = -1 then
            this.SaveIssue(path, 1, "Microsoft.CSharp.targets Not Include", MicrosoftCSharpShouldBeIncludeAlways)
                                
    override this.GetRules =
        [MicrosoftCSharpShouldBeIncludeAlways]      
