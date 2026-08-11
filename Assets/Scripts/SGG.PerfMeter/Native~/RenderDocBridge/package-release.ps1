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

function Get-CMakeCacheValue([string]$CachePath, [string]$Name) {
    $match = Select-String -LiteralPath $CachePath -Pattern ("^" + [Regex]::Escape($Name) + ":[^=]+=(.*)$")
    if ($null -eq $match -or $match.Matches.Count -ne 1) {
        throw "Required CMake cache entry is missing or ambiguous: $Name"
    }
    return $match.Matches[0].Groups[1].Value.Trim()
}

function Invoke-Checked([string]$Executable, [string[]]$Arguments, [string]$Description) {
    & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed with exit code $LASTEXITCODE."
    }
}

$packageRootPath = (Resolve-Path -LiteralPath $PackageRoot).Path
$buildDirectoryPath = [System.IO.Path]::GetFullPath($BuildDirectory)
$packageJsonPath = Join-Path $packageRootPath "package.json"
if (-not (Test-Path -LiteralPath $packageJsonPath -PathType Leaf)) {
    throw "Package manifest not found: $packageJsonPath"
}

$packageManifest = Get-Content -LiteralPath $packageJsonPath -Raw | ConvertFrom-Json
if ($packageManifest.version -ne $PackageVersion) {
    throw "Package version '$($packageManifest.version)' does not match artifact version '$PackageVersion'."
}
if ($PackageVersion -notmatch '^(\d+)\.(\d+)\.(\d+)-(\d+)$') {
    throw "Package version must use the calendar release form YYYY.M.D-N."
}
$expectedFileVersion = "$($Matches[1]),$($Matches[2]),$($Matches[3]),$($Matches[4])"

$repoRoot = (& git -C $packageRootPath rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repoRoot)) {
    throw "Package root is not inside a Git worktree."
}
$headCommit = (& git -C $repoRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $headCommit -ne $SourceCommit) {
    throw "SourceCommit '$SourceCommit' does not match clean Git HEAD '$headCommit'."
}
$worktreeChanges = @(& git -C $repoRoot status --porcelain --untracked-files=all)
if ($LASTEXITCODE -ne 0 -or $worktreeChanges.Count -ne 0) {
    throw "Release artifact packaging requires a clean Git worktree."
}

if (Test-Path -LiteralPath $buildDirectoryPath) {
    if (-not (Test-Path -LiteralPath $buildDirectoryPath -PathType Container)) {
        throw "BuildDirectory must be a directory: $buildDirectoryPath"
    }
    if (@(Get-ChildItem -LiteralPath $buildDirectoryPath -Force).Count -ne 0) {
        throw "Release artifact packaging requires a new empty BuildDirectory: $buildDirectoryPath"
    }
}
else {
    $buildParent = Split-Path -Parent $buildDirectoryPath
    if (-not (Test-Path -LiteralPath $buildParent -PathType Container)) {
        throw "BuildDirectory parent does not exist: $buildParent"
    }
    New-Item -ItemType Directory -Path $buildDirectoryPath | Out-Null
}

Invoke-Checked "cmake" @(
    "-S", $PSScriptRoot,
    "-B", $buildDirectoryPath,
    "-G", "Ninja",
    "-DCMAKE_BUILD_TYPE=Release",
    "-DBUILD_TESTING=ON",
    "-DSGG_RD_ARTIFACT_VERSION=$PackageVersion",
    "-DSGG_RD_FILE_VERSION=$expectedFileVersion") "Release bridge configure"

$cachePath = Join-Path $buildDirectoryPath "CMakeCache.txt"
if (-not (Test-Path -LiteralPath $cachePath -PathType Leaf)) {
    throw "CMake configure did not create its cache: $cachePath"
}
$cmakeSource = (Resolve-Path -LiteralPath (Get-CMakeCacheValue $cachePath "CMAKE_HOME_DIRECTORY")).Path
if ($cmakeSource -ne (Resolve-Path -LiteralPath $PSScriptRoot).Path) {
    throw "CMake build directory was configured from a different source tree: $cmakeSource"
}
if ((Get-CMakeCacheValue $cachePath "CMAKE_BUILD_TYPE") -ne "Release") {
    throw "CMake build type must be Release."
}
if ((Get-CMakeCacheValue $cachePath "BUILD_TESTING") -ne "ON") {
    throw "CMake BUILD_TESTING must be ON."
}
if ((Get-CMakeCacheValue $cachePath "SGG_RD_ARTIFACT_VERSION") -ne $PackageVersion) {
    throw "CMake artifact version does not match $PackageVersion."
}
if ((Get-CMakeCacheValue $cachePath "SGG_RD_FILE_VERSION") -ne $expectedFileVersion) {
    throw "CMake file version does not match $expectedFileVersion."
}
$compilerPath = Get-CMakeCacheValue $cachePath "CMAKE_CXX_COMPILER"
if ($compilerPath -notmatch '[\\/]Hostx64[\\/]x64[\\/]cl\.exe$') {
    throw "Release artifact requires the MSVC Hostx64/x64 compiler: $compilerPath"
}

