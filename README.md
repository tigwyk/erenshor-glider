# Erenshor Glider

A full-featured automation bot for [Erenshor](https://store.steampowered.com/app/2382520/Erenshor/) inspired by WoW-Glider - AFK grinding, combat, looting, waypoints, and 2D radar GUI.

## Features (Planned)

- **AFK Grinding** - Automated combat with configurable profiles
- **Waypoint System** - Record and playback patrol paths
- **Combat Profiles** - Class-specific ability rotations
- **Looting** - Automatic corpse looting
- **2D Radar GUI** - Visual display of nearby entities
- **Auto-mapping** - Discover and record NPC/mob/node locations

## Requirements

- [Erenshor](https://store.steampowered.com/app/2382520/Erenshor/) (Steam version)
- [BepInEx 5.x](https://github.com/BepInEx/BepInEx/releases) for Erenshor
- [.NET SDK 8.0+](https://dotnet.microsoft.com/download) for building

## Project Structure

```
erenshor-glider/
├── src/
│   └── ErenshorGlider/     # Main BepInEx plugin
├── docs/                    # Technical documentation
├── profiles/                # Combat profiles (JSON)
├── waypoints/               # Waypoint paths (JSON)
└── tasks/                   # Development tasks/PRDs
```

## Building

### Prerequisites

1. Install .NET SDK 8.0 or later
2. Clone this repository

### Build Commands

```bash
# Restore dependencies
dotnet restore

# Build in Debug mode
dotnet build

# Build in Release mode
dotnet build -c Release
```

The compiled plugin DLL will be in `src/ErenshorGlider/bin/Release/ErenshorGlider.dll`

## Installation

1. Install BepInEx 5.x in your Erenshor game directory
2. Copy `ErenshorGlider.dll` to `<Erenshor>/BepInEx/plugins/`
3. Launch Erenshor

## Development

### IDE Setup

- **Visual Studio 2022** - Open `ErenshorGlider.sln`
- **JetBrains Rider** - Open `ErenshorGlider.sln`
- **VS Code** - Install C# Dev Kit extension

### Code Style

This project uses EditorConfig for consistent code formatting. Most IDEs will automatically pick up the `.editorconfig` file.

### Game References

For full IntelliSense support with game types, you may want to add references to the Erenshor game assemblies:

1. Locate `<Erenshor>/Erenshor_Data/Managed/Assembly-CSharp.dll`
2. Add it as a reference to the project (optional, for type information)

## License

This project is for educational purposes only. Use at your own risk.

## Disclaimer

This is an automation tool for a single-player game. Always respect game developer's terms of service and use responsibly.
