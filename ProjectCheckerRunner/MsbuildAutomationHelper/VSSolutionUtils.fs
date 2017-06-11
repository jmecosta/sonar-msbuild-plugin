module VSSolutionUtils

open Microsoft.Build.Construction
open System.Resources
open System.Reflection
open System
open System.IO
open FSharp.Data
open System.Xml
open System.Xml.Linq

type Utils() = 
    let getEnvironmentVariable var = 
        System.Environment.GetEnvironmentVariable(var).Split(";".ToCharArray())

    member this.EscapeString(str : string) = 
        let doc = new XmlDocument()
        let node = doc.CreateElement("root")
        node.InnerText <- str
        node.InnerXml

    member this.ExistsOnPath(program) =
        let path = getEnvironmentVariable("PATH")
        let mutable returncode = false

        for i in path do
            let file = System.IO.Path.Combine(i, program)
            if System.IO.File.Exists(file) then
                returncode <- true
        returncode

    member this.ProcessFileUsingReplacementStrings (file : string, propertiestoreplace : string) = 
        if file.Contains("$(") then
            let values = propertiestoreplace.Split(';')
            let mutable fileend = file
            for elem in  values do
                if elem <> "" then
                    let key = elem.Split('=').[0]
                    let value = elem.Split('=').[1]
                    let replacestr = sprintf "$(%s)" key
                    fileend <- fileend.Replace(replacestr, value)

            fileend
        else
            file

type CppProjectFile = XmlProvider<"""<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003" DefaultTargets="Build" ToolsVersion="4.0">
  <ItemGroup Label="ProjectConfigurations">
    <ProjectConfiguration Include="Debug|Win32">
      <Configuration>Debug</Configuration>
      <Platform>Win32</Platform>
    </ProjectConfiguration>
    <ProjectConfiguration Include="Debug|x64">
      <Configuration>Debug</Configuration>
      <Platform>x64</Platform>
    </ProjectConfiguration>
    <ProjectConfiguration Include="Release|Win32">
      <Configuration>Release</Configuration>
      <Platform>Win32</Platform>
    </ProjectConfiguration>
    <ProjectConfiguration Include="Release|x64">
      <Configuration>Release</Configuration>
      <Platform>x64</Platform>
    </ProjectConfiguration>
  </ItemGroup>
  <PropertyGroup Label="Globals">
    <ProjectGuid>{DA54BEC9-D5E5-4BD5-B232-586076AB0278}</ProjectGuid>
    <RootNamespace>libdia_units</RootNamespace>
    <SccProjectName>SurroundSCMScci</SccProjectName>
    <SccLocalPath>..</SccLocalPath>
    <SccProvider>MSSCCI:Surround SCM</SccProvider>
    <Keyword>Win32Proj</Keyword>
    <SccAuxPath />
  </PropertyGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.Default.props" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|Win32'" Label="Configuration">
    <ConfigurationType>StaticLibrary</ConfigurationType>
    <CharacterSet>MultiByte</CharacterSet>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|Win32'" Label="Configuration">
    <ConfigurationType>StaticLibrary</ConfigurationType>
    <CharacterSet>MultiByte</CharacterSet>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|x64'" Label="Configuration">
    <ConfigurationType>StaticLibrary</ConfigurationType>
    <CharacterSet>MultiByte</CharacterSet>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|x64'" Label="Configuration">
    <ConfigurationType>StaticLibrary</ConfigurationType>
    <CharacterSet>MultiByte</CharacterSet>
  </PropertyGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" />
  <ImportGroup Label="ExtensionSettings">
  </ImportGroup>
  <ImportGroup Condition="'$(Configuration)|$(Platform)'=='Debug|Win32'" Label="PropertySheets">
    <Import Project="$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props" Condition="exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')" Label="LocalAppDataPlatform" />
  </ImportGroup>
  <ImportGroup Condition="'$(Configuration)|$(Platform)'=='Release|Win32'" Label="PropertySheets">
    <Import Project="$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props" Condition="exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')" Label="LocalAppDataPlatform" />
  </ImportGroup>
  <ImportGroup Condition="'$(Configuration)|$(Platform)'=='Debug|x64'" Label="PropertySheets">
    <Import Project="$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props" Condition="exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')" Label="LocalAppDataPlatform" />
  </ImportGroup>
  <ImportGroup Condition="'$(Configuration)|$(Platform)'=='Release|x64'" Label="PropertySheets">
    <Import Project="$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props" Condition="exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')" Label="LocalAppDataPlatform" />
  </ImportGroup>
  <PropertyGroup Label="UserMacros" />
  <ItemGroup>
    <ClCompile Include="file1.cpp" />
    <ClCompile Include="file2.cpp" />
  </ItemGroup>
  <ItemGroup>
    <ClInclude Include="header1.hpp" />
    <ClInclude Include="header2.hpp" />
    <Compile Include="Strings.cs" />
    <Compile Include="Strings.cs" />
  </ItemGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" />
  <ImportGroup Label="ExtensionTargets">
  </ImportGroup>
</Project>""">