$renderDocHeaderPath = Join-Path $buildDirectoryPath "_deps\renderdoc\renderdoc_app.h"
if (-not (Test-Path -LiteralPath $renderDocHeaderPath -PathType Leaf) -or
    (Get-Sha256 $renderDocHeaderPath) -ne "b7005e7dc34c3635046868bbd76d81b9b055aede0f56daa0bd39fedee0639ffb") {
    throw "Configured build does not contain the exact pinned RenderDoc app header."
}

Invoke-Checked "cmake" @("--build", $buildDirectoryPath, "--config", "Release") "Release bridge build"
Invoke-Checked "ctest" @("--test-dir", $buildDirectoryPath, "--build-config", "Release", "--output-on-failure") "Release bridge tests"

if (-not (Test-Path -LiteralPath $dllPath -PathType Leaf)) {
    throw "Bridge DLL not found: $dllPath"
}

$dumpbinPath = Join-Path (Split-Path -Parent $compilerPath) "dumpbin.exe"
if (-not (Test-Path -LiteralPath $dumpbinPath -PathType Leaf)) {
    throw "MSVC dumpbin not found beside the configured compiler: $dumpbinPath"
}
$headers = (& $dumpbinPath /headers $dllPath 2>&1) -join "`n"
if ($LASTEXITCODE -ne 0 -or $headers -notmatch '(?m)^\s*8664 machine \(x64\)\s*$' -or $headers -notmatch '(?m)^\s*DLL\s*$') {
    throw "Bridge output is not an AMD64 native DLL."
}
$versionInfo = (Get-Item -LiteralPath $dllPath).VersionInfo
if ($versionInfo.FileVersion -ne $PackageVersion -or $versionInfo.ProductVersion -ne $PackageVersion) {
    throw "Embedded bridge version does not match $PackageVersion."
}

$expectedExports = @(
    "SggRd_GetCapabilitiesV1",
    "SggRd_BeginCaptureV1",
    "SggRd_EndCaptureV1",
    "SggRd_DiscardCaptureV1",
    "SggRd_TryGetNewArtifactV1",
    "SggRd_SetCaptureCommentsV1"
)
$exports = (& $dumpbinPath /exports $dllPath 2>&1) -join "`n"
if ($LASTEXITCODE -ne 0 -or $exports -notmatch '(?m)^\s*6 number of functions\s*$' -or $exports -notmatch '(?m)^\s*6 number of names\s*$') {
    throw "Bridge output does not expose exactly six named exports."
}
foreach ($export in $expectedExports) {
    if ($exports -notmatch ("(?m)^\s*\d+\s+[0-9A-F]+\s+[0-9A-F]+\s+" + [Regex]::Escape($export) + "\s*$")) {
        throw "Required bridge export is missing: $export"
    }
}

$dependents = (& $dumpbinPath /dependents $dllPath 2>&1) -join "`n"
$dependencyNames = @([Regex]::Matches($dependents, '(?mi)^\s*([A-Z0-9_.-]+\.dll)\s*$') |
    ForEach-Object { $_.Groups[1].Value.ToUpperInvariant() })
if ($LASTEXITCODE -ne 0 -or $dependencyNames.Count -ne 1 -or $dependencyNames[0] -ne "KERNEL32.DLL") {
    throw "Bridge output does not satisfy the static MSVC runtime dependency policy."
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
    exports = $expectedExports
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
                $entry = $archive.CreateEntry($_.Name, [System.IO.Compression.CompressionLevel]::NoCompression)
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
[System.IO.File]::WriteAllText(
    $checksumPath,
    "$archiveSha256  $artifactName.zip`n",
    [System.Text.Encoding]::ASCII)
Write-Output ([ordered]@{
    archive = $archivePath
    archive_sha256 = $archiveSha256
    dll = $stagedDll
    dll_sha256 = $dllSha256
    dll_bytes = $dllInfo.Length
    checksum = $checksumPath
} | ConvertTo-Json -Compress)
