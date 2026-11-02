#!/bin/bash
set -euo pipefail

# Copy the complete on-device session tree without deleting or changing it.
project_root="$(cd "$(dirname "$0")/../.." && pwd)"
adb_tool="${ADB:-/Applications/Unity/Hub/Editor/6000.5.6f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb}"
if [[ ! -x "$adb_tool" ]]; then
    adb_tool="$(command -v adb || true)"
fi
if [[ -z "$adb_tool" || ! -x "$adb_tool" ]]; then
    echo "Android adb not found. Set ADB to its full path." >&2
    exit 1
fi
device_path="/sdcard/Android/data/com.rohangudla.cap5118.rotationpilot/files/CAP5118_PilotSessions"
"$adb_tool" get-state >/dev/null
if ! "$adb_tool" shell test -d "$device_path"; then
    echo "No session folder found on the connected Quest." >&2
    exit 1
fi
mkdir -p "$project_root/PilotExports"
destination="$(mktemp -d "$project_root/PilotExports/Quest_$(date +%Y%m%d_%H%M%S)_XXXXXX")"
"$adb_tool" pull "$device_path" "$destination/"
echo "Copied sessions to: $destination/CAP5118_PilotSessions"
echo "The original sessions remain on the headset. Keep test sessions separate from participant data."
