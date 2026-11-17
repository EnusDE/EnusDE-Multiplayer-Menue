# EnusDE Multiplayer Browser

Phase 1 preview, version 0.1.0, built against Railroader 0.6.2 Experimental (Steam build 26335745). It extends **Join Multiplayer** with local text search, sorting, a filter for new employees, and automatic refresh. The asset pack is independent and uses Railroader's optional mod scope.

## Install and use

1. Close Railroader. In Steam, verify that the installed game is Railroader 0.6.2.
2. Extract the `EnusDE.MultiplayerBrowser` folder into `Documents\Railroader\Mods`. The resulting folder should contain `Info.json` and `EnusDE.MultiplayerBrowser.dll` side by side. If you use a custom `-homedir`, use its `Mods` folder.
3. In Railroader, turn on **Allow Unsafe DLL Mods** under **Preferences → Features → Experimental**, then restart the game. Enable **EnusDE Multiplayer Browser** under **Mods → Manage Mods** and apply the selection.
4. Open **Join Multiplayer**. Search by railroad name, reporting mark, or public description. A query such as `mark:AL freight` asks Steam for the exact mark `AL` and searches the returned descriptions and names locally.
5. Use **Sort** to switch between name and player count. Use the new-employee filter to hide lobbies for which Railroader reports a restriction on new employees. Use the game's **Refresh** button to refresh immediately.
6. Select the game's **Join** button and follow its usual prompts. To remove the browser, disable its pack in Manage Mods, exit the game, and remove the pack folder.

Railroader's [DLL mod setup](https://docs.railroadergame.com/modding/dll-mods/) and [mod installation guide](https://docs.railroadergame.com/modding/installing-mods/) describe the built-in loader and its settings.

## Phase 1 behavior

- Ordinary text searches filter the current Steam results locally. Railroader's discovery rules still apply, including its game-version filter and 50-result limit. Use `mark:` for a targeted lookup if the list reaches the limit.
- The list shows the game's lobby name, reporting mark, description, player count, and new-employee restrictions where available. It adds the matched game version and Steam lobby capacity where available. Hover over the player count to see the added details.
- While the menu is open, the browser refreshes about 15 seconds after each successful search. Failures keep the previous results and extend the automatic retry interval to 30, 60, and then 120 seconds. Cached rows can become stale, so a lobby can close or fill before you join.
- Railroader still performs the join, password handling, and mod compatibility checks. The browser has no network protocol of its own in Phase 1. Per-mod compatibility previews, host-published data, Discord links, ping, and error 4001 diagnostics belong to later phases.

## Verification

The DLL was built against the installed 0.6.2 assemblies. Thirteen offline checks cover filtering, ordering, snapshot comparison, and refresh timing. In the separate test profile, Railroader reached Unity startup but exited after Steamworks initialization failed. The in-game layout, discovery, and actual host/client joins still require a Steam session. See [TESTING.md](TESTING.md) for the two-PC checklist.
