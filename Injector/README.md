# MalumMenu Injector

A simple injector that downloads and installs MalumMenu mod into Among Us.

## Features

- 🚀 **Automatic Downloads** - Fetches latest release from GitHub
- 📦 **Complete Package** - Downloads BepInEx, Doorstop, and all dependencies
- 🎯 **Smart Detection** - Automatically finds Among Us installation (Steam, Epic, Microsoft Store)
- ⚙️ **Settings Persistence** - Remembers your game path
- 🔄 **Update Checking** - Notifies when updates are available
- 📝 **One-Click Install** - Extracts everything to the correct folders

## How to Use

1. **Run MalumMenuInjector.exe**
2. **Browse** to your Among Us game folder (or let it auto-detect)
3. **Click "Download & Install"** to get the latest version
4. **Launch Among Us** - Press **DELETE** in-game to open the mod menu

## What Gets Installed

- `MalumMenu.dll` (the mod with AI Mode)
- `BepInEx/` (mod loader framework)
- `doorstop_config.ini` (BepInEx configuration)
- `winhttp.dll` (Doorstop injector)
- `steam_appid.txt` (Steam App ID)
- `.NET runtime` (required for BepInEx IL2CPP)

## Supported Game Platforms

- ✅ Steam
- ✅ Epic Games Launcher
- ✅ Microsoft Store
- ✅ Xbox App
- ✅ Itch.io

## Building the Injector

```bash
dotnet build --configuration Release
```

The compiled executable will be in `bin/Release/net6.0-windows/`.

## Security

This injector only downloads files from the official GitHub repository and places them in your game folder. It does not modify any game files beyond what's necessary for mod loading.

---

**MalumMenu © 2026 - AI-Powered Among Us Mod**