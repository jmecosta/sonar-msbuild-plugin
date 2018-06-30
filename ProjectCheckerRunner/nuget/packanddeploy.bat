del *.nupkg
nuget.exe pack MsbuildAutomationHelper440.nuspec
nuget.exe pack MsbuildAutomationHelper443.nuspec
nuget.exe push *.nupkg -Source https://www.nuget.org/api/v2/package