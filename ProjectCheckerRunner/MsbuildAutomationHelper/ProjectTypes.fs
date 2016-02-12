module ProjectTypes

open System
open System.Xml.Linq
open FSharp.Data

exception CannotFindIdForProject of string
exception NodeAlreadyExists of string
exception IncorrectNameForProject of string
exception FoundElementException of string

type NugetPackage() = 
    member val Name : string =  "" with get, set
    member val Id : string = "" with get, set
    member val Path : string = "" with get, set

type AssemblyRef() = 
    member val Name : string =  "" with get, set
    member val Path : string = "" with get, set

[<AllowNullLiteralAttribute>]
type Project() =
    member val Name : string =  "" with get, set
    member val Guid : Guid = Guid.Empty with get, set
    member val Path : string = "" with get, set
    member val BuildDepencies :  System.Collections.Generic.Dictionary<Guid, Project>  = new System.Collections.Generic.Dictionary<Guid, Project>() with get, set
    member val ProjectReferences :  System.Collections.Generic.Dictionary<Guid, Project>  = new System.Collections.Generic.Dictionary<Guid, Project>() with get, set
    member val HeaderReferences : System.Collections.Generic.Dictionary<Guid, Project>  = new System.Collections.Generic.Dictionary<Guid, Project>() with get, set
    member val DependentDirectories : System.Collections.Generic.HashSet<string> = new System.Collections.Generic.HashSet<string>() with get, set
    member val AssemblyReferences : System.Collections.Generic.Dictionary<Guid, AssemblyRef>  = new System.Collections.Generic.Dictionary<Guid, AssemblyRef>() with get, set
    member val NugetReferences : Set<string> = Set.empty with get, set
    member val Visible : bool = false with get, set

type DirectoryRef() = 
    member val Path : string =  "" with get, set
    member val Project : Project = null with get, set

type FileRef() = 
    member val Path : string =  "" with get, set
    member val Directory : Set<string> = Set.empty with get, set

[<AllowNullLiteralAttribute>]
type Solution() = 
    member val Name : string =  "" with get, set
    member val Path : string =  "" with get, set
    member val Guid : Guid =  Guid.Empty with get, set
    member val Projects : System.Collections.Generic.Dictionary<Guid, Project>  = new System.Collections.Generic.Dictionary<Guid, Project>() with get, set

type MsbuildTarget() = 
    member val Name : string =  "" with get, set
    member val MsbuildTargetDependencies : Map<string, MsbuildTarget> = Map.empty with get, set
    member val Children : Map<string, Solution> = Map.empty with get, set

type Rule() = 
    member val Key : string = "" with get, set
    member val Description : string = "" with get, set
    member val Level : string = "warning" with get, set
    
type SonarIssue() =
    member val Rule : string = "" with get, set
    member val Line : int = 0 with get, set
    member val Component : string = "" with get, set
    member val Message : string = "" with get, set
    member val Level : string = "warning" with get, set

type SonarResoureMetrics(path : string) = 
    member val ResourcePath : string = path with get
    member val Issues : SonarIssue List = List.empty with get, set


type ProjType = XmlProvider<"""<?xml version="1.0" encoding="utf-8"?>
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
    <OutDir>$(BINDir)\plugins\asdsa\Model</OutDir>
    <OutputPath>$(BINDir)\plugins\dsads\Model</OutputPath>
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
  <Reference Include="Library, Version=$(TSVersionNumber).0.0, Culture=neutral, PublicKeyToken=2f04dbe497b71114, processorArchitecture=MSIL">
    <HintPath>$(BINDir)\dll.dll</HintPath>
    <Private>False</Private>
    <SpecificVersion>False</SpecificVersion>
  </Reference>
  <Reference Include="Library1">
    <HintPath>$(BINDir)\Application.Library.dll</HintPath>
    <Private>False</Private>
    <Private>True</Private>
  </Reference>
  </ItemGroup>
  <ItemGroup>
    <ClCompile Include="file1.cpp" />
    <ClCompile Include="file2.cpp" />
  </ItemGroup>
  <ItemGroup>
    <ClInclude Include="header1.hpp" />
    <ClInclude Include="header2.hpp" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\DiffCalc\DiffCalc.csproj">
      <Project>{CA9B6FA7-9A52-49E9-9387-22C82B0F8962}</Project>
      <Name>DiffCalc</Name>
    </ProjectReference>
    <ProjectReference Include="..\DifferenceEngine\DifferenceEngine.csproj">
      <Project>{8ae4897a-ea74-49b1-ba55-d892c27af9c9}</Project>
      <Name>DifferenceEngine</Name>
    </ProjectReference>
    <ProjectReference Include="..\ExtensionTypes\ExtensionTypes.csproj">
      <Project>{75A39A9B-06B2-4249-9D3C-C2319951BC53}</Project>
      <Name>ExtensionTypes</Name>
    </ProjectReference>
    <ProjectReference Include="..\SonarRestService\SonarRestService.fsproj">
      <Project>{64728a55-1166-4ec6-b066-22c4e01fc1c7}</Project>
      <Name>SonarRestService</Name>
    </ProjectReference>
    <ProjectReference Include="..\VSSonarPlugins\VSSonarPlugins.csproj">
      <Project>{3311C918-B662-436D-8BC8-F38B447B1414}</Project>
      <Name>VSSonarPlugins</Name>
    </ProjectReference>
  </ItemGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" />
  <ImportGroup Label="ExtensionTargets">
  </ImportGroup>
</Project>""">
