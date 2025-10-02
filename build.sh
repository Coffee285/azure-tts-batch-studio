#!/bin/bash
# Build script for Azure TTS Batch Studio (Linux/macOS)
# This script builds the application for Windows x64

echo "Building Azure TTS Batch Studio..."

# Set variables
PROJECT_PATH="AzureTtsBatchStudio/AzureTtsBatchStudio.csproj"
OUTPUT_DIR="publish"
CONFIG="Release"

# Clean previous builds
if [ -d "$OUTPUT_DIR" ]; then
    rm -rf "$OUTPUT_DIR"
fi
mkdir -p "$OUTPUT_DIR"

echo ""
echo "Building for Windows x64..."
dotnet publish "$PROJECT_PATH" \
  --configuration "$CONFIG" \
  --runtime win-x64 \
  --self-contained true \
  --output "$OUTPUT_DIR/win-x64" \
  -p:PublishSingleFile=false \
  -p:PublishReadyToRun=true \
  -p:IncludeNativeLibrariesForSelfExtract=true

echo ""
echo "Build completed successfully!"
echo "Output directory: $OUTPUT_DIR"
echo ""
echo "Available build:"
echo "- Windows x64: $OUTPUT_DIR/win-x64/"
echo ""