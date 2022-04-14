@echo off
setlocal

set "SLNDIR=%~dp0src"
dotnet test "%SLNDIR%\apisof.net.sln" --nologo
