# Railroader 0.6.2 two-PC test

Use two Windows PCs signed in to Steam with the same Railroader game version. Use a temporary world and matching required game mods. A public lobby with a unique reporting mark makes discovery easy to verify.

| Test | Expected result |
| --- | --- |
| No browser on either PC | The native menu finds the host and the client can join. |
| Browser only on the client | The public host appears in Join Multiplayer. The client joins using the usual Railroader prompts. |
| Browser only on the host | The unmodified client can find and join the host. |
| Browser on both PCs | Discovery and joining still work. Opening a session does not require matching browser versions. |
| Different browser versions, once a second build exists | Repeat the join test with each version as host. Compare the result with the unmodified baseline. |
| Host closes and reopens the lobby | The list reflects the change after successful refresh. The client can also use Refresh. |
| Client searches by name, reporting mark, and public description | Free text narrows the current results. A `mark:XXXX` query finds the exact mark through the game's Steam search. |
| Multiple lobbies, if available | Sort by name and player count, then apply the new-employee filter. Verify that ordering and restrictions match the game-provided data. |
| Host requires a password or another mod | The existing join flow enforces the relevant requirement. Test a missing and mismatched required mod, then repeat with a matching version. |
| Client briefly loses Steam connectivity while browsing | The previous results remain marked as cached. Automatic refresh backs off. After restoring connectivity, use Refresh or wait for the next retry. |
| Client leaves the browser during a refresh and reopens it | The new menu refreshes without a stuck search or an exception. |
| Client enables and disables the browser and restarts Railroader | The browser appears only while enabled. The native join flow works after disabling it. |

Check version and capacity in both the row details and the player-count tooltip when Steam provides capacity. Check that all controls remain legible at the in-game UI scales you use. Record the game build, browser version, install placement, steps, and observed result for each test. Redact Steam IDs and other player data from shared logs.

Network quality, disconnection classification, and error 4001 are outside this Phase 1 test.
