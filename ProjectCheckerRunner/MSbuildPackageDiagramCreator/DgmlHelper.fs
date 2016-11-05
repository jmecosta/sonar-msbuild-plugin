module DgmlHelper

open System.IO
open System

let stylesAndEnding = """
  <Categories>
    <Category Id="Project" />
    <Category Id="Package" />
    <Category Id="Solution" />
    <Category Id="MSBuildTarget" />

    <Category Id="TargetDependency" />
    <Category Id="PackageDependency" />
    <Category Id="BuildDependency" />
    <Category Id="ProjectDependency" />
    <Category Id="HeaderDependency" />
    <Category Id="SolutionBuildDependency" />
  </Categories>
  <Styles>
    <Style TargetType="Node" GroupLabel="Project" ValueLabel="True">
      <Condition Expression="HasCategory('Project')" />
      <Setter Property="Background" Expression="Color.FromRgb(0,0,255)" />
    </Style>

    <Style TargetType="Node" GroupLabel="Package" ValueLabel="True">
      <Condition Expression="HasCategory('Package')" />
      <Setter Property="Background" Expression="Color.FromRgb(255,204,255)" />
    </Style>

    <Style TargetType="Node" GroupLabel="Solution" ValueLabel="True">
      <Condition Expression="HasCategory('Solution')" />
      <Setter Property="Background" Expression="Color.FromRgb(255,102,102)" />
    </Style>

    <Style TargetType="Node" GroupLabel="MSBuildTarget" ValueLabel="True">
      <Condition Expression="HasCategory('MSBuildTarget')" />
      <Setter Property="Background" Expression="Color.FromRgb(179,139,0)" />
    </Style>


    <Style TargetType="Link" GroupLabel="TargetDependency" ValueLabel="True" >
      <Condition Expression="HasCategory('TargetDependency')" />
      <Setter Property="Stroke" Expression="Color.FromRgb(18,205,205)" />
    </Style>

    <Style TargetType="Link" GroupLabel="PackageDependency" ValueLabel="True" >
      <Condition Expression="HasCategory('PackageDependency')" />
      <Setter Property="Stroke" Expression="Color.FromRgb(160,160,160)" /> 
    </Style>

    <Style TargetType="Link" GroupLabel="BuildDependency" ValueLabel="True" >
      <Condition Expression="HasCategory('BuildDependency')" />
      <Setter Property="Stroke" Expression="Color.FromRgb(30,144,255)" />
    </Style>

    <Style TargetType="Link" GroupLabel="SolutionBuildDependency" ValueLabel="True" >
      <Condition Expression="HasCategory('SolutionBuildDependency')" />
      <Setter Property="Stroke" Expression="Color.FromRgb(191,61,182)" />
    </Style>

    <Style TargetType="Link" GroupLabel="ProjectDependency" ValueLabel="True" >
      <Condition Expression="HasCategory('ProjectDependency')" />
      <Setter Property="Stroke" Expression="Color.FromRgb(0,0,204)" />
    </Style>

    <Style TargetType="Link" GroupLabel="HeaderDependency" ValueLabel="True" >
      <Condition Expression="HasCategory('HeaderDependency')" />
      <Setter Property="Stroke" Expression="Color.FromRgb(0,153,0)" />
    </Style>

  </Styles>
  </DirectedGraph>
"""

let header = """<?xml version="1.0" encoding="utf-8"?>
<DirectedGraph GraphDirection="LeftToRight" xmlns="http://schemas.microsoft.com/vs/2009/dgml">
"""

let WriteProjectLinks(project : ProjectTypes.Project, file : StreamWriter, config : ProjectTypes.ConfigurationXml.Configuration) = 

    if config.PlotProjectDependencies then
        for packageRef in project.ProjectReferences do
            let line = sprintf """ <Link Source="%s" Target="%s" Category="ProjectDependency" /> """ project.Name packageRef.Value.Name
            file.WriteLine(line)    

    if config.PlotPackagesDependecies then
        for packageRef in project.NugetReferences do
            let line = sprintf """ <Link Source="%s" Target="%s" Category="PackageDependency" /> """ project.Name packageRef
            file.WriteLine(line)
        
    if config.PlotHeaderDependency then
        for headerRef in project.HeaderReferences do
            let line = sprintf """ <Link Source="%s" Target="%s" Category="HeaderDependency" /> """ project.Name headerRef.Value.Name
            file.WriteLine(line)                 

    if config.PlotSolutionBuildDependencies then
        for packageRef in project.SolutionInternalBuildDepencies do
            if packageRef.Value.Guid <> Guid.Empty then
                let line = sprintf """ <Link Source="%s" Target="%s" Category="BuildDependency" /> """ project.Name packageRef.Value.Name
                file.WriteLine(line)

