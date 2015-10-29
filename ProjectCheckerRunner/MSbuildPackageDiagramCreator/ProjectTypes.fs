module ProjectTypes

open System
open System.Xml.Linq
open FSharp.Data

type ConfigurationXml = XmlProvider<"""<?xml version="1.0" encoding="utf-8"?>
<Configuration>
    <IgnoreNugetPackages>
        CppCheckTask;
        CppLintTask;
    </IgnoreNugetPackages>
    <PackageBasePath>d:/prod/structures/packages</PackageBasePath>
    <IgnoreIncludeFolders>
        d:/prod/src2/include;
        d:/prod/src2/include;
    </IgnoreIncludeFolders>    
    <PlotHeaderDependency>true</PlotHeaderDependency>
    <CheckRedundantIncludes>false</CheckRedundantIncludes>
    <PlotPackagesDependecies>false</PlotPackagesDependecies>
    <PlotProjectDependencies>false</PlotProjectDependencies>
    <PlotSolutionBuildDependencies>false</PlotSolutionBuildDependencies>
    <PlotHeaderDependencyInsideProject>false</PlotHeaderDependencyInsideProject>
    <PlotHeaderDependencFilter>
        SLN:Solution1;
        SLN:Solution2;
    </PlotHeaderDependencFilter>
  <PlotSolutionNodeFilter>
    SLN:Solution1;
    SLN:Solution2;
  </PlotSolutionNodeFilter>
</Configuration>""">

let DefaultConfigXml = """<?xml version="1.0" encoding="utf-8"?>
<Configuration>
    <IgnoreNugetPackages></IgnoreNugetPackages>
    <PackageBasePath></PackageBasePath>
    <IgnoreIncludeFolders>s</IgnoreIncludeFolders>    
    <PlotHeaderDependency>false</PlotHeaderDependency>
    <CheckRedundantIncludes>false</CheckRedundantIncludes>
    <PlotPackagesDependecies>false</PlotPackagesDependecies>
    <PlotProjectDependencies>true</PlotProjectDependencies>
    <PlotSolutionBuildDependencies>true</PlotSolutionBuildDependencies>
    <PlotHeaderDependencyInsideProject>false</PlotHeaderDependencyInsideProject>
    <PlotHeaderDependencFilter></PlotHeaderDependencFilter>
    <PlotSolutionNodeFilter></PlotSolutionNodeFilter>
</Configuration>"""