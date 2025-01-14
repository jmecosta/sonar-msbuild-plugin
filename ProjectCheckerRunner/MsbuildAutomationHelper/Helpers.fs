module Helpers

open System.IO
open System.Text.RegularExpressions

// Define an immutable stack
type ImmutableStack<'T> =
    | Empty 
    | Stack of 'T * ImmutableStack<'T>

    member s.Push x = Stack(x, s)

    member s.Pop() = 
        match s with
        | Empty -> failwith "Underflow"
        | Stack(t,_) -> t

    member s.Top() = 
        match s with
        | Empty -> failwith "Contain no elements"
        | Stack(_,st) -> st

    member s.IEmpty = 
        match s with
        | Empty -> true
        | _ -> false

    member s.All() = 
        let rec loop acc = function
        | Empty -> acc
        | Stack(t,st) -> loop (t::acc) st
        loop [] s

type Warning() = 
    member val Data : string =  "" with get, set
    member val Path : string = "" with get, set

let mutable cacheOfHeaders = Map.empty
let mutable directoryLookupCache = Map.empty
let mutable warnings : Warning list = list.Empty

let ClearWarnings() =
    warnings <- List.Empty

let AddWarning(path:string, data:string) =
    let warning = new Warning()
    warning.Data <- data
    warning.Path <- path
    warnings <- warnings @ [warning]

let ignoreHeaders = ["cstdlib"; "cstdarg"; "climits"; "cfloat"; "cstding";
                        "csignal"; "typeinfo"; "memory_resource"; "cinttypes";
                        "csetjmp"; "type_traits"; "scoped_allocator"; "limits";
                        "functional"; "bitset"; "memory"; "exception";
                        "utility"; "chrono"; "new"; "stdexcept";
                        "ctime"; "cstddef"; "optional"; "variat";  "cassert";
                        "initializer_list"; "tuple"; "any"; "system_error"; "cerrno";
                        "map"; "string"; "set"; "windows.h"
                        ]

let FindFullPathFromAdditionalDirectories(file : string, mainPath : string) =
    let ValidateFileInFolder(c : string []) = 
        let matchCasingName(d:string) = 
            let fileName = Path.GetFileName(d)
            let fileNameLowerCase = fileName.ToLower()

            if fileName = file || fileNameLowerCase = file then
                if fileName <> file then
                    AddWarning(mainPath, "Include casing is incorrect : " + file)
                true
            else
                false

        (c |> Seq.tryFind(fun d -> matchCasingName d))

    directoryLookupCache |> Seq.tryFind (fun c -> (ValidateFileInFolder c.Value).IsSome)

let EnumerateFilesInIncludeDir(additionalIncludeDirectories : string list, projectFile : string) = 
    try
        let ProcessDir dir = 
            if not(directoryLookupCache.ContainsKey(dir)) then
                if Directory.Exists(dir) then
                    let filesInDir = Directory.GetFiles(dir)
                    directoryLookupCache <- directoryLookupCache.Add(dir, filesInDir)
                else
                    AddWarning(projectFile, "Additional Include Path Has Not Been Found in Disk : " + dir)

        additionalIncludeDirectories
            |> Seq.toArray
            |> Array.Parallel.iter (fun dir -> ProcessDir dir)
    with
    | ex -> printf "Failed to enumerate Dirs node: %A %s %s\n" additionalIncludeDirectories ex.Message  ex.StackTrace


// FSharp 4.3.1 compatibility
let mutable headersData : string [] = Array.empty

let rec CheckFile(fileToCheck:string,
                  stack:ImmutableStack<string>,
                  pathInput : string,
                  projectPath : string) = 
    //printf "Recursion Follow %s \n" fileToCheck

    let CheckLine(line:string, originalFile:string) = 
        let AddHeader(matchdata:Match) =
            let fileFound = matchdata.Groups.[1].Value
            if pathInput.ToLower() = fileFound.ToLower()  || Path.GetFileName(originalFile).ToLower() = fileFound.ToLower() then
                let warning = sprintf "Recursive inclusion of same file : %s <=> %s" pathInput fileFound
                AddWarning(pathInput, warning)
            else
                let ignored = ignoreHeaders |> List.tryFind(fun c -> fileFound.StartsWith(c + ".") || fileFound.StartsWith(c))
                match ignored with
                | Some data -> ()
                | _ ->
                    match FindFullPathFromAdditionalDirectories(fileFound, fileToCheck) with
                    | Some data -> 
                        let abspath = Path.Combine(data.Key, fileFound)
                        let stackData = stack.All()
                        let foundInStack = stackData |> Seq.tryFind(fun c -> c.Equals(abspath))
                        match foundInStack with
                        | Some value ->
                            let warning = sprintf "Cyclic include headers : %A" (stack.Push abspath)
                            AddWarning(pathInput, warning)
                        | _ -> 
                            //printf "STACK %A \n" stackToCheck
                            //printf "Recursion Follow %A from First Entry %A and Previous Entry %A\n" abspath pathInput originalFile

                            // add found header to headers for file
                            if (headersData |> Seq.tryFind (fun d -> d.Equals(abspath))).IsNone then
                                    headersData <- Array.append headersData [|abspath|]

                            let subElements = CheckFile(abspath, stack.Push abspath, pathInput, projectPath)
                            subElements |> Seq.iter (fun c -> 
                                if (headersData |> Seq.tryFind (fun d -> d.Equals(c))).IsNone then
                                    headersData <- Array.append headersData [|c|]
                                )
                    | _ -> ()

        (Regex.Matches(line, "[ ]*\#include[ ]+[\"<]([^\"]*)[\">]"))
            |> Seq.cast
            |> Seq.iter (fun matchdata -> AddHeader matchdata)

    if not(cacheOfHeaders.ContainsKey(fileToCheck)) then
        if File.Exists(fileToCheck) then
            File.ReadAllLines(fileToCheck) |> Array.Parallel.iter (fun line -> CheckLine(line, fileToCheck)) // parallel
            cacheOfHeaders <- cacheOfHeaders.Add(fileToCheck, headersData)
        else
            AddWarning(projectPath, "File : " + fileToCheck + " was not found")

        headersData
    else
        cacheOfHeaders.[fileToCheck]

// gets include files for header, should follow paths upstream.
let GetIncludePathsForFile(pathInput : string, additionalIncludeDirectories : string list, projectPath : string) =

    EnumerateFilesInIncludeDir(additionalIncludeDirectories, projectPath)
    headersData <- Array.empty
    if not(cacheOfHeaders.ContainsKey(Path.GetFullPath(pathInput))) then
        let callStack = ImmutableStack.Empty.Push pathInput
        CheckFile(pathInput, callStack, pathInput, projectPath)
    else
        cacheOfHeaders.[Path.GetFullPath(pathInput)]