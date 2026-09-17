# Modular Chess

A chess platform: standard chess as the always-on rules authority, with optional Modes selected per Match. An Activity is what you came to do; a Mode is a rule pack on a Match.

## Language

**Core**:
The rules authority for standard FIDE chess. Always present in every Match. Not a Mode.
_Avoid_: module, engine, always-on Mode

**Activity**:
What you came to do. MVP Activities are Versus AI and Versus Friend. Later Activities include Puzzle, Survivor, and Auto Battler. Join is not an Activity.
_Avoid_: Opponent, Mode, game mode, match type

**Mode**:
An optional rule pack that hooks Core for one Match. Never replaces Core. A Mode declares which Activities may use it. After the player picks an Activity, only those Modes are offered. Join is never on that list. The first three Modes (Fog of War, Powerful Pieces, Martyr) allow Versus AI and Versus Friend only and are compatible with each other. Later Modes include Health and Chance Combat. A Match may stack several compatible Modes; both players play that same set.
_Avoid_: Add-on, DLC, mod, game mode, Activity

**Survivor**:
A later Activity. This Side fights swarms of enemies. Not a Mode. Not Auto Battler.
_Avoid_: Swarms as a second Activity, Mode, Versus AI

**Auto Battler**:
A later Activity. Pieces fight without the player choosing each Move. Not a Mode. Not Survivor.
_Avoid_: autobattler as a Mode, idle chess, Survivor

**Health**:
A later Mode. Pieces carry hit points for that Match. After a legal Capture, that Mode may wound instead of removing. Runs after Chance Combat miss; Extra Life does not apply to a wound. Does not change which Moves are legal. Not Core. Not an Activity.
_Avoid_: HP as Core, health bars as the Mode name, damage as Capture

**Chance Combat**:
A later Mode. After a legal Capture, that Mode may miss. Runs before Health and Extra Life; a miss does not wound. Does not change which Moves are legal. Not Core. Not an Activity.
_Avoid_: RNG as Core, XCOM as the Mode name, miss as illegal Move

**Join**:
A Play-screen action that enters a Versus Friend Lobby with a Join Code. Not an Activity. Not a spectator path. A Steam invite skips this and dumps the friend into the Lobby.
_Avoid_: Activity, Opponent, watcher

**Play**:
Main-menu path to pick Versus AI, Versus Friend, or Join. Then Mode multi-select (only Modes allowed for that Activity), each Mode’s settings, then Versus AI starts after confirm or Versus Friend goes to Lobby. Match Settings stay on the left until Start. Main menu also has Unlocks, a disabled Customize control, Options, Credits, Exit, and Feedback (Google Form). Escape closes the top popup, then the overlay, back toward the main menu.
_Avoid_: Opponent row

**Exit**:
Quits the Game. From the main menu, Exit and Escape show a confirmation first. Escape on that confirmation cancels. Not Resign, not Leave, not Pause.
_Avoid_: Resign, quit as Leave

**Host**:
The player who authors a Versus Friend Lobby and must own every Mode in that Match's Mode set on that platform. The friend must have the Mode content (download if missing) but need not own it in MVP; future matchmaking will require both to own. Changing the Mode set means leaving and creating a new Lobby. The Host Starts the Match; Start is allowed only when the other player is in the Lobby and synced. The friend sees the same Start control disabled, labeled “waiting for host.”
_Avoid_: owner, server

**Lobby**:
A Versus Friend room waiting for the second player. It ends when the Match starts or the Host leaves. If the Host leaves, the other player returns to Activity selection. If the other player leaves, the Host stays and the Join Code stays valid. Not matchmaking. The other player does not Start; they wait for the Host. No in-Match chat in MVP.
_Avoid_: queue, server browser, matchmaking, in-Match chat

**Join Code**:
A shareable code that admits a player to a Versus Friend Lobby. Not a spectator seat. Cross-play is allowed among desktop, Android, and iOS. No WebGL in MVP. On mobile this is the only join path. Steam invite is desktop-to-desktop only. No matchmaking in MVP.
_Avoid_: room ID, matchmaking, watcher, WebGL

**Match**:
One playthrough of Versus AI or Versus Friend: Core, Match Settings, and one shared Mode set, from setup until a terminal result. Versus AI has no Lobby; the Match starts after Modes and Match Settings are confirmed. On game over, Fog lifts: true Board, true PGN, Empowered marks, and Status.
_Avoid_: game, game mode, Activity

