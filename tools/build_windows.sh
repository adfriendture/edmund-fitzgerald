#!/bin/zsh
# Build the 64-bit Windows player into Builds/windows.
cd "$(dirname "$0")/.."
U=${UNITY:-/Applications/Unity/Hub/Editor/6000.6.6f1/Unity.app/Contents/MacOS/Unity}
mkdir -p Logs
$U -batchmode -nographics -quit -projectPath . -executeMethod Fitz.EditorTools.FitzBuild.BuildWindows -buildOutput Builds/windows -logFile Logs/build-windows.log
code=$?
grep -E "\[FitzBuild\]|error CS|Shader error" Logs/build-windows.log
exit $code
