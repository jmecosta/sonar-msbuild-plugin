namespace ProjectCheckerTask.Test

open System.IO
open System.Reflection

open NUnit.Framework
open Foq
open MSBuildHelper

type MSbuildHelpers() = 
    
    let assemblyRunningPath = Directory.GetParent(Assembly.GetExecutingAssembly().Location).ToString()

    //[<Test>]
    member test.``Should Load External Checkes`` () = 
        let checks = MSBuildHelper.LoadChecksFromPath(Path.Combine(assemblyRunningPath, "DummyChecks.dll"))
        Assert.That(checks.Length, Is.EqualTo(6))
