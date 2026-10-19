param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [string]$ArchiveSha256,

    [Parameter(Mandatory = $true)]
    [string]$ExecutableSha256,

    [Parameter(Mandatory = $true)]
    [string]$DestinationRoot
)

$ErrorActionPreference = 'Stop'
$archiveName = "Azure.Functions.Cli.win-x64.$Version.zip"
$archivePath = Join-Path $DestinationRoot $archiveName
$destination = Join-Path $DestinationRoot 'AzureFunctionsCoreTools'

Invoke-WebRequest -Uri "https://github.com/Azure/azure-functions-core-tools/releases/download/$Version/$archiveName" -OutFile $archivePath
if ((Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash -ne $ArchiveSha256) {
    throw 'Azure Functions Core Tools archive does not match the approved release digest.'
}

Expand-Archive -LiteralPath $archivePath -DestinationPath $destination
Remove-Item -LiteralPath $archivePath

$exePath = Join-Path $destination 'func.exe'
if ((Get-FileHash -LiteralPath $exePath -Algorithm SHA256).Hash -ne $ExecutableSha256) {
    throw 'Azure Functions Core Tools executable does not match the approved digest.'
}

$signature = Get-AuthenticodeSignature -LiteralPath $exePath
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch '(^|, )O=Microsoft Corporation(,|$)') {
    throw 'Azure Functions Core Tools executable does not have a valid Microsoft signature.'
}

Write-Host "##vso[task.setvariable variable=FunctionsHostExePath]$exePath"
