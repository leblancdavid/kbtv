@echo off
REM Test Coverage Report Script for Windows
REM Usage: report-tests.bat [godot_path]
REM Default Godot path: D:\Software\Godot\Godot_v4.5.1-stable_mono_win64.exe

setlocal

set GODOT_PATH=%~1
if "%GODOT_PATH%"=="" set GODOT_PATH=D:\Software\Godot\Godot_v4.5.1-stable_mono_win64.exe

set OUTPUT_DIR=.\coverage
set COVERAGE_FILE=%OUTPUT_DIR%\coverage.xml

echo === KBTV Test Coverage Report ===
echo Godot path: %GODOT_PATH%

REM Ensure output directory exists
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

REM Run tests with coverage using coverlet
echo Running tests with coverage...
dotnet tool run coverlet ./godot/mono/temp/bin/Debug/KBTV.dll ^
  --target "%GODOT_PATH%" ^
  --targetargs "--run-tests --coverage --quit-on-finish" ^
  --format "opencover" ^
  --output "%COVERAGE_FILE%" ^
  --exclude-by-file "**/test/**/*" ^
  --exclude-by-file "**/tests/**/*" ^
  --exclude-by-file "**/*Microsoft.NET.Test.Sdk.Program.cs" ^
  --exclude-by-file "**/Godot.SourceGenerators/**/*" ^
  --exclude-assemblies-without-sources "missingall"

echo.
echo === Coverage Report Generated ===
echo Coverage file: %COVERAGE_FILE%

REM Generate summary if reportgenerator is available
dotnet tool list -g | findstr "reportgenerator" >nul
if %errorlevel% equ 0 (
  dotnet reportgenerator ^
    -reports:"%COVERAGE_FILE%" ^
    -targetdir:"%OUTPUT_DIR%\report" ^
    -reporttypes:"Html;Badges"
  echo HTML report generated: %OUTPUT_DIR%\report\index.html
) else (
  echo Note: Install 'reportgenerator' for HTML reports
  echo   dotnet tool install -g dotnet-reportgenerator-globaltool
)

echo.
echo === Coverage Summary ===
echo Coverage data available in: %COVERAGE_FILE%

echo.
echo Done! See %COVERAGE_FILE% for full coverage data.

endlocal
