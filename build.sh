#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
RID="${1:-linux-x64}"
case "$RID" in
  win-x64|win-arm64|linux-x64|linux-arm64|osx-x64|osx-arm64) ;;
  *) echo "Usage: $0 [win-x64|win-arm64|linux-x64|linux-arm64|osx-x64|osx-arm64]" >&2; exit 2 ;;
esac
dotnet publish DndEncounters.csproj -c Release -r "$RID" --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "publish/$RID"
echo "Build ready in publish/$RID"
