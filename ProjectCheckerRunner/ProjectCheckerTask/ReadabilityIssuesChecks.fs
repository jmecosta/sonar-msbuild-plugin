namespace ProjectCheckerTask

open System.IO
open System.Globalization
open VSSolutionUtils
open System.Xml.Linq
open System.Xml
open RuleBase
open ProjectTypes

type ReadabilityIssuesChecks() = 
    inherit RuleBase()

    let EmptyTagsRule =
        new Rule(Key = "EmptyTagsRule",
                 Description = "Do not leave empty PropertyGroup / ItemGroup tags or tag pairs in the project files.")

    member val AssemblyLookpupLines : string array = [||] with get, set

    override this.SupportsProject(path) =
        path.EndsWith(".fsproj") || path.EndsWith(".csproj") || path.EndsWith(".vcxproj")

    override this.ExecuteCheck(project, path, lines, solution, outputPath) =
                 
        for attribute in project.XElement.Elements() do
            if not(attribute.HasElements) && not(attribute.HasAttributes) then
                this.SaveIssue(path, 1, "Make sure all empty xml nodes in this file are removed.", EmptyTagsRule)
                
                                
    override this.GetRules =
        [EmptyTagsRule]