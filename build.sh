#!/usr/bin/env bash
#
# Builds Overlayer (Release_ML, the module compiles against it), then the ADOFAI module, and copies both into the game (UserLibs + UserData).
# GamePath/GameData come from ./Directory.Build.props, else ../Overlayer/Directory.Build.props.
#   ./build.sh [Release|Debug] [dotnet build args...]
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CONFIG="${1:-Release}"
if [[ $# -gt 0 ]]; then shift; fi

PROPS="$ROOT/Directory.Build.props"
[[ -f "$PROPS" ]] || PROPS="$ROOT/../Overlayer/Directory.Build.props"
if [[ ! -f "$PROPS" ]]; then
    echo "error: Directory.Build.props not found (checked $ROOT and $ROOT/../Overlayer)." >&2
    echo "Copy ../Overlayer/Directory.Build.example.props to one of those and set GamePath first." >&2
    exit 1
fi

dotnet build "$ROOT/../Overlayer/Overlayer/Overlayer.csproj" -c Release_ML -p:DirectoryBuildPropsPath="$PROPS"
dotnet build "$ROOT/ADOFAI/Overlayer.Module.ADOFAI.csproj" -c "$CONFIG" -p:DirectoryBuildPropsPath="$PROPS" "$@"