**Match history**:
A stored record of a finished Match: Moves as Square from–to plus a small Move kind and promotion PieceType; seconds the Match clock was actually ticking (both Sides; not Setup, Draft, Disconnect, or Pause; increment does not add; none stores 0); the Mode set and those Modes’ settings; Match Settings (main time, increment, resolved Host Side, Versus AI strength); and result (0 White, 1 Black, 2 Draw). No mover PieceType. Written only for a complete Match: Checkmate, Draw (including Stalemate and FIDE draws), Timeout, Resign, and Disconnect (that Side loses). Not Setup Leave. Not the Game. Not live notation. Bombard is one live Move (the Rook stays) and two history actions: from→target, then target→from. An Empowered King’s extra Move is two live Moves and two history actions; no synthetic return.
_Avoid_: game history, PGN as the store, Host as the winner field, Bombard as a single from–to that looks like a slide-capture

**Rematch**:
From Results, keep the same Modes and Match Settings. Versus Friend: new Lobby, same Host, same Join Code if the friend is still on Results; Host Starts; new Setup. Random Host color re-rolls. Versus AI: Confirm starts a new Match immediately. Either player may leave to Activity selection. If the friend already left, Host returns to Activity selection.
_Avoid_: skip Setup, keep last Empowered set

**Match Settings**:
Per-Match options that are part of Core, not a Mode. Host-authored and editable in the Lobby until the Match starts. Time control is two Host picks: main time (default none; none, Bullet 1 minute, Blitz 5 minutes, Rapid 15 minutes, Standard 30 minutes, or Extended 120 minutes) and increment (none, 1, 2, 5, 10, 15, 30, or 60 seconds). When main time is none, increment is none and that control is disabled. 0+0 is none. Host color (White, Black, or Random; White or Black shows in the Lobby immediately, Random resolves at Start before Setup), Versus AI strength (Easy, Medium, Hard; the AI uses the same Vision as a human — strength is play quality, not omniscience), and whether a Side may End Turn with zero Moves this Turn (default off).
_Avoid_: Base Settings, Time Pressure, Options, delay clock, hourglass, 0+0 as a second None, combined 10+5 labels, custom minutes

**Options**:
Account and app settings. Includes show notation. Not a Match and not Customization. With Fog of War, notation is per-Side: your Moves are full; opponent Moves are full only if you had Vision on the relevant Squares, otherwise a generic line. After the Match, a full true PGN is available.
_Avoid_: Match Settings, Customization, live true notation under Fog

**Unlocks**:
The catalog of Modes the player can Buy. Each Mode is shown owned or not; Buy happens on a covering popup, not on the row. MVP Buys Modes. Later also Activities and Customization. Core, Versus AI, Versus Friend, and Join are not Bought here. Ownership is per platform (Steam, iOS, Android), not synced across platforms in MVP. Versus Friend: the Host must own the Mode set; the friend downloads missing content and need not own.
_Avoid_: Shop, store, DLC menu

**Game**:
The product, Modular Chess. Not a playthrough.
_Avoid_: using "game" for a Match

**Customization**:
Account-level presentation: unit sets, colors, icons, BGM, and SFX. Not a Mode. Each player's unit set, colors, and icons appear on that player's pieces for both people. BGM and SFX stay on the local machine. Not in MVP except a disabled Customize control on the main menu.
_Avoid_: client-side, mod, skin pack

**Piece**:
A specific occupant of the Board: identity, Side, and current PieceType. Two knights of the same Side are two Pieces.
_Avoid_: unit, token, PieceType

**PieceType**:
The kind of a Piece. Core ships Pawn, Knight, Bishop, Rook, Queen, and King. A Mode may introduce additional kinds for a Match.
_Avoid_: Piece, class, role

