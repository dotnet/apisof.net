@echo off

set "SLNDIR=%~dp0src"
dotnet run --project "%SLNDIR%\apisof.net\apisof.net.csproj" --nologo
