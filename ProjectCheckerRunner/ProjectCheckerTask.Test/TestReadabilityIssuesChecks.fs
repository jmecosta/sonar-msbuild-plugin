namespace ProjectCheckerTask.Test

open NUnit.Framework
open Foq
open FSharp.Data
open System.Xml.Linq
open System.IO
open ProjectCheckerTask
open RuleBase
open ProjectTypes
type TestReadabilityIssuesChecks() = 

    let InvalidDataFile = [ """<?xml version="1.0" encoding="utf-8"?> """;
                            """ <Project ToolsVersion="4.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003"> """;                          
                            """   <Import Project="$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props" Condition="exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')" Label="LocalAppDataPlatform" /> """;
                            """ <Import Project="$(VCTargetsPath)\Microsoft.Cpp.Default.props" />""";
                            """ <Import Project="$(SRCDir)\MSBuild\root_compile.props" /> """;
                            """ <Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" /> """;
                            """ <ImportGroup Label="PropertySheets"> """;                          
                            """ </ImportGroup> """;
                            """ <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Debug|x86'"> """;
                            """ </PropertyGroup> """;
                            """ <ItemGroup> """;
                            """ </ItemGroup> """;
                            """ <PropertyGroup /> """
                            """ <SccAuxPath /> """
                            """ <ItemGroup> """;
                            """ <Compile Include="Dialog\MainDialog.cs"> """;
                            """  <SubType>Form</SubType> """;
                            """</Compile> """;
                            """ </ItemGroup> """;
                            """</Project> """
                        ]

    [<Test>]
    member test.``Should Not Run for Know supported Project Type`` () = 
        let Rule = new ReadabilityIssuesChecks()
        Assert.That((Rule :> RuleBase).SupportsProject("path.vsdcxproj"), Is.False)
        Assert.That((Rule :> RuleBase).SupportsProject("path.casproj"), Is.False)
        Assert.That((Rule :> RuleBase).SupportsProject("path.fspdroj"), Is.False)


    [<Test>]
    member test.``Should Run for Every Project Type`` () = 
        let Rule = new ReadabilityIssuesChecks()
        Assert.That((Rule :> RuleBase).SupportsProject("path.vcxproj"), Is.True)
        Assert.That((Rule :> RuleBase).SupportsProject("path.csproj"), Is.True)
        Assert.That((Rule :> RuleBase).SupportsProject("path.fsproj"), Is.True)

    [<Test>]
    member test.``Empty Item Groups Should Report Violations`` () = 
        let Rule = new ReadabilityIssuesChecks()
        let data = InvalidDataFile |> List.reduce (+)
        let project = ProjType.Parse(data)
        (Rule :> RuleBase).ExecuteCheck(project, "path.csproj", List.toArray InvalidDataFile, "", "")
        let issues = (Rule :> RuleBase).GetIssues()
        Assert.That(issues.Length, Is.EqualTo(1))
        
    [<Test>]
    member test.``Reports CorrectNumber of Rule`` () =
        let Rule = new ReadabilityIssuesChecks()       
        Assert.That((Rule :> RuleBase).GetRules.Length, Is.EqualTo(1))
