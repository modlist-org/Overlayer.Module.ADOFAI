# Builds Overlayer (Release_ML, the module compiles against it), then the ADOFAI module, and copies both into the game (UserLibs + UserData).
# GamePath/GameData come from .\Directory.Build.props, else ..\Overlayer\Directory.Build.props.
#   .\build.ps1
#   .\build.ps1 Debug
param(
    [string]$Configuration = 'Release',
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$BuildArgs
)
$ErrorActionPreference = 'Stop'

$Root = $PSScriptRoot
$Props = Join-Path $Root 'Directory.Build.props'
if (-not (Test-Path $Props)) { $Props = Join-Path $Root '../Overlayer/Directory.Build.props' }
if (-not (Test-Path $Props)) {
    Write-Error 'Directory.Build.props not found (checked root and ../Overlayer). Copy ../Overlayer/Directory.Build.example.props to one of those and set GamePath first.'
}
$PropsPath = (Resolve-Path $Props).Path
dotnet build (Join-Path $Root '../Overlayer/Overlayer/Overlayer.csproj') -c Release_ML "-p:DirectoryBuildPropsPath=$PropsPath"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build (Join-Path $Root 'ADOFAI/Overlayer.Module.ADOFAI.csproj') -c $Configuration "-p:DirectoryBuildPropsPath=$PropsPath" @BuildArgs
exit $LASTEXITCODE
