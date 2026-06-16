#Requires -Version 5
<#
.SYNOPSIS
    Cleaner installer for Windows.
.DESCRIPTION
    Downloads the latest Native AOT binary for your platform into ~\.cleaner\bin and adds it to PATH.

        irm https://raw.githubusercontent.com/suxrobGM/cleaner-cli/main/scripts/install.ps1 | iex
#>
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'   # Invoke-WebRequest is far faster without the progress bar.
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

$Repo = 'suxrobGM/cleaner-cli'
$InstallDir = Join-Path $HOME '.cleaner\bin'
$Headers = @{ 'User-Agent' = 'cleaner-installer'; 'Accept' = 'application/vnd.github+json' }

function Write-Info($message) { Write-Host $message -ForegroundColor Cyan }

# 1. Detect architecture -> runtime identifier (e.g. win-x64, win-arm64).
# Read the architecture from the env vars Windows always populates rather than
# [RuntimeInformation]::OSArchitecture: that property was added in .NET Framework 4.7.1, and on an
# older host PowerShell returns $null for the missing member (no error, even under -ErrorAction Stop),
# so the switch fell through to "Unsupported architecture:" with an empty value. PROCESSOR_ARCHITEW6432
# is set when a 32-bit process runs on 64-bit Windows and reports the true OS architecture.
$archRaw = if ($env:PROCESSOR_ARCHITEW6432) { $env:PROCESSOR_ARCHITEW6432 } else { $env:PROCESSOR_ARCHITECTURE }
$arch = switch ($archRaw) {
    'AMD64' { 'x64' }
    'ARM64' { 'arm64' }
    default { throw "Unsupported architecture: '$archRaw'. Cleaner ships win-x64 and win-arm64 builds; see https://github.com/$Repo/releases" }
}
$rid = "win-$arch"
Write-Info "Detected platform: $rid"

# 2. Resolve the matching asset on the latest release.
$release = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repo/releases/latest" -Headers $Headers
$asset = $release.assets | Where-Object { $_.name -like "*$rid*.zip" } | Select-Object -First 1
if (-not $asset) { throw "No release asset found for $rid. See https://github.com/$Repo/releases" }

# 3. Download and extract into a scratch dir.
$tmp = Join-Path ([System.IO.Path]::GetTempPath()) ("cleaner-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tmp -Force | Out-Null
try {
    Write-Info "Downloading $($asset.name)"
    $zip = Join-Path $tmp 'cleaner.zip'
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zip -Headers $Headers
    Expand-Archive -Path $zip -DestinationPath $tmp -Force

    $exe = Get-ChildItem -Path $tmp -Recurse -Filter 'cleaner.exe' | Select-Object -First 1
    if (-not $exe) { throw "The downloaded archive did not contain 'cleaner.exe'." }

    # 4. Install into ~\.cleaner\bin.
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
    Copy-Item -Path $exe.FullName -Destination (Join-Path $InstallDir 'cleaner.exe') -Force
    Write-Info "Installed to $InstallDir\cleaner.exe"
}
finally {
    Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue
}

# 5. Put ~\.cleaner\bin on the user PATH (idempotent).
$userPath = [Environment]::GetEnvironmentVariable('Path', 'User')
if (($userPath -split ';') -notcontains $InstallDir) {
    $newPath = if ([string]::IsNullOrEmpty($userPath)) { $InstallDir } else { "$userPath;$InstallDir" }
    [Environment]::SetEnvironmentVariable('Path', $newPath, 'User')
    $env:Path = "$env:Path;$InstallDir"
    Write-Info "Added $InstallDir to your user PATH - restart your terminal to use 'cleaner' everywhere."
}

Write-Host "`nDone. Run 'cleaner' to get started." -ForegroundColor Green
