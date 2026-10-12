@echo off
setlocal
cd /d "%~dp0"
dotnet publish DndEncounters.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "publish\win-x64"
if errorlevel 1 exit /b %errorlevel%
echo Build ready: publish\win-x64\DndEncounters.exe
pause
