namespace ProjectCheckerTask

open RuleBase
open ProjectTypes

type CannotReadProjectCheck() = 
    inherit RuleBase()
    let CannotReadProjectCheck =
        new Rule(Key = "CannotReadProjectError",
                 Description = "Cannot Read Project File Error")

    let mutable valid = false

    override this.SupportsProject(path) =
        true

    override this.ExecuteCheck(project, path, lines, solution, outputPath) =
        try
            project.ToolsVersion |> ignore            
        with
        | _ -> this.SaveIssue(path, 1, "Cannot Read File: " + path, CannotReadProjectCheck) 


    override this.GetRules =
        [CannotReadProjectCheck]        
