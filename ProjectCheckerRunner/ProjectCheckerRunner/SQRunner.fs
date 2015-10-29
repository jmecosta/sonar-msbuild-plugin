namespace ProjectCheckerTask

open System
open System.Text
open System.IO
open System.Xml
open System.Xml.Linq
open RuleBase
open ProjectTypes
open ProjectCheckerTask

type SQAnalyser() =
            
    let mutable resources : SonarResoureMetrics List = List.Empty
    let mutable externalDlls : string List = List.Empty
    let mutable ingoreFolder : string List = List.Empty
    let resourcesLocker = new System.Object()

    member this.AddExternalAnalyser(path : string) =  
        if File.Exists(path) then
            externalDlls <- externalDlls @ [path]
        else
            raise(new Exception("External Path Does Not Exist: " + path))

    member this.AddIgnoreIncludeFolder(path : string) =  
        if File.Exists(path) then
            ingoreFolder <- ingoreFolder @ [path]
        else
            raise(new Exception("Include folder to ingore does Not Exist: " + path))

            
    member this.RunAnalysesForOutput(path : string) =  
        let analyser = new ProjectCheckerTask()
        analyser.ExecuteAnalysisOnProjectFile(path, "")
        analyser.PrintResults()

    member this.RunAnalyses(path : string) =  
        let resourceMetric = SonarResoureMetrics(path)
        resourceMetric.Issues <- this.RunTool(path)       
        lock resourcesLocker (fun () -> resources <- resources @ [resourceMetric] )

    member this.RunTool(path : string) = 
        try
            let analyser = new ProjectCheckerTask()
            analyser.ExternalDlls <- externalDlls
            analyser.IncludeFoldersToIgnore <- ingoreFolder            
            analyser.ExecuteAnalysisOnProjectFile(path, "")
            analyser.GetAllIssues()
        with
        | ex -> printf "Lint Execution Failed %A" ex
                List.Empty

    member this.WriteXmlToDisk(xmlOutPath : string) = 
        let xmlOutSettings = new XmlWriterSettings(Encoding = Encoding.UTF8, Indent = true, IndentChars = "  ")
        use xmlOut = XmlWriter.Create(xmlOutPath, xmlOutSettings)
        xmlOut.WriteStartElement("AnalysisOutput") // 1
        xmlOut.WriteStartElement("Files") // 2

        for resource in resources do
            xmlOut.WriteStartElement("File") // 3
            xmlOut.WriteElementString("Path", resource.ResourcePath)

            xmlOut.WriteStartElement("Issues") // 6 

            resource.Issues |> Seq.iter (fun diagnostic -> 
                xmlOut.WriteStartElement("Issue")
                xmlOut.WriteElementString("Id", sprintf "%s" diagnostic.Rule)
                xmlOut.WriteElementString("Line", sprintf "%i" diagnostic.Line)
                xmlOut.WriteElementString("Message", sprintf "%s" diagnostic.Message)
                xmlOut.WriteEndElement()
                )
                
            xmlOut.WriteEndElement()    // 6
                                                         
            xmlOut.WriteEndElement() // 3

        xmlOut.WriteEndElement() // 2
        xmlOut.WriteEndElement() // 1

        xmlOut.WriteEndDocument()
        xmlOut.Flush()
