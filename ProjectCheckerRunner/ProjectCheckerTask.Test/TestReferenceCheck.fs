namespace ProjectCheckerTask.Test

open NUnit.Framework
open Foq
open FSharp.Data
open System.Xml.Linq
open System.IO
open ProjectCheckerTask
open System.Reflection
open RuleBase
open ProjectTypes
type TestReferenceCheck() = 

    let assemblyRunningPath = Directory.GetParent(Assembly.GetExecutingAssembly().Location).ToString()

    [<Test>]
    member test.``Should Not Run for Know supported Project Type`` () = 
        let Rule = new ReferencesCheck()
        Assert.That((Rule :> RuleBase).SupportsProject("path.vsdcxproj"), Is.False)
        Assert.That((Rule :> RuleBase).SupportsProject("path.casproj"), Is.False)
        Assert.That((Rule :> RuleBase).SupportsProject("path.fspdroj"), Is.False)


    [<Test>]
    member test.``Should Run for Every Project Type`` () = 
        let Rule = new ReferencesCheck()
        Assert.That((Rule :> RuleBase).SupportsProject("path.vcxproj"), Is.True)
        Assert.That((Rule :> RuleBase).SupportsProject("path.csproj"), Is.True)
        Assert.That((Rule :> RuleBase).SupportsProject("path.fsproj"), Is.True)
       
    [<Test>]
    member test.``Reports CorrectNumber of Rule`` () =
        let Rule = new ReferencesCheck()       
        Assert.That((Rule :> RuleBase).GetRules.Length, Is.EqualTo(2))
