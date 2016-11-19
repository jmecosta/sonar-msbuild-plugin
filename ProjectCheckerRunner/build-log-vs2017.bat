@echo on
nuget\nuget restore ProjectChecker.sln
call "C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\Tools\VsMSBuildCmd.bat"
call "C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\Tools\VsDevCmd.bat"
msbuild ProjectChecker.sln /p:VisualStudioVersion=15.0 /p:VsVersion=15.0 /p:VsFolder=vs17  /p:Configuration=Release > buildlog2017.txt

