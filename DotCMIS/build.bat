@echo off

rem This batch file creates the Debug DLL, the Release DLL and the documentation.
rem It requires the .NET Framework 3.5, Sandcastle and Sandcastle Help File Builder.

echo Building Debug DLL...
msbuild DotCMIS.csproj /ToolsVersion:3.5 /p:Configuration=Debug

echo Building Release DLL...
msbuild DotCMIS.csproj /ToolsVersion:3.5 /p:Configuration=Release

echo Building documentation...
msbuild DotCMIS.shfbproj /ToolsVersion:3.5 /p:Configuration=Release