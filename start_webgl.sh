#!/usr/bin/env bash
# Quick runner script for the Playable WebGL Build
set -e

PORT=8080
BUILD_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/Build/WebGL" && pwd)"

echo "=========================================================="
echo "  🎰 Launching Unity WebGL Slot Machine Build"
echo "  📁 Serving directory: ${BUILD_DIR}"
echo "  🌐 Local URL: http://localhost:${PORT}/index.html"
echo "=========================================================="
echo "Press Ctrl+C to stop the server."

python3 -m http.server ${PORT} --directory "${BUILD_DIR}"
