<#
.SYNOPSIS
    Repackages the official GraphViz Windows binaries as a NuGet package for the internal
    'apisofdotnet-tools' feed.

.DESCRIPTION
    The build consumes GraphViz via PackageReference so it never reaches gitlab.com, which
    violates 1ES Network Isolation (CFSClean2). See IcM 839785330.

    Run this from a developer machine, never from a pipeline - it downloads from gitlab.com.
    One-time per GraphViz version. The binaries packaged are the same ones the build used to
    download.

.EXAMPLE
    ./Publish-GraphVizPackage.ps1
    Builds the package locally so you can inspect it.

.EXAMPLE
    ./Publish-GraphVizPackage.ps1 -Push
    Builds and publishes to the feed.
#>
[CmdletBinding()]
param(
    [string]$Version = '6.0.2',
    [string]$ZipPath,
    [string]$FeedUrl = 'https://pkgs.dev.azure.com/devdiv/OnlineServices/_packaging/apisofdotnet-tools/nuget/v3/index.json',
    [string]$OutputDirectory,
    [switch]$Push
)

$ErrorActionPreference = 'Stop'

$packageId = 'ApisOfDotNet.GraphViz.Win32'

if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $PSScriptRoot 'artifacts'
}

$work = Join-Path ([System.IO.Path]::GetTempPath()) "graphviz-pack-$([Guid]::NewGuid().ToString('n'))"
$staging = Join-Path $work 'staging'
$projectDir = Join-Path $work 'project'

New-Item -ItemType Directory -Force -Path $staging, $projectDir, $OutputDirectory | Out-Null

try {
    if ($ZipPath) {
        if (-not (Test-Path -LiteralPath $ZipPath)) {
            throw "Zip not found: $ZipPath"
        }
        $zip = (Resolve-Path -LiteralPath $ZipPath).Path
        Write-Host "Using existing zip: $zip"
    }
    else {
        $zip = Join-Path $work 'GraphViz.zip'
        $url = "https://gitlab.com/api/v4/projects/4207231/packages/generic/graphviz-releases/$Version/windows_10_msbuild_Release_graphviz-$Version-win32.zip"
        Write-Host "Downloading $url"
        Write-Host '(Developer machine only - this is exactly the connection the build must not make.)'
        Invoke-WebRequest -Uri $url -OutFile $zip -UseBasicParsing
    }

    # The build's Copy globs depend on the top-level 'Graphviz' folder in this archive.
    Write-Host 'Extracting...'
    Expand-Archive -LiteralPath $zip -DestinationPath $staging -Force

    $graphVizRoot = Join-Path $staging 'Graphviz'
    if (-not (Test-Path -LiteralPath $graphVizRoot)) {
        $found = Get-ChildItem -LiteralPath $staging -Directory | Select-Object -ExpandProperty Name
        throw "Expected a 'Graphviz' folder inside the archive but found: $($found -join ', '). The upstream layout changed; update this script and the Copy globs in Directory.Build.targets together."
    }

    if (-not (Get-ChildItem -LiteralPath $graphVizRoot -Recurse -Filter 'dot.exe' | Select-Object -First 1)) {
        throw "dot.exe was not found under '$graphVizRoot'. NetUpgradePlanner shells out to Graphviz\dot.exe at runtime, so packaging without it would produce a silently broken app."
    }

    $fileCount = (Get-ChildItem -LiteralPath $graphVizRoot -Recurse -File).Count
    Write-Host "Staged $fileCount GraphViz files."

    $csproj = @"
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <EnableDefaultItems>false</EnableDefaultItems>
    <IncludeBuildOutput>false</IncludeBuildOutput>
    <SuppressDependenciesWhenPacking>true</SuppressDependenciesWhenPacking>
    <DevelopmentDependency>true</DevelopmentDependency>
    <NoWarn>NU5128</NoWarn>

    <PackageId>$packageId</PackageId>
    <Version>$Version</Version>
    <Authors>.NET Developer Community</Authors>
    <Description>Official GraphViz $Version win32 binaries, repackaged for internal consumption so builds do not reach gitlab.com. Build-time only.</Description>
    <PackageTags>graphviz;native;build</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <None Include="$graphVizRoot\**\*.*" Pack="true" PackagePath="tools\Graphviz" />
  </ItemGroup>

</Project>
"@

    $csprojPath = Join-Path $projectDir "$packageId.csproj"
    Set-Content -LiteralPath $csprojPath -Value $csproj -Encoding UTF8

    Write-Host 'Packing...'
    & dotnet pack $csprojPath --configuration Release --output $OutputDirectory
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet pack failed with exit code $LASTEXITCODE."
    }

    $nupkg = Join-Path $OutputDirectory "$packageId.$Version.nupkg"
    if (-not (Test-Path -LiteralPath $nupkg)) {
        throw "Expected package not produced: $nupkg"
    }
    Write-Host "Package created: $nupkg"

    if ($Push) {
        Write-Host "Pushing to $FeedUrl"
        & dotnet nuget push $nupkg --source $FeedUrl --api-key AzureArtifacts --interactive
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet nuget push failed with exit code $LASTEXITCODE."
        }
        Write-Host 'Published.'
    }
    else {
        Write-Host 'Re-run with -Push to publish to the feed.'
    }
}
finally {
    Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
}