let WriteSolutionLinks(solution : ProjectTypes.Solution, file : StreamWriter, config : ProjectTypes.ConfigurationXml.Configuration) = 

    for solutionNode in solution.SolutionExternalBuildDepencies do
        let line = sprintf """ <Link Source="%s" Target="%s" Category="SolutionBuildDependency" /> """ solution.Name solutionNode.Value.Name
        file.WriteLine(line)

    for project in solution.Projects do
        let line = sprintf """ <Link Source="%s" Target="%s" Category="Contains" /> """ solution.Name project.Value.Name
        file.WriteLine(line)

        WriteProjectLinks(project.Value, file, config)

let writeMsbuildLinks(target : ProjectTypes.MsbuildTarget, file : StreamWriter, config : ProjectTypes.ConfigurationXml.Configuration) = 

    for dep in target.MsbuildTargetDependencies do
        let line = sprintf """ <Link Source="%s" Target="%s" Category="TargetDependency" /> """ target.Name dep.Value.Name
        file.WriteLine(line)    
             
    for solution in target.Children do
        let line = sprintf """ <Link Source="%s" Target="%s" Category="Contains" /> """ target.Name solution.Value.Name
        file.WriteLine(line)
        WriteSolutionLinks(solution.Value, file, config)

let WriteSolutionNodes(solution : ProjectTypes.Solution, file : StreamWriter, config : ProjectTypes.ConfigurationXml.Configuration) = 
    let line = sprintf """ <Node Id="%s" Group="Collapsed" Category="Solution" /> """ solution.Name
    file.WriteLine(line)

    for project in solution.Projects do
        if project.Value.Visible then
            let line = sprintf """ <Node Id="%s" Label="%s" Category="Project" /> """ project.Value.Name project.Value.Name
            file.WriteLine(line)

            if config.PlotPackagesDependecies then
                for packageRef in project.Value.NugetReferences do
                    let line = sprintf """ <Node Id="%s" Label="%s" Category="Package" /> """ packageRef packageRef
                    file.WriteLine(line)                                     
        else
            let line = sprintf """ <Node Id="%s" Label="%s" Category="Project" Visibility="Hidden"/> """ project.Value.Name project.Value.Name
            file.WriteLine(line)

                                      
let WriteDgmlSolutionDocument(path : string, solutionlist : ProjectTypes.Solution List, config : ProjectTypes.ConfigurationXml.Configuration) =
    let outFile = new StreamWriter(path)
    outFile.Write(header)

    let line = sprintf """<Nodes>"""
    outFile.WriteLine(line)

    for solution in solutionlist do
        WriteSolutionNodes(solution, outFile, config)

    let line = sprintf """</Nodes>"""
    outFile.WriteLine(line)
      
    let line = sprintf """<Links>"""
    outFile.WriteLine(line)

    for solution in solutionlist do
        WriteSolutionLinks(solution, outFile, config)

    let line = sprintf """</Links>"""
    outFile.WriteLine(line)

    outFile.Write(stylesAndEnding)

    outFile.Flush()
    outFile.Close()

let writeMsbuildNodes(target : ProjectTypes.MsbuildTarget, file : StreamWriter, config : ProjectTypes.ConfigurationXml.Configuration) = 

    if target.Children.Count = 0 then
        let line = sprintf """ <Node Id="%s" Group="Collapsed" Category="MSBuildTarget" Visibility="Hidden"/> """ target.Name
        file.WriteLine(line)
    else
        let line = sprintf """ <Node Id="%s" Group="Collapsed" Category="MSBuildTarget" /> """ target.Name
        file.WriteLine(line)


    for dep in target.MsbuildTargetDependencies do
        let line = sprintf """ <Node Id="%s" Label="%s" Category="MSBuildTarget" /> """ dep.Value.Name dep.Value.Name
        file.WriteLine(line)
             
    for solution in target.Children do
        WriteSolutionNodes(solution.Value, file, config)


    
let WriteDgmlTargetDocument(path : string, targetList : ProjectTypes.MsbuildTarget List, config : ProjectTypes.ConfigurationXml.Configuration) =
    let outFile = new StreamWriter(path)
    outFile.Write(header)

    let line = sprintf """<Nodes>"""
    outFile.WriteLine(line)

    for target in targetList do
        writeMsbuildNodes(target, outFile, config)

    let line = sprintf """</Nodes>"""
    outFile.WriteLine(line)
      
    let line = sprintf """<Links>"""
    outFile.WriteLine(line)

    for target in targetList do
        writeMsbuildLinks(target, outFile, config)

    let line = sprintf """</Links>"""
    outFile.WriteLine(line)

    outFile.Write(stylesAndEnding)

    outFile.Flush()
    outFile.Close()
