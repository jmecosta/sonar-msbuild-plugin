@echo on
nuget\nuget restore ProjectChecker.sln
call "C:\Program Files\Microsoft Visual Studio\18\Professional\Common7\Tools\VsMSBuildCmd.bat"
call "C:\Program Files\Microsoft Visual Studio\18\Professional\Common7\Tools\VsDevCmd.bat"
msbuild ProjectChecker.sln /p:VisualStudioVersion=18.0 /p:VsVersion=18.0 /p:VsFolder=vs26  /p:Configuration=Release > buildlog2026.txt

