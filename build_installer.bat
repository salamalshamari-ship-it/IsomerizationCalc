@echo off
REM One click: builds IsomerizationCalc.exe, then builds Setup.exe with Inno Setup.
REM Result: installer_output\IsomerizationCalc_Setup_1.0.0.exe
cd /d "%~dp0"

echo [1/2] Building the program...
dotnet publish IsomerizationCalc.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
if errorlevel 1 goto fail

echo.
echo [2/2] Building the installer...
set "ISCC="
if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if exist "%LocalAppData%\Programs\Inno Setup 6\ISCC.exe" set "ISCC=%LocalAppData%\Programs\Inno Setup 6\ISCC.exe"
if "%ISCC%"=="" (
  echo Inno Setup 6 was not found. Install it from https://jrsoftware.org/isdl.php
  echo or open installer.iss in Inno Setup and press Ctrl+F9.
  goto fail
)
"%ISCC%" installer.iss
if errorlevel 1 goto fail

echo.
echo Done. The installer is in the "installer_output" folder.
explorer installer_output
pause
exit /b 0

:fail
echo.
echo FAILED. Copy the error text above and send it to Claude.
pause
exit /b 1
