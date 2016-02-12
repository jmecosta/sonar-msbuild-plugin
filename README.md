# sonar-msbuild-plugin

The plugin provides analysis for msbuild projects. 

[![Build status](https://ci.appveyor.com/api/projects/status/qwqbn68q2ejhd4xg/branch/master?svg=true)](https://ci.appveyor.com/project/jorgecosta/sonar-msbuild-plugin-7e5ti/branch/master)

# Description / Features

 - Metrics: LOC, number of classes, number of methics, complexity 
 - Code duplication detection
 - Own checker
 - Custom rules
 - Runs under windows and Linux (mono)
 - dgml creator, C++ nuget dependency support.
 
# Configuration
## General Configuration
  sonar.msbuild.file.suffixes - files extensions to import
  sonar.msbuild.include.folder.ignores - list of include folders to ignore during analysis

## Project Checker
  sonar.msbuild.projectchecker.customrules - list of dlls with rules to run
  sonar.msbuild.prjectChecker.enabled - enables disables project checker
  sonar.msbuild.prjectChecker.Path - external path for project checker
  
## Dgml creator
  sonar.msbuild.diagramCreator.enabled - enables disables diagram creation
  sonar.msbuild.diagramCreator.path - overrides internal creator 
  sonar.msbuild.packages.basePath - nuget repository path, absolute or relative
  sonar.msbuild.nuget.filter - filter out nuget packages
  sonar.msbuild.header.dependency.filter - filter out folders from header dependency
  sonar.msbuild.plot.header.dependencies - enables/disables header dependencies
  sonar.msbuild.plot.header.dependencies.inside.project - enables/disables header dependencies inside a solution
  sonar.msbuild.plot.project.dependencies - enables/disables project dependencies
  sonar.msbuild.plot.nuget.dependencies - enables/disable nuget dependencies
  sonar.msbuild.plot.solution.build.dependencies - enables/disable build dependencies
  sonar.msbuild.solution.node.filter - plot only the solutios defined in filter



