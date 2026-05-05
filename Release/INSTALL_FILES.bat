@echo off
title Prepare MalumMenu v3.2.0 Release Files

echo.
echo This script prepares all files needed for the release package.
echo.

REM Create release directory structure
if not exist "MalumMenu-v3.2.0-Steam-Itch" mkdir "MalumMenu-v3.2.0-Steam-Itch"
cd "MalumMenu-v3.2.0-Steam-Itch"

echo Created release directory...

REM Create BepInEx directory structure
if not exist "BepInEx" mkdir "BepInEx"
if not exist "BepInEx\core" mkdir "BepInEx\core"
if not exist "BepInEx\patchers" mkdir "BepInEx\patchers"
if not exist "BepInEx\plugins" mkdir "BepInEx\plugins"

echo Created BepInEx directories...

REM Copy BepInEx core files (these would normally be downloaded by BepInEx itself)
echo Copying BepInEx core files...
if exist "..\..\BepInEx\core\*.*" (
    copy "..\..\BepInEx\core\*.*" "BepInEx\core\" /y > nul
    echo BepInEx core files copied.
) else (
    echo WARNING: BepInEx core files not found.
    echo These will be downloaded automatically on first launch.
)

REM Copy MalumMenu.dll
echo Copying MalumMenu.dll...
if exist "..\..\src\bin\Release\net6.0\MalumMenu.dll" (
    copy "..\..\src\bin\Release\net6.0\MalumMenu.dll" "BepInEx\plugins\" /y > nul
    echo MalumMenu.dll copied successfully.
) else (
    echo ERROR: MalumMenu.dll not found!
    echo Please build the project first.
)

REM Copy Doorstop files
echo Copying Doorstop files...
if exist "winhttp.dll" (
    copy "winhttp.dll" "." /y > nul
    echo winhttp.dll copied.
) else (
    echo ERROR: winhttp.dll not found!
)

if exist "doorstop_config.ini" (
    copy "doorstop_config.ini" "." /y > nul
    echo doorstop_config.ini copied.
) else (
    echo ERROR: doorstop_config.ini not found!
)

if exist ".doorstop_version" (
    copy ".doorstop_version" "." /y > nul
)

REM Copy .NET runtime files
echo Copying .NET runtime files...
if exist "dotnet\*.*" (
    xcopy "dotnet\*.*" "dotnet\" /e /i /y > nul
    echo .NET runtime files copied.
) else (
    echo WARNING: .NET runtime files not found!
    echo These will be downloaded automatically by BepInEx.
)

REM Copy other files
echo Copying additional files...
if exist "steam_appid.txt" (
    copy "steam_appid.txt" "." /y > nul
    echo steam_appid.txt copied.
) else (
    echo WARNING: steam_appid.txt not found!
)

if exist "changelog.txt" (
    copy "changelog.txt" "." /y > nul
    echo changelog.txt copied.
) else (
    echo WARNING: changelog.txt not found!
)

REM Create CTAUL installer
echo Creating CTAUL installer...
if exist "..\CTAUL.bat" (
    copy "..\CTAUL.bat" "." /y > nul
    echo CTAUL.bat created.
) else (
    echo ERROR: CTAUL.bat not found!
)

echo.
echo ========================================
echo Release package preparation complete!
echo Directory: MalumMenu-v3.2.0-Steam-Itch
echo Files ready for distribution.
echo ========================================
echo.
pause