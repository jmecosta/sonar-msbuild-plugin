# sonar-msbuild-plugin

The plugin provides analysis for msbuild projects. 

[![Build status](https://ci.appveyor.com/api/projects/status/qwqbn68q2ejhd4xg/branch/master?svg=true)](https://ci.appveyor.com/project/jorgecosta/sonar-msbuild-plugin-7e5ti/branch/master)

# Description / Features
 - Metrics: LOC
 - Project Checker that validates common mistakes developers and visual studio does
 - Custom rules that can be written using Project Checker API
 - Runs under windows and Linux (mono)
 - dgml creator, C++ nuget dependency support
 
# Installation
 1. Download latest release from https://github.com/jmecsoftware/sonar-msbuild-plugin/releases, sonar-msbuild-plugin-xxx.jar
 2. Copy to extensions/plugins folder in sonar installation
 
# Usage
Run a normal analysis using your favourite scanner. If you use SonarQubeMSBuild runner, its recommended to use the https://github.com/jmecsoftware/sonar-cxx-msbuild-tasks to allow project files to be imported.

## Configuration
### General Configuration
  sonar.msbuild.file.suffixes - files extensions to import
  sonar.msbuild.include.folder.ignores - list of include folders to ignore during analysis

### Project Checker
The project checked is squid sensor, responsible to find issues with msbuild project file.

These are the current configuration options: 
  1. sonar.msbuild.projectchecker.customrules - list of dlls with custom rules to run. see bellow on how to create rules
  2. sonar.msbuild.projectchecker.enabled - enables disables project checker
  3. sonar.msbuild.prjectChecker.Path - external path for project checker
  
### Dgml creator
The dgml creator is a small application that will try to map your build dependencies. It supports C++ files project files, and if nuget packages are used in c++ project files then they will be show in the dgml.

These are the current configuration options: 
  1. sonar.msbuild.diagramCreator.enabled - enables disables diagram creation
  2. sonar.msbuild.diagramCreator.path - overrides internal creator 
  3. sonar.msbuild.packages.basePath - nuget repository path, absolute or relative
  4. sonar.msbuild.nuget.filter - filter out nuget packages
  5. sonar.msbuild.header.dependency.filter - filter out folders from header dependency
  6. sonar.msbuild.plot.header.dependencies - enables/disables header dependencies
  7. sonar.msbuild.plot.header.dependencies.inside.project - enables/disables header dependencies inside a solution
  8. sonar.msbuild.plot.project.dependencies - enables/disables project dependencies
  9. sonar.msbuild.plot.nuget.dependencies - enables/disable nuget dependencies
  10. sonar.msbuild.plot.solution.build.dependencies - enables/disable build dependencies
  11. sonar.msbuild.solution.node.filter - plot only the solutios defined in filter

# How to create custom rules
Install the nuget package. MsbuildAutomationHelper 
 ```
 Install-Package MsbuildAutomationHelper
 ```
 
This is a small rule to verify if a project is correct:
```
namespace ProjectCheckerTask

open RuleBase
open ProjectTypes

type CannotReadProjectCheck() = 
    inherit RuleBase()
    let CannotReadProjectCheck =
        new Rule(Key = "CannotReadProjectError",
                 Description = "Cannot Read Project File Error")

    let mutable valid = false

    override this.SupportsProject(path) =
        true

    override this.ExecuteCheck(project, path, lines, solution, outputPath) =
        try
            project.ToolsVersion |> ignore            
        with
        | _ -> this.SaveIssue(path, 1, "Cannot Read File: " + path, CannotReadProjectCheck) 


    override this.GetRules =
        [CannotReadProjectCheck]  
```

