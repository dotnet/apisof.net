@echo off

set "SLNDIR=%~dp0src"
dotnet build "%SLNDIR%\apisof.net.sln" --nologo
