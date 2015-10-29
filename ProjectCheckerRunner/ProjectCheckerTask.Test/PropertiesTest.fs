namespace ProjectCheckerTask.Test

open NUnit.Framework
open ProjectCheckerTask
open Foq

type ProjectCheckerPropertiesTest() = 
    [<Test>]
    member test.``Empty Project and Solution Should Throw Exception`` () = 
        let Task = new ProjectCheckerTask()
        Assert.Throws<System.InvalidOperationException>(fun c -> (Task.CheckBothProjectAndSolutionCannotEmpty.Force() |> ignore))  |> ignore

    [<Test>]
    member test.``Non Empty Solution Should Not Throw Exception`` () = 
        let Task = new ProjectCheckerTask()
        Task.SourceDir <- "paht1"
        Task.CheckBothProjectAndSolutionCannotEmpty.Force()

    [<Test>]
    member test.``Non Empty Project Should Not Throw Exception`` () = 
        let Task = new ProjectCheckerTask()
        Task.ProjectPath <- "paht1"
        Task.CheckBothProjectAndSolutionCannotEmpty.Force()

    [<Test>]
    member test.``when Both Project and Solution Are Defined It Should Throw Exception`` () = 
        let Task = new ProjectCheckerTask()
        Task.SourceDir <- "paht1"
        Task.ProjectPath <- "paht12"
        Assert.Throws<System.InvalidOperationException>(fun c -> (Task.CheckBothProjectAndSolutionCannotBeDefinedAtSameTime.Force() |> ignore)) |> ignore

    [<Test>]
    member test.``when only Project is defined there will be no exception`` () = 
        let Task = new ProjectCheckerTask()
        Task.SourceDir <- "paht1"
        Task.CheckBothProjectAndSolutionCannotBeDefinedAtSameTime.Force()

    [<Test>]
    member test.``when only Solution is defined there will be no exception`` () = 
        let Task = new ProjectCheckerTask()
        Task.ProjectPath <- "paht1"
        Task.CheckBothProjectAndSolutionCannotBeDefinedAtSameTime.Force()

    [<Test>]
    member test.``Should Throw Exception When Path are Not Found`` () = 
        let Task = new ProjectCheckerTask()
        Task.SourceDir <- "paht1"
        Task.ProjectPath <- "paht12"
        Assert.Throws<System.InvalidOperationException>(fun c -> (Task.VerifySolutionOrProjectExists.Force() |> ignore)) |> ignore

