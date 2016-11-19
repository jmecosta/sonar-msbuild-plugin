@echo on
nuget\nuget restore ProjectChecker.sln
call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\vsvars32.bat"
call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\VsDevCmd.bat"
msbuild ProjectChecker.sln /p:VisualStudioVersion=14.0 /p:VsVersion=14.0 /p:VsFolder=vs15  /p:Configuration=Release > buildlog2015.txt

