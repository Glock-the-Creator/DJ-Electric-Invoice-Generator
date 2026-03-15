param(
    [string]$Version,
    [string]$UpdateFeedUrl,
    [string]$ReleaseNotes = "Latest release.",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

function Get-RepoRoot {
    return Split-Path -Parent $PSScriptRoot
}

function Get-VersionNode {
    param(
        [xml]$Xml,
        [string]$NodeName
    )

    return $Xml.SelectSingleNode("//$NodeName")
}

function Set-ReleaseVersion {
    param(
        [string]$PropsPath,
        [string]$NewVersion
    )

    [xml]$xml = Get-Content -Path $PropsPath
    $versionNode = Get-VersionNode -Xml $xml -NodeName "Version"
    $assemblyNode = Get-VersionNode -Xml $xml -NodeName "AssemblyVersion"
    $fileNode = Get-VersionNode -Xml $xml -NodeName "FileVersion"
    $infoNode = Get-VersionNode -Xml $xml -NodeName "InformationalVersion"

    if (-not $versionNode -or -not $assemblyNode -or -not $fileNode -or -not $infoNode) {
        throw "Directory.Build.props is missing one of the version nodes."
    }

    $parts = $NewVersion.Split(".")
    if ($parts.Count -eq 3) {
        $assemblyVersion = "$NewVersion.0"
    }
    elseif ($parts.Count -eq 4) {
        $assemblyVersion = $NewVersion
    }
    else {
        throw "Version must look like 1.2.3 or 1.2.3.4."
    }

    $versionNode.InnerText = $NewVersion
    $assemblyNode.InnerText = $assemblyVersion
    $fileNode.InnerText = $assemblyVersion
    $infoNode.InnerText = $NewVersion
    $xml.Save($PropsPath)
}

function Get-ReleaseVersion {
    param([string]$PropsPath)

    [xml]$xml = Get-Content -Path $PropsPath
    $versionNode = Get-VersionNode -Xml $xml -NodeName "Version"
    if (-not $versionNode) {
        throw "Directory.Build.props is missing the Version node."
    }

    return $versionNode.InnerText
}

function Reset-Path {
    param([string]$PathValue)

    if (Test-Path $PathValue) {
        Remove-Item -Path $PathValue -Recurse -Force
    }
}

function Compress-DirectoryContents {
    param(
        [string]$SourceDirectory,
        [string]$DestinationZip
    )

    if (Test-Path $DestinationZip) {
        Remove-Item -Path $DestinationZip -Force
    }

    $items = Get-ChildItem -Path $SourceDirectory
    if (-not $items) {
        throw "Nothing was found to package in $SourceDirectory."
    }

    Compress-Archive -Path $items.FullName -DestinationPath $DestinationZip
}

function Invoke-Dotnet {
    param(
        [string[]]$Arguments
    )

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments[0]) failed with exit code $LASTEXITCODE."
    }
}

$repoRoot = Get-RepoRoot
$propsPath = Join-Path $repoRoot "Directory.Build.props"
$appProject = Join-Path $repoRoot "DJ Electric Invoice Generator\DJ Electric Invoice Generator.csproj"
$installerProject = Join-Path $repoRoot "DJ Electric Installer\DJ Electric Installer.csproj"
$artifactsRoot = Join-Path $repoRoot "artifacts\release"
$appPublishDir = Join-Path $artifactsRoot "app-publish"
$setupPublishDir = Join-Path $artifactsRoot "setup-publish"
$setupBundleDir = Join-Path $artifactsRoot "setup-bundle"
$docsDir = Join-Path $repoRoot "docs"
$downloadsDir = Join-Path $docsDir "downloads"
$packageZipPath = Join-Path $downloadsDir "app-package.zip"
$setupZipPath = Join-Path $downloadsDir "DJ-Electric-Invoice-Generator-Setup.zip"
$setupExePath = Join-Path $setupPublishDir "DJ Electric Invoice Generator Setup.exe"

if ($Version) {
    Set-ReleaseVersion -PropsPath $propsPath -NewVersion $Version
}

$resolvedVersion = Get-ReleaseVersion -PropsPath $propsPath

New-Item -ItemType Directory -Force -Path $artifactsRoot | Out-Null
New-Item -ItemType Directory -Force -Path $downloadsDir | Out-Null

Reset-Path -PathValue $appPublishDir
Reset-Path -PathValue $setupPublishDir
Reset-Path -PathValue $setupBundleDir

$appPublishArgs = @(
    "publish",
    $appProject,
    "-c", "Release",
    "-r", $Runtime,
    "--self-contained", "false",
    "-p:PublishTrimmed=false",
    "-p:PublishReadyToRun=false",
    "-p:DebugType=None",
    "-p:DebugSymbols=false",
    "-p:UseAppHost=true",
    "-nologo",
    "-o", $appPublishDir
)

if ($UpdateFeedUrl) {
    $appPublishArgs += "-p:UpdateFeedUrl=$UpdateFeedUrl"
}

Invoke-Dotnet -Arguments $appPublishArgs

$setupPublishArgs = @(
    "publish",
    $installerProject,
    "-c", "Release",
    "-r", $Runtime,
    "--self-contained", "false",
    "-p:PublishTrimmed=false",
    "-p:PublishReadyToRun=false",
    "-p:DebugType=None",
    "-p:DebugSymbols=false",
    "-p:UseAppHost=true",
    "-nologo",
    "-o", $setupPublishDir
)

Invoke-Dotnet -Arguments $setupPublishArgs

Compress-DirectoryContents -SourceDirectory $appPublishDir -DestinationZip $packageZipPath

New-Item -ItemType Directory -Force -Path $setupBundleDir | Out-Null
Copy-Item -Path (Join-Path $setupPublishDir "*") -Destination $setupBundleDir -Recurse
Copy-Item -Path $packageZipPath -Destination (Join-Path $setupBundleDir "app-package.zip")
Compress-DirectoryContents -SourceDirectory $setupBundleDir -DestinationZip $setupZipPath

$releaseManifest = [ordered]@{
    productName    = "DJ Electric Invoice Generator"
    version        = $resolvedVersion
    publishedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    packageUrl     = "downloads/app-package.zip?v=$resolvedVersion"
    installerUrl   = "downloads/DJ-Electric-Invoice-Generator-Setup.zip?v=$resolvedVersion"
    releaseNotes   = $ReleaseNotes
}

$releaseManifest | ConvertTo-Json | Set-Content -Path (Join-Path $docsDir "release.json") -Encoding UTF8

Write-Host "Release $resolvedVersion packaged."
Write-Host "Website files: $docsDir"
Write-Host "Setup download: $setupZipPath"
Write-Host "Update package: $packageZipPath"
