@echo off
REM Builds a self-contained single-file IsomerizationCalc.exe into the "publish" folder.
cd /d "%~dp0"
dotnet publish IsomerizationCalc.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
if errorlevel 1 (
  echo.
  echo Build FAILED. Copy the error text and send it to Claude.
) else (
  echo.
  echo Done. The program is in the "publish" folder.
)
pause
