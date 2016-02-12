namespace ProjectCheckerTask

open System.IO
open System.Globalization
open VSSolutionUtils

open RuleBase
open ProjectTypes

type ReferencesCheck() = 
    inherit RuleBase()

    let FullyQualifiedReferenceRule =
        new Rule(Key = "FullyQualifiedReferenceRule",
                 Description = "Fully Qualify the References, Always specify the Version, Culture, PublicKeyToken attributes to make sure they will resolve to the right version of the DLL.")
    
    let AlwaysUserProjectReferencesInSameSolution =
        new Rule(Key = "AlwaysUserProjectReferencesInSameSolution",
                 Description = "For projects in the same solution, always use project references instead of DLL references. (Create them from the Projects tab of the Add References window of Visual Studio) In the project files, they will look different.")

    let MultiplePrivateSet =
        new Rule(Key = "MultiplePrivateSet",
                 Description = "Several Private Flags have been found, this is likely a nuget update issue. Please ensure only one private flag is used",
                 Level = "error")


    member val AssemblyLookpupLines : string array = [||] with get, set

    override this.SupportsProject(path) =
        path.EndsWith(".fsproj") || path.EndsWith(".csproj") || path.EndsWith(".vcxproj")

    override this.ExecuteCheck(project, path, lines, solution, outputPath) =
                
        let validateItemGroup(itemgroup : ProjType.ItemGroup) =

            let validateReferences(reference : ProjType.Reference) =

                let ValidateAlwaysUserProjectReferencesInSameSolution(path : string, reference : string, solution : string, lines : string []) =
                        try
                            let solutionHelper = new VSSolutionUtils()
                            let projects = solutionHelper.GetProjectFilesFromSolutions(solution)

                            let IsAssemblyInSolution(element : string) =
                                let eme = reference.Split(',')
                                element.Equals(eme.[0], System.StringComparison.InvariantCultureIgnoreCase)

                            let elem = List.toArray projects |> Array.find (fun ele -> IsAssemblyInSolution(ele.name))

                            let name = elem.name
                            this.SaveIssue(path, this.GetLineFromString(reference, lines) + 1, name + " should be referenced as project reference instead of dll reference", AlwaysUserProjectReferencesInSameSolution)
                        with
                         | ex -> ()

                let ref = reference.Include

                // valid fully qualified
                try
                    if not(ref.Contains("Version=")) || not(ref.Contains("Culture")) || not(ref.Contains("PublicKeyToken")) then
                        let indexOf = this.AssemblyLookpupLines |> Array.findIndex (fun elem -> ref.StartsWith(elem.Split(':').[0], true, CultureInfo.InvariantCulture))
                        if indexOf > 0 then
                            this.SaveIssue(path, this.GetLineFromString(ref, lines) + 1, "Always specify reference with Version, PublicKeyToken and Culture", FullyQualifiedReferenceRule)
                with
                | ex -> ()

                ValidateAlwaysUserProjectReferencesInSameSolution(path, reference.Include, solution, lines)

                // make sure only one private flag is set
                if reference.Privates.Length > 1 then
                     this.SaveIssue(path, this.GetLineFromString(ref, lines) + 1, ref + " contains multiple private flags.", MultiplePrivateSet)

            try
                itemgroup.References |> Seq.iter (fun x -> validateReferences(x))
            with
             | ex -> ()

        let assemblyReferencePath = Path.Combine(outputPath, "assemblyLookup.txt")
        if File.Exists(assemblyReferencePath) then
                this.AssemblyLookpupLines <- File.ReadAllLines(assemblyReferencePath)

        project.ItemGroups |> Seq.iter (fun x -> validateItemGroup(x))
                                
    override this.GetRules =
        [AlwaysUserProjectReferencesInSameSolution] @
        [FullyQualifiedReferenceRule]