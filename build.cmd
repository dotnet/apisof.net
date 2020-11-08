@echo off

set "SLNDIR=%~dp0src"
dotnet build "%SLNDIR%\ApiCatalog.sln" --nologo || exit /b
