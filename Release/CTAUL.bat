@echo off
title MalumMenu v3.2.0 - CTAUL Installer
echo.
echo ========================================
echo  MalumMenu v3.2.0 - AI Mode Release
echo ========================================
echo.
echo This installer will copy MalumMenu mod files to your Among Us game.
echo Make sure Among Us is NOT running before continuing.
echo.
echo Press any key to continue...
pause > n

REM Check if Among Us is running
tasklist /fi "Among Us.exe" /fo | findstr /i "Among Us.exe" > nul
if %errorlevel% equ 0 (
    echo.
    echo WARNING: Among Us appears to be running!
    echo Please close Among Us before installing the mod.
    echo.
    pause
    exit /b 1
)

set /p GAME_PATH="Enter your Among Us game folder path: "
if "%GAME_PATH%"=="" (
    echo.
    echo ERROR: No game path specified!
    echo.
    echo Please run this installer again and provide a valid path.
    echo.
    pause
    exit /b 1
)

if not exist "%GAME_PATH%" (
    echo.
    echo ERROR: Game folder does not exist: %GAME_PATH%
    echo.
    echo Please check the path and try again.
    echo.
    pause
    exit /b 1
)

echo.
echo Installing MalumMenu v3.2.0...
echo.

REM Create necessary directories
if not exist "%GAME_PATH%\BepInEx" mkdir "%GAME_PATH%\BepInEx"
if not exist "%GAME_PATH%\BepInEx\core" mkdir "%GAME_PATH%\BepInEx\core"
if not exist "%GAME_PATH%\BepInEx\patchers" mkdir "%GAME_PATH%\BepInEx\patchers"
if not exist "%GAME_PATH%\BepInEx\plugins" mkdir "%GAME_PATH%\BepInEx\plugins"
if not exist "%GAME_PATH%\dotnet" mkdir "%GAME_PATH%\dotnet"

echo Created BepInEx directories...

REM Copy BepInEx core files (these would normally be downloaded by BepInEx itself)
echo Copying BepInEx core files...
if exist "BepInEx\core\*.*" (
    copy "BepInEx\core\*.*" "%GAME_PATH%\BepInEx\core\" /y > nul
    if %errorlevel% equ 0 (
        echo BepInEx core files copied successfully.
    ) else (
        echo WARNING: Some BepInEx core files may be missing.
    )
) else (
    echo WARNING: BepInEx core files not found in installer directory.
    echo These will be downloaded automatically on first game launch.
)

REM Copy MalumMenu.dll
echo Copying MalumMenu.dll...
if exist "MalumMenu.dll" (
    copy "MalumMenu.dll" "%GAME_PATH%\BepInEx\plugins\" /y > nul
    if %errorlevel% equ 0 (
        echo MalumMenu.dll copied successfully.
    ) else (
        echo ERROR: Failed to copy MalumMenu.dll!
    )
) else (
    echo ERROR: MalumMenu.dll not found in installer directory!
    pause
    exit /b 1
)

REM Copy Doorstop files
echo Copying Doorstop injector...
if exist "winhttp.dll" (
    copy "winhttp.dll" "%GAME_PATH%\" /y > nul
    if %errorlevel% equ 0 (
        echo winhttp.dll copied successfully.
    ) else (
        echo ERROR: Failed to copy winhttp.dll!
    )
) else (
    echo ERROR: winhttp.dll not found in installer directory!
)

if exist "doorstop_config.ini" (
    copy "doorstop_config.ini" "%GAME_PATH%\" /y > nul
    if %errorlevel% equ 0 (
        echo doorstop_config.ini copied successfully.
    ) else (
        echo ERROR: Failed to copy doorstop_config.ini!
    )
) else (
    echo ERROR: doorstop_config.ini not found in installer directory!
)

if exist ".doorstop_version" (
    copy ".doorstop_version" "%GAME_PATH%\" /y > nul
)

REM Copy .NET runtime files
echo Copying .NET runtime files...
if exist "dotnet\*.*" (
    xcopy "dotnet\*.*" "%GAME_PATH%\dotnet\" /e /i /y > nul
    if %errorlevel% equ 0 (
        echo .NET runtime files copied successfully.
    ) else (
        echo WARNING: Some .NET files may not have copied.
    )
) else (
    echo WARNING: .NET runtime files not found in installer directory.
    echo These will be downloaded automatically by BepInEx.
)

REM Copy steam_appid.txt
echo Copying steam_appid.txt...
if exist "steam_appid.txt" (
    copy "steam_appid.txt" "%GAME_PATH%\" /y > nul
    if %errorlevel% equ 0 (
        echo steam_appid.txt copied successfully.
    ) else (
        echo ERROR: Failed to copy steam_appid.txt!
    )
) else (
    echo ERROR: steam_appid.txt not found in installer directory!
)

REM Copy changelog.txt
echo Copying changelog.txt...
if exist "changelog.txt" (
    copy "changelog.txt" "%GAME_PATH%\" /y > nul
    if %errorlevel% equ 0 (
        echo changelog.txt copied successfully.
    ) else (
        echo WARNING: Failed to copy changelog.txt.
    )
) else (
    echo WARNING: changelog.txt not found.
)

echo.
echo ========================================
echo Installation Complete!
echo ========================================
echo.
echo MalumMenu v3.2.0 with AI Mode has been installed.
echo.
echo Next steps:
echo 1. Launch Among Us
echo 2. Press DELETE in-game to open the MalumMenu
echo 3. Go to AI tab to configure your Groq API key
echo 4. Enable AI Mode to start using the AI assistant
echo.
echo.
echo Enjoy your AI-powered Among Us experience!
echo.
echo Press any key to exit...
pause > nul