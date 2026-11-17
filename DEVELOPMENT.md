# Development

## Build on Windows

Use PowerShell and an installed .NET SDK with the C# 9 compiler, such as .NET 8. Use a local Railroader 0.6.2 Steam installation as the reference set.

```powershell
.\Build.ps1 -RailroaderPath 'D:\SteamLibrary\steamapps\common\Railroader'
dotnet run --project tests/BrowserList.Tests.csproj --configuration Release
```

The build script compiles against Railroader's bundled Mono assemblies and writes an asset pack under `dist`. The script references the `0Harmony.dll` present in the game's Managed directory. The release package ships the browser DLL and its manifest. For a clean installation, verify that the game's Managed directory contains Harmony before testing the package. The source project includes an offline GitHub Actions workflow that requires only the .NET SDK.

## Integration

The built-in DLL loader discovers `BrowserMod` via `IModEntryPoint`. The manifest uses `scope: optional` and requires the 0.6 line starting at 0.6.2. The release was compiled with C# 9 for the Mono environment used by Railroader.

The public DLL contract exposes lifecycle hooks. The main-menu integration uses internal game members whose signatures may change with game updates. The entry point verifies those members before registering Harmony patches. The browser intercepts discovery and search at the menu, calls Railroader's `ClientLobbyHelper.FetchLobbies`, and creates native `LobbyRow.Info` rows. The native `JoinLobbyById` callback remains responsible for initiating the actual join. Unloading removes the browser patches and restores the search field.

Railroader's discovery filters by exact game version, open status, and optionally a reporting mark. It reads the host's public listing fields from Steam and applies the game's text filtering. The browser reuses those results and caches unchanged rows. The listing in this build has no host mod inventory or pre-join latency measurement. Future phases can publish additional opt-in fields under separate lobby metadata keys, with an independent browser protocol version.

- [Railroader DLL mod contract](https://docs.railroadergame.com/modding/dll-mods/)
- [Railroader manifest and optional scope](https://docs.railroadergame.com/modding/info-json/#scope-optional)
- [Steam lobby matchmaking reference](https://partner.steamgames.com/doc/api/ISteamMatchmaking#RequestLobbyList)
