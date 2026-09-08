# Modular Chess

A chess platform: standard chess as the always-on rules authority, with optional Modes selected per Match. An Activity is what you came to do; a Mode is a rule pack on a Match.

## Language

**Core**:
The rules authority for standard FIDE chess. Always present in every Match. Not a Mode.
_Avoid_: module, engine, always-on Mode

**Activity**:
What you came to do. MVP Activities are Versus AI and Versus Friend. Later Activities include Puzzle and Survivor. Join is not an Activity.
_Avoid_: Opponent, Mode, game mode, match type

**Mode**:
An optional rule pack that hooks Core for one Match. Never replaces Core. A Mode declares which Activities may use it. After the player picks an Activity, only those Modes are offered. A Match may stack several compatible Modes; both players play that same set.
_Avoid_: Add-on, DLC, mod, game mode, Activity

**Join**:
A Play-screen action that enters a Versus Friend Lobby with a Join Code. Not an Activity. A Steam invite skips this and dumps the friend into the Lobby.
_Avoid_: Activity, Opponent

**Host**:
The player who authors a Versus Friend Lobby and must own every Mode in that Match's Mode set. Changing the Mode set means leaving and creating a new Lobby. The Host Starts the Match; Start is allowed only when the other player is in the Lobby and synced.
_Avoid_: owner, server

**Lobby**:
A Versus Friend room waiting for the second player. It ends when the Match starts or the Host leaves. Not matchmaking. The other player does not Start; they wait for the Host.
_Avoid_: queue, server browser, matchmaking

**Join Code**:
A shareable code that admits a player to a Versus Friend Lobby. Cross-play is allowed. On mobile this is the only join path. Steam invite is desktop-to-desktop only.
_Avoid_: room ID, matchmaking

**Match**:
One playthrough of Versus AI or Versus Friend: Core, Match Settings, and one shared Mode set, from setup until a terminal result. Versus AI has no Lobby; the Match starts after Modes and Match Settings are confirmed.
_Avoid_: game, game mode, Activity

**Match Settings**:
Per-Match options that are part of Core, not a Mode. Host-authored and editable in the Lobby until the Match starts. Time control (including none), Host color (White, Black, or Random), and Versus AI strength (Easy, Medium, Hard).
_Avoid_: Base Settings, Time Pressure, Options

**Options**:
Account and app settings. Includes show notation. Not a Match and not Customization.
_Avoid_: Match Settings, Customization

**Game**:
The product, Modular Chess. Not a playthrough.
_Avoid_: using "game" for a Match

**Customization**:
Account-level presentation: unit sets, colors, icons, BGM, and SFX. Not a Mode. Each player's unit set, colors, and icons appear on that player's pieces for both people. BGM and SFX stay on the local machine. Not in MVP except a disabled Customize control on the main menu.
_Avoid_: client-side, mod, skin pack
