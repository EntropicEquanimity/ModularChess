# Modular Chess

A chess platform: standard chess as the always-on rules authority, with optional Add-ons selected per Match, and a Mode that only chooses the opponent relationship.

## Language

**Core**:
The rules authority for standard FIDE chess. Always present in every Match. Not an Add-on.
_Avoid_: module, base Add-on, engine

**Add-on**:
An optional rule pack that hooks Core for one Match. Never replaces Core.
_Avoid_: module, DLC, mod

**Match**:
One playthrough of Core plus one shared Add-on set, from setup until a terminal result.
_Avoid_: game, game mode, Mode

**Mode**:
The opponent relationship for a Match — who plays, and how they connect. Not the rules and not the Add-on set. Default Modes are Versus AI and Versus Friend.
_Avoid_: game mode, match type, playlist

**Game**:
The product, Modular Chess. Not a playthrough.
_Avoid_: using "game" for a Match
