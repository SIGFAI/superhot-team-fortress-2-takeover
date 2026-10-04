#!/bin/bash
# build, start the demo, take screenshots at given delays after it starts
cd /c/mod/work/b30_0815
powershell -NoProfile -ExecutionPolicy Bypass -File kits/superhot/check.ps1 -PlaySec 3 2>&1 | tail -5
rm -f /c/mod/work/superhot-rec.txt
powershell -NoProfile -ExecutionPolicy Bypass -File kits/superhot/play.ps1 -Mod C:/mod/work/b30_0815/release -Demo 2>&1 | tail -1
for i in $(seq 1 40); do grep -q SIGF_READY /c/mod/work/superhot/game/BepInEx/LogOutput.log 2>/dev/null && break; sleep 2; done
sleep 3; echo 1 > /c/mod/work/superhot-rec.txt
t0=0
for d in "$@"; do sleep $((d-t0)); t0=$d; powershell -NoProfile -ExecutionPolicy Bypass -File C:/mod/repo/machine/showcase/showcase.ps1 -Game -Seconds 3 | tail -1; done
grep -n "ERROR\|WARN" /c/mod/work/superhot/game/BepInEx/LogOutput.log | head
