<#
.SYNOPSIS
    Refreshes the vendored GraphViz binaries in eng/graphviz.

.DESCRIPTION
    NetUpgradePlanner shells out to GraphViz at runtime, so the binaries ship with the app.
    They are committed to the repo rather than downloaded during the build: a build-time
    download from gitlab.com violates 1ES Network Isolation (CFSClean2) and stops the pipeline
    running on a network-isolated pool.

    Run this from a developer machine, never from a pipeline - it downloads from gitlab.com.
    It is only needed when moving to a new GraphViz release.

    Import libraries (.lib, .exp) are excluded: they are used when linking against GraphViz,
    never when running it, and account for roughly 12 MB.

    Files are stored flat, which is the layout the build copies into the app and the layout
    GraphViz expects when dot.exe and its plugins sit side by side.

.EXAMPLE
    ./Update-GraphViz.ps1 -Version 7.0.0
    Replaces eng/graphviz with GraphViz 7.0.0. Review and commit the result.
#>
[CmdletBinding()]
param(
    [string]$Version = '6.0.2',
    [string]$ZipPath
)

$ErrorActionPreference = 'Stop'

$destination = Join-Path $PSScriptRoot 'graphviz'
$work = Join-Path ([System.IO.Path]::GetTempPath()) "graphviz-$([Guid]::NewGuid().ToString('n'))"
New-Item -ItemType Directory -Force -Path $work | Out-Null

try {
    if ($ZipPath) {
        $zip = (Resolve-Path -LiteralPath $ZipPath).Path
    }
    else {
        $zip = Join-Path $work 'GraphViz.zip'
        $url = "https://gitlab.com/api/v4/projects/4207231/packages/generic/graphviz-releases/$Version/windows_10_msbuild_Release_graphviz-$Version-win32.zip"
        Write-Host "Downloading $url"
        Invoke-WebRequest -Uri $url -OutFile $zip -UseBasicParsing
    }

    Write-Host 'Extracting...'
    $staging = Join-Path $work 'staging'
    Expand-Archive -LiteralPath $zip -DestinationPath $staging -Force

    $root = Join-Path $staging 'Graphviz'
    if (-not (Test-Path -LiteralPath $root)) {
        throw "Expected a 'Graphviz' folder inside the archive. The upstream layout changed; update this script."
    }

    $files = Get-ChildItem -LiteralPath $root -Recurse -File |
        Where-Object { $_.Extension -notin '.lib', '.exp' }

    if (-not ($files | Where-Object Name -eq 'dot.exe')) {
        throw 'dot.exe was not found. NetUpgradePlanner invokes Graphviz\dot.exe at runtime, so this would ship a broken app.'
    }

    $collisions = $files | Group-Object Name | Where-Object Count -gt 1
    if ($collisions) {
        throw "Flattening would overwrite: $($collisions.Name -join ', '). The upstream layout changed; update this script."
    }

    if (Test-Path -LiteralPath $destination) {
        Remove-Item -LiteralPath $destination -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $destination | Out-Null

    $files | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $destination }

    $mb = [math]::Round((($files | Measure-Object Length -Sum).Sum) / 1MB, 2)
    Write-Host "Wrote $($files.Count) files ($mb MB) to $destination"

    Write-Host 'Verifying...'
    $svg = 'digraph { a -> b; }' | & (Join-Path $destination 'dot.exe') -Tsvg
    if ($LASTEXITCODE -ne 0 -or $svg -notmatch '<svg') {
        throw 'dot.exe did not render SVG. The vendored copy is incomplete.'
    }
    Write-Host 'GraphViz renders correctly. Review the diff and commit.'
}
finally {
    Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
}
