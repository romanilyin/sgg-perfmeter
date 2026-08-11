param(
    [Parameter(Mandatory = $true)]
    [string]$PackageRoot,
    [Parameter(Mandatory = $true)]
    [string]$BuildDirectory,
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory,
    [Parameter(Mandatory = $true)]
    [string]$PackageVersion,
    [Parameter(Mandatory = $true)]
    [string]$SourceCommit
)

$ErrorActionPreference = "Stop"
$dllName = "sgg_renderdoc_bridge.dll"
$artifactName = "sgg-perfmeter-renderdoc-bridge-$PackageVersion-windows-x64"
$dllPath = Join-Path $BuildDirectory $dllName
$stagingPath = Join-Path $OutputDirectory $artifactName
$archivePath = Join-Path $OutputDirectory "$artifactName.zip"
$checksumPath = Join-Path $OutputDirectory "$artifactName.sha256"

function Get-Sha256([string]$Path) {
    $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::Read)
    try {
        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        try {
            return ([BitConverter]::ToString($sha256.ComputeHash($stream))).Replace("-", "").ToLowerInvariant()
        }
        finally {
            $sha256.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

if (-not (Test-Path -LiteralPath $dllPath -PathType Leaf)) {
    throw "Bridge DLL not found: $dllPath"
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
Remove-Item -LiteralPath $stagingPath -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $archivePath, $checksumPath -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $stagingPath | Out-Null

$notices = @(
    @{ Source = Join-Path $PackageRoot "LICENSE.ru.md"; Destination = "LICENSE.ru.md" },
    @{ Source = Join-Path $PackageRoot "LICENSE.md"; Destination = "LICENSE.md" },
    @{ Source = Join-Path $PackageRoot "NOTICE.md"; Destination = "NOTICE.md" },
    @{ Source = Join-Path $PSScriptRoot "ThirdPartyNotices.md"; Destination = "ThirdPartyNotices.md" }
)

Copy-Item -LiteralPath $dllPath -Destination (Join-Path $stagingPath $dllName)
foreach ($notice in $notices) {
    if (-not (Test-Path -LiteralPath $notice.Source -PathType Leaf)) {
        throw "Required notice not found: $($notice.Source)"
    }
    Copy-Item -LiteralPath $notice.Source -Destination (Join-Path $stagingPath $notice.Destination)
}

$stagedDll = Join-Path $stagingPath $dllName
$dllInfo = Get-Item -LiteralPath $stagedDll
$dllSha256 = Get-Sha256 $stagedDll
$manifest = [ordered]@{
    schema_version = 1
    artifact_id = "sgg.perfmeter.renderdoc.bridge.windows-x64"
    artifact_version = $PackageVersion
    compatible_package_version = $PackageVersion
    filename = $dllName
    byte_length = $dllInfo.Length
    sha256 = $dllSha256
    bridge_abi_major = 1
    bridge_abi_minor = 0
    platform = "windows"
    architecture = "x86_64"
    pe_machine = "AMD64"
    build_configuration = "Release"
    msvc_runtime = "static"
    source_commit = $SourceCommit
    renderdoc_header_commit = "7db2264afa00a5313154022f8c4ae0628a641300"
    renderdoc_header_sha256 = "b7005e7dc34c3635046868bbd76d81b9b055aede0f56daa0bd39fedee0639ffb"
    exports = @(
        "SggRd_GetCapabilitiesV1",
        "SggRd_BeginCaptureV1",
        "SggRd_EndCaptureV1",
        "SggRd_DiscardCaptureV1",
        "SggRd_TryGetNewArtifactV1",
        "SggRd_SetCaptureCommentsV1"
    )
}
$manifestJson = $manifest | ConvertTo-Json -Depth 4
[System.IO.File]::WriteAllText(
    (Join-Path $stagingPath "artifact.json"),
    $manifestJson + [Environment]::NewLine,
    [System.Text.UTF8Encoding]::new($false))

Add-Type -AssemblyName System.IO.Compression
$archiveStream = [System.IO.File]::Open($archivePath, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
try {
    $archive = [System.IO.Compression.ZipArchive]::new($archiveStream, [System.IO.Compression.ZipArchiveMode]::Create, $true)
    try {
        $fixedTimestamp = [DateTimeOffset]::new(2000, 1, 1, 0, 0, 0, [TimeSpan]::Zero)
        Get-ChildItem -LiteralPath $stagingPath -File |
            Sort-Object Name |
            ForEach-Object {
                $entry = $archive.CreateEntry($_.Name, [System.IO.Compression.CompressionLevel]::Optimal)
                $entry.LastWriteTime = $fixedTimestamp
                $source = [System.IO.File]::OpenRead($_.FullName)
                $destination = $entry.Open()
                try {
                    $source.CopyTo($destination)
                }
                finally {
                    $destination.Dispose()
                    $source.Dispose()
                }
            }
    }
    finally {
        $archive.Dispose()
    }
}
finally {
    $archiveStream.Dispose()
}

$archiveSha256 = Get-Sha256 $archivePath
Set-Content -LiteralPath $checksumPath -Value "$archiveSha256  $artifactName.zip" -Encoding ascii
Write-Output ([ordered]@{
    archive = $archivePath
    archive_sha256 = $archiveSha256
    dll = $stagedDll
    dll_sha256 = $dllSha256
    dll_bytes = $dllInfo.Length
    checksum = $checksumPath
} | ConvertTo-Json -Compress)
