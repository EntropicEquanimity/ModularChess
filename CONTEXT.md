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
An optional rule pack that hooks Core for one Match. Never replaces Core. A Mode declares which Activities may use it. After the player picks an Activity, only those Modes are offered. Join is never on that list. The first three Modes (Fog of War, Powerful Pieces, Martyr) allow Versus AI and Versus Friend only. A Match may stack several compatible Modes; both players play that same set.
_Avoid_: Add-on, DLC, mod, game mode, Activity

**Join**:
A Play-screen action that enters a Versus Friend Lobby with a Join Code. Not an Activity. A Steam invite skips this and dumps the friend into the Lobby.
_Avoid_: Activity, Opponent

**Host**:
The player who authors a Versus Friend Lobby and must own every Mode in that Match's Mode set. Changing the Mode set means leaving and creating a new Lobby. The Host Starts the Match; Start is allowed only when the other player is in the Lobby and synced.
_Avoid_: owner, server

**Lobby**:
A Versus Friend room waiting for the second player. It ends when the Match starts or the Host leaves. If the Host leaves, the other player returns to Activity selection. If the other player leaves, the Host stays and the Join Code stays valid. Not matchmaking. The other player does not Start; they wait for the Host.
_Avoid_: queue, server browser, matchmaking

**Join Code**:
A shareable code that admits a player to a Versus Friend Lobby. Cross-play is allowed. On mobile this is the only join path. Steam invite is desktop-to-desktop only.
_Avoid_: room ID, matchmaking

**Match**:
One playthrough of Versus AI or Versus Friend: Core, Match Settings, and one shared Mode set, from setup until a terminal result. Versus AI has no Lobby; the Match starts after Modes and Match Settings are confirmed.
_Avoid_: game, game mode, Activity

**Piece**:
A specific occupant of the Board: identity, Side, and current PieceType. Two knights of the same Side are two Pieces.
_Avoid_: unit, token, PieceType

**PieceType**:
The kind of a Piece. Core ships Pawn, Knight, Bishop, Rook, Queen, and King. A Mode may introduce additional kinds for a Match.
_Avoid_: Piece, class, role

**Side**:
White or Black. Who owns a Piece, and who is to move.
_Avoid_: color, player, team

**Move**:
One Core state change: a Piece from–to, including castle, en passant, and promotion.
_Avoid_: action, sub-move, Turn

**Turn**:
One Side's opportunity to make one or more Moves. FIDE default is one Move. A Mode may allow further Moves before the Turn ends. Extra Moves are optional. That Side's clock runs for the whole Turn.
_Avoid_: action, sub-move, round

**Timeout**:
That Side loses immediately when their clock hits zero, even mid-Move, unless a Mode says otherwise.
_Avoid_: flag

**Disconnect**:
During a Match, a reconnect window; if that player does not return, that Side loses, unless a Mode says otherwise.
_Avoid_: Timeout, ragequit

**End Turn**:
Closes a Turn that did not end after a Move. Shown only after a Move that left the Turn open. By default a Side cannot End Turn with zero Moves this Turn. End Turn is illegal while that Side is in check, unless a Mode changes that.
_Avoid_: Pass, skip

**Match Settings**:
Per-Match options that are part of Core, not a Mode. Host-authored and editable in the Lobby until the Match starts. Time control (including none), Host color (White, Black, or Random), Versus AI strength (Easy, Medium, Hard), and whether a Side may End Turn with zero Moves this Turn (default off).
_Avoid_: Base Settings, Time Pressure, Options

**Options**:
Account and app settings. Includes show notation. Not a Match and not Customization.
_Avoid_: Match Settings, Customization

**Shop**:
The catalog of purchasable content. MVP sells Modes. Later also Activities and Customization. Core, Versus AI, Versus Friend, and Join are not sold here. Ownership is per platform store (Steam, App Store, Play), not synced across platforms in MVP.
_Avoid_: Unlock, store, DLC menu

**Game**:
The product, Modular Chess. Not a playthrough.
_Avoid_: using "game" for a Match

**Customization**:
Account-level presentation: unit sets, colors, icons, BGM, and SFX. Not a Mode. Each player's unit set, colors, and icons appear on that player's pieces for both people. BGM and SFX stay on the local machine. Not in MVP except a disabled Customize control on the main menu.
_Avoid_: client-side, mod, skin pack
