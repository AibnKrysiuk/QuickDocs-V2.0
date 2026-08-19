#!/usr/bin/env bash
set -e

RID="$1"        # win-x64 o linux-x64
CONFIG="${2:-Release}"

if [ -z "$RID" ]; then
  echo "Uso: ./publicar.sh <win-x64|linux-x64> [Configuration]"
  exit 1
fi

DIR_UI="$(dirname "$0")/QuickDocs.UI/QuickDocs.UI.Desktop"
DIR_BACKEND="$(dirname "$0")/QuickDocs.Backend"

echo "==> Publicando cliente (UI) para $RID..."
dotnet publish "$DIR_UI" -c "$CONFIG" -r "$RID" --self-contained true -p:PublishSingleFile=true

PUBLISH_DIR="$DIR_UI/bin/$CONFIG/net8.0/$RID/publish"

echo "==> Publicando backend para $RID..."
dotnet publish "$DIR_BACKEND" -c "$CONFIG" -r "$RID" --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -o "$PUBLISH_DIR/backend"

echo "==> Listo: $PUBLISH_DIR"
ls -la "$PUBLISH_DIR"