**Empowered**:
A Piece chosen in Powerful Pieces Setup. Permanent for this Match, keyed by that Piece’s identity. Each Core PieceType has one power. The Queen’s power is one combined pattern: Queen plus Knight in a single Move, not an extra Move. The Rook’s power is passing through allied Pieces when moving; Vision follows that pattern. Empowered King: after that King Moves, the Turn stays open for an optional second Move by that same King, or End Turn. Castling is a King Move and opens the extra step (the King may then step, not castle again). Any other Piece’s Move ends the Turn as usual. Check is tested after each Move. Super Pawn: cannot Capture (forward Quiet only, including the double-step and promotion by advancing). May be Captured from any Square except the 3 adjacent Squares in front (forward and both forward diagonals). Sides, behind, Knights, distant front sliders, and en passant may Capture it. Super Pawn applies only while PieceType is Pawn; promotion or any change off Pawn drops it. Knight Extra Life: the first Capture of that Knight is negated (both Pieces stay, Move spent); then Extra Life is gone and the Empowered mark comes off — it is a normal Knight. Legality uses the result after Extra Life — if that Capture was the only Check escape, it is Checkmate. Unused Extra Life still carries through a type change. A Super Pawn on a Square in your pattern is shown identified even from a direction that cannot Capture it; that Capture is simply not legal. Empowered mark: shown on allied Pieces and on enemy Pieces that are identified in Vision; not on Shadow or Hidden. Empowered Bishop: may swap with an allied Pawn on any of the 8 neighboring Squares (King neighborhood), in lieu of a normal Move. Orthogonal swap flips the Bishop’s color; diagonal swap does not. Swap follows Core Check: illegal if it leaves your King in Check.
_Avoid_: Amazon, extra Queen Move, two patterns, buff, upgrade, Super Queen

**Side**:
White or Black. Who owns a Piece, and who is to move. Each player sees their Side at the bottom of the screen. No flip toggle in MVP. After the Match, review keeps that orientation.
_Avoid_: color, player, team, always White at bottom

**Square**:
One file and rank on the Board (a1–h8 in Core).
_Avoid_: tile, cell, position

**Board**:
Occupancy of Pieces on Squares. Core is 8×8 until a Mode changes it. A King is not replaced by placing another Piece on its Square. Not a Match Setting.
_Avoid_: map, grid

**Pattern**:
A Piece’s movement and capture geometry on the Board. Legal Moves, Attack, and Vision use it. Not legal Moves. Not Vision. Not a Mode.
_Avoid_: ray as the name, attack map, MoveGenerator

**Vision**:
Under Fog of War, the Squares a Side is shown. Each Piece grants Vision from its Pattern, not from Core legal Moves. Check and pins do not shrink Vision. A Pawn sees its forward push Squares and its capture Squares, not capture-only. A blocker on a Pawn’s forward Square is shown identified, same as a slider seeing the first occupied Square; that Pawn does not grant Vision behind the blocker (an unmoved Pawn does not see the double-step Square if the Square in front is occupied). An Empowered Rook’s Vision continues through allied Pieces and stops on the first enemy (identified). En passant: for the rest of that Side’s Turn, the capturing Pawn also sees the jumped enemy Pawn, identified. Allied Pieces are always shown. Home Vision: each Side always sees its own back 2 ranks, identified (empty or enemy). Home Vision is not a ray and does not grant Shadow onto the next rank. Under Fog, the HUD shows this Side’s castling rights only. Hidden opponent Moves play one generic SFX; identified Moves (Vision on from/to) and your own Moves use full SFX. Do not animate a Piece across Hidden Squares; a Piece that enters Vision pops in with no path. Fully Hidden Moves have no Piece motion. Last-move markers only on Squares in Vision. Opponent en passant is not a HUD flag; the capturing Pawn’s Vision window is the tell. Legal-move highlights are Core-legal destinations (those Squares are in Vision). Recomputed after every Move.
_Avoid_: attack-range-only, legal-move vision, memory, trails, Chess.com pawn-sensor, opponent castle HUD, unique Hidden SFX, ghost slides, opponent EP HUD, last-move through Fog

**Shadow**:
Under Fog of War, an unidentified enemy occupant exactly 1 Square beyond the edge of Vision on a **ray**: Bishop, Rook, Queen, and a Pawn’s forward short ray. Knight and King grant no Shadow. You know something is there, not PieceType or Side. Allies are never Shadow. Empty Squares 1 beyond Vision are Hidden, not Shadow. Pawn capture Squares are not a ray: no Shadow beyond them.
_Avoid_: radar, memory, knight halo

**Hidden**:
A Square not in Vision and not Shadow. Enemy occupancy is unknown.
_Avoid_: Shadow, fog (as the Mode name)

