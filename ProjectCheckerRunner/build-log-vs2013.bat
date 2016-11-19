@echo on
nuget\nuget restore ProjectChecker.sln
call "C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\Tools\vsvars32.bat"
call "C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\Tools\VsDevCmd.bat"
msbuild ProjectChecker.sln /p:VisualStudioVersion=12.0 /p:VsVersion=12.0 /p:VsFolder=vs13  /p:Configuration=Release /v:diag > buildlog2013.txt

