@echo off
setlocal

set "SLNDIR=%~dp0src"
set "CONFIG=%~1"
set "OUTPUT=%~2"

REM Default to Release if no configuration specified
if "%CONFIG%"=="" set "CONFIG=Release"

echo Building solution in %CONFIG% configuration...

REM Restore packages first
dotnet restore "%SLNDIR%\apisof.net.sln" --configfile "%SLNDIR%\nuget.config" --verbosity minimal

REM Build the solution
dotnet build "%SLNDIR%\apisof.net.sln" --configuration %CONFIG% --no-restore --nologo

if errorlevel 1 (
    echo Build failed!
    exit /b 1
)

echo Build completed successfully!