**Move**:
One Core state change: a Piece from–to, including castle, en passant, promotion, and Mode-registered Moves such as an empowered Bishop swapping with an allied Pawn on a neighboring Square or a Rook Bombard. Castling only with original unmoved Rooks, not Pieces that became Rooks. No premove in MVP.
_Avoid_: action, sub-move, Turn, premove

**Capture**:
A Move that would remove an enemy Piece from the Board, including en passant. After the Move is legal, a Mode may change the result: negate, miss, or wound instead of removing. When several apply: miss first, then wound vs remove, then Extra Life only if that Piece would still leave. The Move is still spent. Pieces that actually leave sit in the Capture tray.
_Avoid_: kill, take (as the noun)

**Capture tray**:
Presentation of Pieces that left the Board, ordered by when they left. This Side’s lost Pieces on the left; the opponent’s lost Pieces on the right. Extra Life bounce is not shown. Exiled Pieces sit in that pile with a chain until they return. Pieces float off and back onto the Board in an arc.
_Avoid_: graveyard pick, HUD icons instead of the parked Pieces

**Lost Material**:
Martyr’s monotonic point total of Pieces that actually left this Side’s Board (Pawn 1, Knight/Bishop 3, Rook 5, Queen 9, King 0). Recapture does not decrease it. Extra Life bounce and other negated Captures count 0. Summoned Pieces that leave still count 0. Threshold default 6 (Mode tunable). Both Sides’ totals and next threshold are public on the HUD.
_Avoid_: net deficit, capture attempts, hidden Martyr bar

**Draft**:
Martyr’s power pick after Lost Material crosses a threshold (default 6). Happens at the start of this Side’s next Turn, before they Move. The opponent finishes their Turn first, including extra King Moves. One Draft per this Side’s Turn; extra crossings queue and do not expire. Each Draft offers up to 3 unique options drawn from powers that are still obtainable and relevant. Durability: a power leaves the bag when this Side has obtained it up to its max. Never two copies of the same card in one Draft. If fewer than 3 obtainable relevant powers remain, show fewer cards. Cannot skip. This Side sees a description above the options when the Draft opens. 60s; timeout picks uniformly at random among the shown cards (and a random legal target if the power needs one). The Match clock pauses during Draft; Draft has its own 60s. The other player does not see the cards or that description. They see only a wait line: “Other player drafting in progress: 60 seconds left.” Versus AI Drafts instantly; that wait line is not shown. The result is public when it applies. Obtain limits: Fleet Pawns, Stasis Field, Untouchable King, Rally, Bombard, Phalanx max 1; Revival and Exile max 3; Reinforcements, Battlefield Promotion, and Knight Ascension infinite. Infinite and remaining-count powers re-trigger when drawn again. MVP pool: Reinforcements, Fleet Pawns, Untouchable King, Stasis Field, Knight Ascension, Battlefield Promotion, Rally, Revival, Exile. Bombard and Phalanx are not in use.
_Avoid_: Setup, shop roll, interrupt mid-Turn, dump all Drafts at once, pad with irrelevant, duplicate cards, exhaust-then-wrap

**Status**:
A timed overlay on a Piece (Stasis, Invulnerable). Summoned is a tag, not a timed Status. Duration counts the **affected Side’s** Turns and includes the Turn of application if that Side is to move. Ticks at the **end** of each affected Side’s Turn; apply now does not tick. N means N complete Turns. Untouchable King Drafted now: this Turn is 1 of 5. Stasis on the enemy Queen: your current Turn does not count; their next Turn is 1 of 3.
_Avoid_: global priority number, both-Sides Turn count, tick-at-start, Rooted

**Invulnerable**:
A Status. Other Pieces cannot target this Piece, so it cannot be in Check if it is a King. The owner may still Move it. It still occupies its Square, grants Vision, and may attack. Untouchable King applies Invulnerable to that King. Checkmate cannot end the Match through this King while Invulnerable. Stalemate still can.
_Avoid_: checkmate shield as a second rule, untargetable-but-in-Check

