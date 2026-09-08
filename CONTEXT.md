# Modular Chess

A chess platform: standard chess as the always-on rules authority, with optional Modes selected per Match, and an Opponent choice that only picks who you play.

## Language

**Core**:
The rules authority for standard FIDE chess. Always present in every Match. Not a Mode.
_Avoid_: module, engine, always-on Mode

**Mode**:
An optional rule pack that hooks Core for one Match. Never replaces Core. A Match may stack several compatible Modes; both players play that same set.
_Avoid_: Add-on, DLC, mod, game mode

**Opponent**:
Who plays a Match, and how they connect. Default Opponents are Versus AI and Versus Friend.
_Avoid_: Mode, game mode, match type, playlist

**Host**:
The player who authors a Versus Friend Match and must own every Mode in that Match's Mode set.
_Avoid_: owner, server

**Match**:
One playthrough of Core plus one shared Mode set, from setup until a terminal result.
_Avoid_: game, game mode

**Game**:
The product, Modular Chess. Not a playthrough.
_Avoid_: using "game" for a Match

**Customization**:
Account-level presentation: unit sets, colors, icons, BGM, and SFX. Not a Mode. Each player's unit set, colors, and icons appear on that player's pieces for both people. Not in MVP except a disabled Customize control on the main menu.
_Avoid_: client-side, mod, skin pack