type ProjectFiles(projectName : string, absolutepath : string) = 
    member val name = projectName with get
    member val path = absolutepath with get

type VSSolutionUtils() =
    let Readlines solutionPath = File.ReadLines(solutionPath)

    member this.GetProjectFilesFromSolutions(solutionPath : string) =
        let mutable projectFiles : ProjectFiles list = []
        for line in Readlines(solutionPath) do
            if line <> null then
                if line.StartsWith("Project(\"{") then
                    let projectName = line.Split(',').[0].Split('=').[1].Replace("\"","").Trim()
                    let projectRelativePath = line.Split(',').[1].Replace("\"","").Trim()
                    let path = Path.Combine(Directory.GetParent(solutionPath).ToString(), projectRelativePath)
                    if File.Exists(path) then
                        projectFiles <- projectFiles @ [new ProjectFiles(projectName, path)]
        projectFiles

type VSProjectUtils() = 
    let Readlines solutionPath = File.ReadLines(solutionPath)

    member this.GetCompilationFiles(projectFile : string, hasString : string, pathReplaceStrings : string) =
        let mutable files : string list = []
        let str = String.Concat(Readlines(projectFile))
        let data = CppProjectFile.Parse(str)

        for item in data.ItemGroups do

            let filterAndChangeStringsInFile(file : string) = 
                let mutable fileFinal = ""

                if String.IsNullOrEmpty(hasString) then
                    fileFinal <- file
                else
                    for elem in hasString.Split(';') do
                        if file.ToLowerInvariant().Contains(elem.ToLowerInvariant()) then
                            fileFinal <- file

                if not(String.IsNullOrEmpty(pathReplaceStrings)) then
                    fileFinal <- Utils().ProcessFileUsingReplacementStrings(fileFinal, pathReplaceStrings)

                fileFinal

            for source in item.ClCompiles do
                let validFile = filterAndChangeStringsInFile source.Include
                if not(String.IsNullOrEmpty(validFile)) then
                    if Path.IsPathRooted(validFile) then
                        files <- files @ [validFile]
                    else
                        files <- files @ [Path.Combine(Directory.GetParent(projectFile).ToString(), validFile)]
                          

            for headers in item.ClIncludes do
                let validFile = filterAndChangeStringsInFile headers.Include
                if not(String.IsNullOrEmpty(validFile)) then
                    if Path.IsPathRooted(validFile) then
                        files <- files @ [validFile]
                    else
                        files <- files @ [Path.Combine(Directory.GetParent(projectFile).ToString(), validFile)]

            for cssource in item.Compiles do
                let validFile = filterAndChangeStringsInFile cssource.Include
                if not(String.IsNullOrEmpty(validFile)) then
                    if Path.IsPathRooted(validFile) then
                        files <- files @ [validFile]
                    else
                        files <- files @ [Path.Combine(Directory.GetParent(projectFile).ToString(), validFile)]

        files
