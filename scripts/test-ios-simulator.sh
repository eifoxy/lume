#!/bin/bash
set -euo pipefail
app="$1"
device=$(xcrun simctl list devices available -j | python3 -c 'import json,sys; d=json.load(sys.stdin); print(next(v["udid"] for group in d["devices"].values() for v in group if v["name"].startswith("iPhone")))')
xcrun simctl boot "$device"
xcrun simctl bootstatus "$device" -b
xcrun simctl install "$device" "$app"
xcrun simctl launch "$device" br.com.lume.aquario.ios --smoke-test
container=$(xcrun simctl get_app_container "$device" br.com.lume.aquario.ios data)
for i in $(seq 1 30); do
  if [ -f "$container/Documents/ios-smoke.json" ]; then break; fi
  sleep 2
done
mkdir -p artifacts/ios-preview
cp "$container/Documents/ios-smoke.json" artifacts/ios-preview/ios-smoke.json
python3 -c 'import json; d=json.load(open("artifacts/ios-preview/ios-smoke.json")); print(d); assert d["ok"], d'
xcrun simctl io "$device" screenshot artifacts/ios-preview/ios-simulator.png
xcrun simctl shutdown "$device"
