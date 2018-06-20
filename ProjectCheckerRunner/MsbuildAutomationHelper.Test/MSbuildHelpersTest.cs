using NUnit.Framework;
using System;
using System.IO;

namespace MSbuildHelpersTest
{
    public class MSbuildHelpersTest
    {
        readonly string executionFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", ""));

        [Test]
        public void GetsIncludeGraphs()
        {
            var pathSolution = Path.Combine(executionFolder, "TestData", "ConsoleApp");
            var solution = MSBuildHelper.PreProcessSolution("", "", Path.Combine(pathSolution, "ConsoleApp.sln"), true, false, "14.0");
            var guid = new Guid("C864A049-0A9E-4139-8217-DA58D9A3B73D");
            var project = solution.Projects[guid];
            var callGraph = MSBuildHelper.GetIncludeGraphForFile(Path.Combine(pathSolution, "ConsoleApp", "ConsoleApp.cpp"), project);
            Assert.That(callGraph.Node.Length, Is.EqualTo(1));
        }

        [Test]
        public void GetsRealIncludeGraphs()
        {
            var packagesFolder = @"C:\prod\structures\MSBuild\..\Packages";
            var pathSolution = Path.Combine(@"C:\prod\structures\Core\Drawings", "Drawings.sln");
            var data = MSBuildHelper.CreateTargetTree(@"C:\prod\structures\MSBuild\RunFast.msbuild", "BuildTSAndDotAppsNORPC", packagesFolder, string.Empty, false, string.Empty, string.Empty, true, "14.0");
            var solution = MSBuildHelper.PreProcessSolution("", "", pathSolution, true, false, "14.0");
            var guid = new Guid("C864A049-0A9E-4139-8217-DA58D9A3B73D");
            var project = solution.Projects[guid];
            var callGraph = MSBuildHelper.GetIncludeGraphForFile(Path.Combine(pathSolution, "ConsoleApp", "ConsoleApp.cpp"), project);
            Assert.That(callGraph.Node.Length, Is.EqualTo(1));
        }
    }
}
