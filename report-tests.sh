#!/bin/bash
# Test Coverage Report Script
# Usage: ./report-tests.sh [--godot <path>]

set -e

GODOT_PATH="${GODOT:-godot}"
OUTPUT_DIR="./coverage"
COVERAGE_FILE="$OUTPUT_DIR/coverage.xml"

echo "=== KBTV Test Coverage Report ==="
echo "Godot path: $GODOT_PATH"

# Ensure output directory exists
mkdir -p "$OUTPUT_DIR"

# Run tests with coverage using coverlet
echo "Running tests with coverage..."
coverlet "./.godot/mono/temp/bin/Debug/KBTV.dll" \
  --target "$GODOT_PATH" \
  --targetargs "--run-tests --coverage --quit-on-finish" \
  --format "opencover" \
  --output "$COVERAGE_FILE" \
  --exclude-by-file "**/test/**/*.cs" \
  --exclude-by-file "**/tests/**/*.cs" \
  --exclude-by-file "**/*Microsoft.NET.Test.Sdk.Program.cs" \
  --exclude-by-file "**/Godot.SourceGenerators/**/*.cs" \
  --exclude-assemblies-without-sources "missingall" \
  --verbose

echo ""
echo "=== Coverage Report Generated ==="
echo "Coverage file: $COVERAGE_FILE"

# Generate summary if reportgenerator is available
if command -v reportgenerator &> /dev/null; then
  reportgenerator \
    -reports:"$COVERAGE_FILE" \
    -targetdir:"$OUTPUT_DIR/report" \
    -reporttypes:"Html;Badges"

  echo "HTML report generated: $OUTPUT_DIR/report/index.html"
else
  echo "Note: Install 'reportgenerator' for HTML reports"
  echo "  dotnet tool install -g dotnet-reportgenerator-globaltool"
fi

# Calculate and display summary
echo ""
echo "=== Coverage Summary ==="
if command -v grep &> /dev/null; then
  COVERAGE=$(grep -oP '(?<=coveredby=").*?(?=")' "$COVERAGE_FILE" 2>/dev/null || echo "N/A")
  echo "Coverage data available in: $COVERAGE_FILE"
fi

echo ""
echo "Done! See $COVERAGE_FILE for full coverage data."
