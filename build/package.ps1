<#
.SYNOPSIS
    Publishes Kluis.App and packs it into a Velopack installer. Used by CI and for local builds.
.EXAMPLE
    ./build/package.ps1 -Version 0.1.0
#>
param(
    [Parameter(Mandatory)] [string] $Version,
    [string] $OutputDir = 'Releases'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$publishDir = Join-Path $root 'publish'
$outputPath = Join-Path $root $OutputDir

# Self-contained: the installer carries the .NET runtime, so users install nothing else.
dotnet publish (Join-Path $root 'src/Kluis.App/Kluis.App.csproj') `
    --configuration Release `
    --runtime win-x64 `
    --self-contained `
    --output $publishDir
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

dotnet tool restore
if ($LASTEXITCODE -ne 0) { throw 'dotnet tool restore failed.' }

# The pack id decides the install folder, %LOCALAPPDATA%\<packId>, which the uninstaller deletes
# entirely. It must NOT be "Kluis": the encrypted database lives in %LOCALAPPDATA%\Kluis, and
# uninstalling the app must never delete the user's data.
dotnet vpk pack `
    --packId KluisApp `
    --packTitle Kluis `
    --packAuthors 'Kluis contributors' `
    --packVersion $Version `
    --packDir $publishDir `
    --mainExe Kluis.App.exe `
    --shortcuts StartMenuRoot `
    --outputDir $outputPath
if ($LASTEXITCODE -ne 0) { throw 'vpk pack failed.' }

Get-ChildItem $outputPath | Select-Object Name, Length
