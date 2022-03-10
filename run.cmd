@echo off

set "SLNDIR=%~dp0src"
dotnet run --project "%SLNDIR%\ApiCatalogWeb\ApiCatalogWeb.csproj" --nologo
