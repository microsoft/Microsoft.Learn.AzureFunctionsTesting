param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('VisualStudio', 'Npm', 'Homebrew')]
    [string]$Source,

    [ValidateSet('Process', 'User')]
    [string]$Scope = 'Process'
)

$ErrorActionPreference = 'Stop'
$onWindows = [Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::Windows)
$onMacOS = [Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::OSX)

if ($env:TF_BUILD -eq 'True') {
    throw 'This helper is for local development. CI must install and select its own verified Core Tools executable.'
}
if ($Scope -eq 'User' -and -not $onWindows) {
    throw 'User-scoped environment variables are only supported by this helper on Windows.'
}

$candidates = @()
switch ($Source) {
    'VisualStudio' {
        if (-not $onWindows) {
            throw 'Visual Studio Core Tools discovery is only supported on Windows.'
        }
        $releases = Join-Path $env:LOCALAPPDATA 'AzureFunctionsTools\Releases'
        if (Test-Path -LiteralPath $releases -PathType Container) {
            foreach ($release in (Get-ChildItem -LiteralPath $releases -Directory -Filter '4*' | Where-Object Name -Match '^4(\.|$)' | Sort-Object LastWriteTimeUtc -Descending)) {
                foreach ($cliDirectory in @('cli', 'cli_x64')) {
                    $candidate = Join-Path $release.FullName "$cliDirectory\func.exe"
                    if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                        $candidates += $candidate
                    }
                }
            }
        }
    }
    'Npm' {
        $npmRoot = & npm root -g
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($npmRoot)) {
            throw 'Could not locate the global npm installation. Check npm root -g.'
        }
        if ($onWindows -and [IO.Path]::GetFullPath($npmRoot) -match '^C:\\npm\\prefix(\\|$)') {
            throw 'The shared C:\npm\prefix installation is not trusted for local discovery.'
        }
        $exeName = if ($onWindows) { 'func.exe' } else { 'func' }
        $candidates = @(Join-Path $npmRoot "azure-functions-core-tools/bin/$exeName")
    }
    'Homebrew' {
        if (-not $onMacOS) {
            throw 'Homebrew Core Tools discovery is only supported on macOS.'
        }
        $brewPrefix = & brew --prefix azure-functions-core-tools@4
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($brewPrefix)) {
            throw 'Could not locate the Homebrew Core Tools installation.'
        }
        $candidates = @(Join-Path $brewPrefix 'bin/func')
    }
}

if ($candidates.Count -eq 0) {
    throw "No Core Tools v4 executable was found in the $Source installation."
}

if ($candidates.Count -gt 1) {
    for ($index = 0; $index -lt $candidates.Count; $index++) {
        Write-Host "$($index + 1). $($candidates[$index])"
    }
    $selection = Read-Host 'Select the installed Core Tools executable to trust'
    $choice = 0
    if (-not [int]::TryParse($selection, [ref]$choice) -or $choice -lt 1 -or $choice -gt $candidates.Count) {
        throw 'Select a number from the displayed list.'
    }
    $hostPath = $candidates[$choice - 1]
}
else {
    $hostPath = $candidates[0]
}

if (-not (Test-Path -LiteralPath $hostPath -PathType Leaf)) {
    throw "Core Tools executable does not exist at '$hostPath'."
}

$env:FunctionsHostExePath = [IO.Path]::GetFullPath($hostPath)
if ($Scope -eq 'User') {
    [Environment]::SetEnvironmentVariable('FunctionsHostExePath', $env:FunctionsHostExePath, 'User')
}
Write-Host "FunctionsHostExePath ($Scope): $env:FunctionsHostExePath"
