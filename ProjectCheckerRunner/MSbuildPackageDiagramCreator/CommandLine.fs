module CommandLine

open System
open System.IO
open System.Text
open System.Text.RegularExpressions

let ShowHelp () =
        Console.WriteLine ("Usage: MSbuildPackageDiagramCreator [OPTIONS]")
        Console.WriteLine ("Creates DMGL Diagrams for msbuild and dependecies")
        Console.WriteLine ()
        Console.WriteLine ("Options:")
        Console.WriteLine ("    /I|/i:<xml configuration file>")
        Console.WriteLine ("    /S|/s:<Solution to Parse>")
        Console.WriteLine ("    /O|/o:<Output fileName>")
        Console.WriteLine ("    /M|/m:<msbuild file to parse>")
        Console.WriteLine ("    /T|/t:<target to use in msbuild>")
        Console.WriteLine ("    /D|/d:<directory>")
        Console.WriteLine ("    /H|/h <show help>")

// parse command using regex
// if matched, return (command name, command value) as a tuple
let (|Command|_|) (s:string) =
    let r = new Regex(@"^(?:-{1,2}|\/)(?<command>\w+)[=:]*(?<value>.*)$",RegexOptions.IgnoreCase)
    let m = r.Match(s)
    if m.Success then 
        Some(m.Groups.["command"].Value.ToLower(), m.Groups.["value"].Value)
    else
        None

// take a sequence of argument values
// map them into a (name,value) tuple
// scan the tuple sequence and put command name into all subsequent tuples without name
// discard the initial ("","") tuple
// group tuples by name 
// convert the tuple sequence into a map of (name,value seq)
let parseArgs (args:string seq) =
    args 
    |> Seq.map (fun i -> 
                        match i with
                        | Command (n,v) -> (n,v) // command
                        | _ -> ("",i)            // data
                       )
    |> Seq.scan (fun (sn,_) (n,v) -> if n.Length>0 then (n,v) else (sn,v)) ("","")
    |> Seq.skip 1
    |> Seq.groupBy (fun (n,_) -> n)
    |> Seq.map (fun (n,s) -> (n, s |> Seq.map (fun (_,v) -> v) |> Seq.filter (fun i -> i.Length>0)))
    |> Map.ofSeq

