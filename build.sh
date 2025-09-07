#!/bin/bash
# Build script for Azure TTS Batch Studio (Linux/macOS)
# This script builds the application for multiple target platforms

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
echo "Building for Windows x86..."
dotnet publish "$PROJECT_PATH" \
  --configuration "$CONFIG" \
  --runtime win-x86 \
  --self-contained true \
  --output "$OUTPUT_DIR/win-x86" \
  -p:PublishSingleFile=false \
  -p:PublishReadyToRun=true \
  -p:IncludeNativeLibrariesForSelfExtract=true

echo ""
echo "Building for Windows ARM64..."
dotnet publish "$PROJECT_PATH" \
  --configuration "$CONFIG" \
  --runtime win-arm64 \
  --self-contained true \
  --output "$OUTPUT_DIR/win-arm64" \
  -p:PublishSingleFile=false \
  -p:PublishReadyToRun=true \
  -p:IncludeNativeLibrariesForSelfExtract=true

echo ""
echo "Creating portable packages..."
mkdir -p "$OUTPUT_DIR/portable"

# Create portable zip files
cd "$OUTPUT_DIR/win-x64" && zip -r "../portable/AzureTtsBatchStudio-x64-portable.zip" . && cd ../..
cd "$OUTPUT_DIR/win-x86" && zip -r "../portable/AzureTtsBatchStudio-x86-portable.zip" . && cd ../..
cd "$OUTPUT_DIR/win-arm64" && zip -r "../portable/AzureTtsBatchStudio-arm64-portable.zip" . && cd ../..

echo ""
echo "Build completed successfully!"
echo "Output directory: $OUTPUT_DIR"
echo ""
echo "Available builds:"
echo "- Windows x64: $OUTPUT_DIR/win-x64/"
echo "- Windows x86: $OUTPUT_DIR/win-x86/"
echo "- Windows ARM64: $OUTPUT_DIR/win-arm64/"
echo "- Portable packages: $OUTPUT_DIR/portable/"
echo ""