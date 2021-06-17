module CsvHelper

open System.IO
open System

let WriteSolutionInfoToCsv(outputfile : string, solutionList : List<ProjectTypes.Solution>) = 
    let outFile = new StreamWriter(outputfile)
    for solution in solutionList do
        for project in solution.Projects do
            let line = sprintf "%s,%s,%s,%s" solution.Name solution.Path project.Value.OutputPath project.Value.TargetPath
            outFile.WriteLine(line)
    outFile.Flush()
    outFile.Close()