**Stasis**:
A Status from Martyr’s Stasis Field. Choose one enemy Queen (including promoted); if only one, apply automatically. Duration 3 of that Side’s Turns. Cannot Move, cannot be targeted or Captured, does not attack (no Check from her). Still occupies (sliders stop on her Square). Still grants Vision to her owner. Stasis Field is not offered in Draft while the opponent has no Queen. Max 1 obtain.
_Avoid_: freeze all Queens, original-Queen-only, Stasis blinds Vision

**Bombard**:
A Martyr power on your Rooks (replaces Wall Formation). Optional extra Move on top of normal Rook slides: along a rank or file, the first enemy at distance ≥ 5 with an otherwise empty path may be Captured; the Rook stays. Allies block the path; Empowered Rook pass-through does not apply. Extra Life or other illegal Capture: Bombard fails, Rook stays, Move spent. Vision uses the same ray. Slide-capture and Bombard onto the same target are two different Moves. Max 1 obtain. Not in use.
_Avoid_: Xiangqi Cannon, Wall Formation, hop a screen, replaces sliding Captures

**Summoned**:
A tag on Pieces created by Martyr (not a timed Status). 0 Lost Material if they leave. Not Empowered.
_Avoid_: timed summon

**Reinforcements**:
A Martyr power. After the card is picked, still in the same Draft 60s, this Side clicks up to 2 empty Squares on their back rank to place Summoned Pawns. Occupied Squares are skipped. Timeout fills remaining at random among those empties. If fewer than 2 empties, leftover summons vanish. Infinite obtains.
_Avoid_: airdrop, rank 2 overflow, overwrite occupants

**Battlefield Promotion**:
A Martyr power. The card is already Knight or Bishop (rolled when the Draft options are built). Same 60s: pick which surviving Pawn. One Pawn → apply immediately. Several Pawns → click one. Timeout: random surviving Pawn. Plays as that PieceType. Lost Material uses the new value unless Summoned (stays 0). Super Pawn drops. Infinite obtains.
_Avoid_: choose type after pick, promote all Pawns

**Fleet Pawns**:
A Martyr power. All your Pawns may move 2 forward from any rank when both Squares are empty. En passant stays Core: only the rank-2/7 jump. Capture still diagonal. Vision follows the 2-step ray. Max 1 obtain.
_Avoid_: EP on every 2-step

**Knight Ascension**:
A Martyr power. All surviving Knights on this Side become Rooks now. Later Knights stay Knights. Unused Extra Life carries; Empowered mark stays until Extra Life is spent. Those Rooks cannot castle. Infinite obtains: converts Knights that exist then; if none, that card does nothing extra.
_Avoid_: persistent no-Knights rule, choose which Knights

**Rally**:
A Martyr power. One-shot on the Turn of that Draft: this Side's first Move does not end the Turn. Any Piece may make the extra Move, or End Turn. Cap is two Moves this Turn. Empowered King's extra Move does not stack on Rally: after the second Move the Turn ends even if the first was that King. Not a second Turn: increment and Status wait until the Turn ends. Later Turns are normal. Max 1 obtain.
_Avoid_: extra Turn, skip opponent, persistent extra Move, three Moves with Empowered King

**Revival**:
A Martyr power. Brings back the last friendly Piece in the Capture tray (the last that physically left; Extra Life bounce never entered the tray). Skips Exiled Pieces still waiting to return. Removes it from the tray and places a new Summoned identity of that PieceType, not Empowered. Placement: uniformly random among empty Squares on the first rank from this Side’s back toward the opponent that still has an empty Square, then the next rank, and so on. Dud if the Board has no empty Square. Max 3 obtains. Not the Square it died on.
_Avoid_: Return of the Fallen, graveyard pick, last Lost Material only, tile, death Square, first empty file, farm Lost Material

**Exile**:
A Martyr power. Choose any enemy Piece except the King (one target → apply immediately). It leaves the Board and sits in the Capture tray with a chain. Not Stasis, and not Lost Material. Duration 2 of that Side’s Turns; your current Turn does not count. Ticks at the end of each of their Turns. Returns at the end of their second Turn, so they have it for their third Turn. Return Square: the Square it left if empty, otherwise the first empty Square walking from that Side’s back rank toward the opponent. Max 3 obtains. Animate off and back in an arc. Chain sprite later.
_Avoid_: freeze in place, Stasis, destroy forever, exile the King, Lost Material farm

