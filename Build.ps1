[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $RailroaderPath,
    [string] $OutputDirectory = (Join-Path $PSScriptRoot 'dist')
)

$ErrorActionPreference = 'Stop'
$game = (Resolve-Path -LiteralPath $RailroaderPath).Path
$managed = Join-Path $game 'Railroader_Data\Managed'
$sdk = dotnet --list-sdks |
    ForEach-Object {
        if ($_ -match '^(\S+) \[(.*)\]$') {
            $candidate = Join-Path $Matches[2] "$($Matches[1])\Roslyn\bincore\csc.dll"
            if (Test-Path -LiteralPath $candidate) { $candidate }
        }
    } |
    Select-Object -Last 1
if (!$sdk) { throw 'A .NET SDK with the Roslyn compiler is required.' }

$references = @(
    'mscorlib.dll'
    'System.dll'
    'System.Core.dll'
    'netstandard.dll'
    'System.Runtime.dll'
    'Railroader.Modding.dll'
    'Railroader.dll'
    'Heathen.Steamworks.dll'
    'com.rlabrecque.steamworks.net.dll'
    '0Harmony.dll'
    'Unity.TextMeshPro.dll'
    'UnityEngine.CoreModule.dll'
    'UnityEngine.TextRenderingModule.dll'
    'UnityEngine.TextCoreTextEngineModule.dll'
    'UnityEngine.UIModule.dll'
    'UnityEngine.UI.dll'
) | ForEach-Object {
    $path = Join-Path $managed $_
    if (!(Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing game assembly: $path" }
    "/reference:`"$path`""
}
$pack = (New-Item -Path (Join-Path $OutputDirectory 'EnusDE.MultiplayerBrowser') -ItemType Directory -Force).FullName
$response = Join-Path (Split-Path -Parent $pack) 'compile.rsp'
$sources = Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' |
    Sort-Object Name | ForEach-Object { "`"$($_.FullName)`"" }
@('/nologo', '/nostdlib+', '/target:library', '/langversion:9', '/warnaserror+', '/optimize+',
    '/deterministic+', "/out:`"$(Join-Path $pack 'EnusDE.MultiplayerBrowser.dll')`"") +
    $references + $sources | Set-Content -LiteralPath $response -Encoding UTF8
try {
    dotnet $sdk "@$response"
    if ($LASTEXITCODE -ne 0) { throw "Compiler failed with exit code $LASTEXITCODE." }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'pack\Info.json') -Destination $pack
    Write-Output $pack
}
finally {
    Remove-Item -LiteralPath $response -ErrorAction SilentlyContinue
}
