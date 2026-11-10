#!/bin/zsh
# tools/shot.sh <out.png> [extra game args...]
# Runs the Mac build, grabs a screenshot after a few seconds, and quits.
# Useful args: -fitzStart 18:40  -fitzLook yaw,pitch  -fitzShotDelay 10  -fitzSkipTitle
cd "$(dirname "$0")/.."
out=$1; shift
"Builds/mac/Edmund Fitzgerald.app/Contents/MacOS/Edmund Fitzgerald" -screen-width 1600 -screen-height 900 -screen-fullscreen 0 \
  -fitzShot "$out" -fitzQuit "$@" -logFile "${out%.png}.log" &
pid=$!
for i in {1..95}; do sleep 1; kill -0 $pid 2>/dev/null || break; done
kill $pid 2>/dev/null
grep -E "\[Fitz\]|Exception" "${out%.png}.log" | head -20
