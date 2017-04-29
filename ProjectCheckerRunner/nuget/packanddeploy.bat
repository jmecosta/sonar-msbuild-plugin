del *.nupkg
nuget.exe pack MsbuildAutomationHelper431.nuspec
nuget.exe pack MsbuildAutomationHelper440.nuspec
nuget.exe pack MsbuildAutomationHelper441.nuspec
nuget.exe push *.nupkg -Source https://www.nuget.org/api/v2/package