**Phalanx**:
A Martyr power. Max 1 obtain. Not in use. Intended: this Side’s Pawns cannot Move and cannot be Captured for 2 of this Side’s Turns.
_Avoid_: freeze the whole Board

**Turn**:
One Side's opportunity to make one or more Moves. FIDE default is one Move. A Mode may allow further Moves before the Turn ends. Extra Moves are optional. That Side's clock runs for the whole Turn. Increment is added when the Turn actually ends, not after a middle extra Move (Empowered King or Rally).
_Avoid_: action, sub-move, round

**Check**:
The Side to move's King is attacked on the Board. Fog does not change this. Show Check when Core says Check, even if the attacker is Hidden. Invulnerable: the owner may still Move that King; no other Piece may target it, so it cannot be in Check. That King still occupies its Square. Sliding is blocked unless a Mode already allows passing through that occupant.
_Avoid_: threat, attack (as the noun), hide Check banner

**Checkmate**:
The Side to move is in Check and has no legal Move. Ends the Match. An Invulnerable King cannot be in Check, so cannot be Checkmated; Stalemate still can.
_Avoid_: Check, checkmate-shield as a second pipeline

**Stalemate**:
The Side to move is not in Check and has no legal Move. Ends the Match as a draw. Invulnerable and Stasis do not prevent Stalemate.
_Avoid_: Checkmate, skip Turn

**Draw**:
Threefold, 50-move, and insufficient material use the true Board and are automatic. Fog does not change them. Summoned Pawns still prevent insufficient material.
_Avoid_: per-Side draw, disable FIDE draws in Fog

**Timeout**:
That Side loses immediately when their clock hits zero, even mid-Move, unless a Mode says otherwise.
_Avoid_: flag

**Disconnect**:
After Match Start (including Setup), a **60s** reconnect window. Match clock pauses if it is running. The other player sees a wait line with seconds left. If that player does not return, that Side loses. Not Timeout.
_Avoid_: Timeout, ragequit, tick-during-reconnect, menu Leave

**Resign**:
A Side may Resign only after Turn 1 has started. They lose. Not allowed during Setup. After Turn 1, menu Leave is Resign (immediate, no reconnect window).
_Avoid_: forfeit, quit

**Offer Draw**:
Versus Friend, from Turn 1, on your Turn only. Not Setup, not during Draft or Disconnect. The other player Accepts, Declines, or Moves (a Move declines). Versus AI has no Offer Draw.
_Avoid_: offer on their Turn, Setup draw spam

**Pause**:
Versus AI only. Stops the Match clock if any, and the AI. Resume continues the same Match. Not during Setup or Draft. Versus Friend has no Pause. Escape Pauses Versus AI when no popup is open and it is not Setup or Draft. No takebacks, hints, or engine bar in MVP.
_Avoid_: Friend Pause, Pause as Resign, takeback, hint, Escape as Exit during a Match

**Setup**:
After Start, before Turn 1. Powerful Pieces: each Side must select exactly N Empowered Pieces (default 2), then Confirm. Confirm stays visible and not interactable until N are selected. Duplicate types allowed. Shared 30s clock. Versus AI already selected; your Confirm starts Turn 1 immediately. Versus Friend: Confirm becomes interactable when both Sides have selected N. The first Confirm does not start the Match; the other Side’s button reads Confirm (opponent ready). The second Confirm starts Turn 1. Timeout → autopick remaining slots uniformly at random from unselected own Pieces (duplicate types allowed), then reveal. Unconfirm of picks is allowed until both have Confirmed or time runs out; dropping below N clears Confirm. The clock does not reset. Picks stay hidden until both have Confirmed or time runs out; then marks apply and Fog still hides enemy marks on Shadow or Hidden Squares. Your own picks are visible to you. Resign is not allowed. Menu Leave during Setup aborts: no winner, both return to Activity selection. Versus AI Setup Leave returns to the menu.
_Avoid_: draft, pick phase, up to N, live enemy picks

**End Turn**:
Closes a Turn that did not end after a Move. Shown only after a Move that left the Turn open (Empowered King extra Move or Rally), on that Side’s HUD. Hidden otherwise. Slides in from the right. By default a Side cannot End Turn with zero Moves this Turn. End Turn is illegal while that Side is in check, unless a Mode changes that.
_Avoid_: Pass, skip, Options drawer, always-on End Turn
