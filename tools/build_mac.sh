#!/bin/zsh
# Build the Apple Silicon Mac app into Builds/mac. Pass -universal for Intel+ARM.
cd "$(dirname "$0")/.."
U=${UNITY:-/Applications/Unity/Hub/Editor/6000.6.6f1/Unity.app/Contents/MacOS/Unity}
mkdir -p Logs
$U -batchmode -nographics -quit -projectPath . -executeMethod Fitz.EditorTools.FitzBuild.BuildMac -buildOutput Builds/mac "$@" -logFile Logs/build-mac.log
code=$?
grep -E "\[FitzBuild\]|error CS|Shader error" Logs/build-mac.log
exit $code
