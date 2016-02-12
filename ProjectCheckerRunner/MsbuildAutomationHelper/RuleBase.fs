module RuleBase

open ProjectTypes

type RuleBase() = 
  let mutable issueList : SonarIssue list = []

  member this.SaveIssue(file : string, line : int, issue : string, rule : Rule) =
        try 
            let index = issueList |> List.findIndex (fun arg -> (arg.Component.Equals(file) && arg.Message.Equals(issue) && arg.Line.Equals(line)))
            if index = -1 then
                issueList <- issueList @ [new SonarIssue(Component = file,
                                                    Line = line, 
                                                    Message= issue,
                                                    Rule = rule.Key,
                                                    Level = rule.Level)]
        with
        | ex ->
            issueList <- issueList @ [new SonarIssue(Component = file,
                                                Line = line, 
                                                Message= issue,
                                                Rule = rule.Key,
                                                Level = rule.Level)]

  member this.GetIssues() = 
    issueList

  member this.GetLineFromString(str : string, lines : string []) =
    try        
        (lines |> Array.findIndex (fun elem -> (elem.IndexOf(str, System.StringComparison.InvariantCultureIgnoreCase) >= 0))) + 1
    with
    | ex -> -1
              
  abstract member SupportsProject : string -> bool
  default this.SupportsProject(path) = false

  abstract member ExecuteCheck : ProjType.Project * string * string [] * string * string -> unit
  default this.ExecuteCheck(project, path, lines, solutionPath, outputPath) = ()

  abstract member ExecuteCheckMsbuild : project : Microsoft.Build.Evaluation.Project * string * string [] * string * string * string List -> unit
  default this.ExecuteCheckMsbuild(project, path, lines, solutionPath, outputPath, includefolderstoignore) = ()

  abstract member GetRules : Rule list
  default this.GetRules = []
