# Modular Chess

A chess platform for Steam Early Access: Core occupancy, Pattern, and Turn always on; FIDE Law by default for Versus Activities; optional Modes selected per Match. An Activity is what you came to do; a Mode is a rule pack on a Match. An Activity may supply a different Law. Meta progression uses Merit spent in Unlocks. No Steam DLC in EA.

## Language

**Core**:
Always-on occupancy, Piece identity, Pattern geometry, and Turn structure. Present in every Match and every Stage. Not FIDE. Not Law. Not a Mode.
_Avoid_: module, engine, always-on Mode, FIDE, rules authority

**Law**:
The chess-law pack for a Match or a Stage: King mobility, promotion, slider range, Check obligations, and what ends that Match or Stage. Versus AI and Versus Friend use FIDE Law. An Activity may supply a different Law. Not Core. Not a Mode.
_Avoid_: Ruleset, FIDE as a Mode, Core as FIDE, rules authority

**Activity**:
What you came to do. EA Activities are Versus AI, Versus Friend, and Campaign. Later Activities include Survivor, Auto Battler, and a chess-roguelike Activity that uses Run and Stage instead of Match. Versus AI and Versus Friend use FIDE Law; an Activity may supply a different Law. Join is not an Activity.
_Avoid_: Opponent, Mode, game mode, match type, Puzzle (as an Activity name)

**Mode**:
An optional rule pack that hooks Core for one Match. Never replaces Core. Never replaces Law. A Mode declares which Activities may use it. After the player picks an Activity, only those Modes are offered. Join is never on that list. A Mode may disallow hotseat (same-device Sit in Lobby); Fog of War does. EA Modes: Fog of War, Powerful Pieces, Martyr, Action Economy, Complex Terrain, Randomizer — mutually compatible unless a Mode says otherwise. Later Modes include Health and Chance Combat. A Match may stack several compatible Modes; both players play that same set. Mode content ships in the build; Unlocks gate who may Host-author a Mode, not whether the friend can play it.
_Avoid_: Add-on, DLC, mod, game mode, Activity

**Fog of War**:
A Mode. Each Side sees only Squares in Vision; all else is Hidden. No Shadow. Compatible with Complex Terrain (Forest adds local cover). Disallows hotseat.
_Avoid_: FOW as Core, Shadow, memory fog

**Powerful Pieces**:
A Mode. In Setup each Side spends a shared Host-tuned Empower budget to Empower own Pieces. Each Core PieceType has one power and one Empower cost.
_Avoid_: PP as Core, fixed pick-N as the only Setup rule

**Martyr**:
A Mode. Lost Material unlocks Drafts. Obtained powers are public on the HUD for both Sides after each Draft resolves. During Draft the opponent sees only the wait line.
_Avoid_: Martyr as Activity, hidden permanent power list

**Action Economy**:
A Mode. Each Turn that Side receives a Host-tuned pool of action points (default 3; range 2–16). A normal Move spends 1 action point. Empowered King follow-up and Rally extra cost 0 action points. Other Modes may grant further 0-cost Moves. No global cap on Moves per Turn. End Turn is available when at least one Move was made and action points remain or a 0-cost extra is still available. Without this Mode, FIDE default is one Move unless another Mode opens the Turn.
_Avoid_: always-3, hard Move cap, King follow-up spending a point

**Complex Terrain**:
A Mode. The Board carries terrain on Squares: Swamp, Forest, Mountain. Versus: Host picks a layout preset or tunable random layout. Campaign: authored per level. Works with or without Fog of War.
_Avoid_: terrain as Core, requires Fog, Randomizer as the terrain placer

**Swamp**:
Terrain. Enter-and-stop: a Move that would pass through a swamp Square must stop on that Square if the Move is otherwise legal. Capture onto swamp is allowed. The Piece ends that Move on the swamp.
_Avoid_: action-point tax as the swamp rule, impassable swamp

**Forest**:
Terrain. Passable. Blocks vision through that Square (rays treat it like a vision blocker). The forest Square itself can be in Vision; an enemy occupant there is Hidden unless this Side has Vision that reveals it. Allied Pieces in forest remain fully visible to their owner. Without Fog of War, the rest of the Board stays fully visible — Forest is local cover only.
_Avoid_: impassable forest, Fog-required forest

**Mountain**:
Terrain. Impassable. Blocks movement and vision like a wall. Never holds an occupant.
_Avoid_: enterable mountain, hill

**Randomizer**:
A Mode. Host multi-selects one or more options: Shuffle Start (non-King Pieces shuffled in the starting army; Kings stay), Random Colors (each Piece gets a random tint; White sprite is the base; shared seed so both clients match), Random Placement (non-King Pieces placed randomly on that Side’s half; King on that Side’s back rank). Any non-empty subset is legal. Compatible with Fog of War. Does not place Complex Terrain; terrain layouts stay on Complex Terrain.
_Avoid_: three separate Modes, Random as Match Setting only

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

**Campaign**:
An Activity. An authored sequence of levels that teach chess and Mode mechanics and award Merit. EA targets about 50 levels; the Steam demo is the first 10. Not Versus. Not a Mode. Not Puzzle.
_Avoid_: Puzzle Activity, Run, Stage, matchmaking

**Campaign level**:
One authored Board and win criteria inside Campaign. Awards up to 3 Merit via Stars. Replayable; each Star’s Merit is earned once. Not a Match.
_Avoid_: Match, Stage, puzzle as the type name

**Star**:
One Merit criterion on a Campaign level: complete the level, beat the turn limit, and stay within the loss limit (e.g. lose 0 Pieces). Each Star grants 1 Merit the first time it is earned.
_Avoid_: medal, grade, repeatable Merit per Star

**Merit**:
Persistent unlock currency. Primary source: Campaign Stars. Also: first three finished Versus checkmates award 3, then 2, then 1 once; afterward 1 Merit per finished Versus Match (checkmate, resign, timeout, or draw), max 3 per day. Spent in Unlocks. Synced via Steam Cloud. Not Elo. Not a skill rating.
_Avoid_: Chess Rating, coin, gem, XP as the shop wallet

**Join**:
A Play-screen action that enters a Versus Friend Lobby with a Join Code. Not an Activity. Not a spectator path. A Steam overlay invite skips this and dumps the friend into the Lobby.
_Avoid_: Activity, Opponent, watcher

**Play**:
Main-menu path to pick Versus AI, Versus Friend, Join, or Campaign. For Versus: Mode multi-select (only Modes the Host has unlocked for authoring, and allowed for that Activity), each Mode’s settings, then Versus AI starts after confirm or Versus Friend goes to Lobby. Match Settings stay on the left until Start. Main menu also has History (if unlocked), Unlocks, Customize, Options, Credits, Exit, Feedback, Legal, and Language Select. Escape closes the top popup, then the overlay, back toward the main menu.
_Avoid_: Opponent row

**Language Select**:
Main-menu control (corner). Opens a grid of flags with each language named in that language. Never gated by Merit. Not inside Options.
_Avoid_: Options language dropdown as the only path

**Legal**:
Main-menu control (corner) for legal / policy text. Not gated by Merit.
_Avoid_: Options legal burrow

**Exit**:
Quits the Game. From the main menu, Exit and Escape show a confirmation first. Escape on that confirmation cancels. Not Resign, not Leave, not Pause.
_Avoid_: Resign, quit as Leave

**Host**:
The player who authors a Versus Friend Lobby. Must have unlocked every Mode in that Match’s Mode set (Host-author right). The friend may play those Modes without unlocking them. Changing the Mode set means leaving and creating a new Lobby. The Host Starts the Match; Start is allowed only when the other player is in the Lobby and synced. The friend sees the same Start control disabled, labeled “waiting for host.”
_Avoid_: owner, server, both-must-unlock, DLC ownership

**Lobby**:
A Versus Friend room waiting for the second player. It ends when the Match starts or the Host leaves. If the Host leaves, the other player returns to Activity selection. If the other player leaves, the Host stays and the Join Code stays valid. Sit (same-device hotseat) is disabled when any Mode in the set disallows hotseat; the Sit control is not interactable and reads Sit (disabled by mode). Not matchmaking. The other player does not Start; they wait for the Host. No in-Match chat in EA. Entered via Join Code or Steam overlay invite.
_Avoid_: queue, server browser, matchmaking, in-Match chat, hotseat Fog

**Join Code**:
A shareable code that admits a player to a Versus Friend Lobby. Not a spectator seat. Steam desktop EA only — no mobile, no WebGL, no cross-play in EA. Steam overlay invite is an alternate admit path. No matchmaking in EA.
_Avoid_: room ID, matchmaking, watcher, WebGL, cross-play MVP

**Match**:
One playthrough of Versus AI or Versus Friend: Core, FIDE Law, Match Settings, and one shared Mode set, from setup until a terminal result. Versus AI has no Lobby; the Match starts after Modes and Match Settings are confirmed. On game over, Fog lifts: true Board, true PGN, Empowered marks, and Status.
_Avoid_: game, game mode, Activity, Run, Stage, Campaign level

**Run**:
A 13-Stage playthrough of a later Activity. Ends if the player's King is captured; won by beating the Stage 13 boss. Not a Match. No meta-progression between Runs.
_Avoid_: Match, Season, Campaign, New Game+

**Stage**:
One Board in a Run. This Activity's Law applies when the Stage starts. Ends when that Stage's target Piece is captured. Not a Match.
_Avoid_: Match, level, round, Turn, Campaign level

**Match history**:
A stored record of a finished Match, written only for a complete Match: Checkmate, Draw (including Stalemate and FIDE draws), Timeout, Resign, and Disconnect (that Side loses). Not Setup Leave. Not the Game. Not live notation. Payload is a compact event log for re-simulation: history actions (Square from–to, Move kind, promotion PieceType), Setup Empowered picks, Draft picks and targets, and Mode settings — enough to rebuild Piece identity, PieceType, Empowered, Status, and summons. Also stores clock seconds actually ticking (both Sides; not Setup, Draft, Disconnect, or Pause; increment does not add; none stores 0), the Mode set and those Modes’ settings, Match Settings (main time, increment, resolved Host Side, Versus AI strength), result (0 White, 1 Black, 2 Draw), and when the Match ended. Bombard is one live Move and two history actions: from→target, then target→from. An Empowered King’s extra Move is two history actions. Replay re-sims this log; it does not write a second store. Records that lack the event-log payload stay listed in History but cannot open Replay. Versus Friend: each device writes its own record when the Match completes. Synced via Steam Cloud subject to the History cap.
_Avoid_: game history, PGN as the store, Host as the winner field, Bombard as a single from–to that looks like a slide-capture, Replay as the store, full Board snapshot per action, Host-only Match history

**History**:
Main-menu overlay of past Match history records. Starts locked at cap 0 (control not usable until the first History tier is bought). Merit tiers set the cap to 5, then 10, then 20, then 50 (costs 1 / 3 / 5 / 10 Merit). Scroll list, newest first; oldest drop when over cap. No clear-all and no per-row delete. Each row shows date/time, Activity as `Versus AI` or `Versus Friend`, Mode names, and result as Win, Loss, or Draw from this player’s Side. Tap a row to select it. Bottom controls: Replay and Back. Empty list shows a short empty-state line with Replay disabled. History is main-menu only. Not Match history (the store). Not Replay (the session).
_Avoid_: free default-10 cap, Options slider as the only cap control, Replay menu, unlimited keep-all, History mid-Match, White/Black as the row result, Join as the History Activity label, clear History

**Replay**:
A viewing session of one Replayable Match history record. Uses the normal Match Board and Match HUD, not a separate screen. Not interactive play. Match clocks are hidden. Controls at the bottom: Auto play, Auto play speed (shown only while Auto play is on), Next Move, Last Move, Restart, Leave. No Draft cards, no Mode popups. Next/Last advance one history action per click. Next on the final action and Last on the opening are no-ops. Setup picks and Draft results apply instantly with no UI when that event is reached. Auto play advances actions; the wait applies when a Turn ends. Default Turn wait is 2 seconds; speed is a multiplier on that wait: 0.5×, 1×, 2×, 3×, 5×. When Auto play reaches the final action, it stops and stays on the end position. Tapping Next, Last, or Restart while Auto play is on stops Auto play, then applies that control. Entered from Results Replay or from History’s Replay. Restart resets to the opening position of that record. Leave ends the Replay (from Results path → main menu; from History path → History with the same row still selected). Escape does the same as Leave. Options is not available mid-Replay. Match HUD top line is `Replaying · {date} · {Activity} · {Mode names}`, replacing the Side-to-move line. Notation follows whether show notation is unlocked; when shown, lines are the full true log as actions advance (no Fog-obscured lines). Capture tray / lost material updates as captures re-sim, same as a live Match. Board clicks are view-only and do not attempt Moves. Piece clicks play `tap.wav` and may show status; empty/Hidden-looking Squares play `piece-move.wav`; never invalid-click. Replay Vision is review: Fog regions stay marked, but Pieces are always shown identified, and Empowered/Status auras show whenever those marks exist. Not Rematch. Not a live Match.
_Avoid_: Play Again, Rematch, spectator as Join, editing history, 3s default wait, Draft UI in Replay, Match clocks in Replay, Fog-obscured notation in Replay, Options mid-Replay, interactive Moves in Replay

**Results**:
The game-over panel after a Match ends. Bottom button group only: Rematch, Replay, Leave. Replay dismisses Results and opens a Replay of this Match’s history at the opening position. Leave returns to the main menu. Rematch follows Rematch. Not Options.
_Avoid_: Play Again, extra Results actions beyond that trio, Results kept under Replay

**Rematch**:
From Results, keep the same Modes and Match Settings. Versus Friend: new Lobby, same Host, same Join Code if the friend is still on Results; Host Starts; new Setup. Random Host color re-rolls. Versus AI: Confirm starts a new Match immediately. Either player may leave to Activity selection. If the friend already left, Host returns to Activity selection. Versus Friend Rematch waits for the other player; if they Leave, that control greys out and reads that they left.
_Avoid_: Play Again, skip Setup, keep last Empowered set

**Match Settings**:
Per-Match options that are part of Core, not a Mode. Host-authored and editable in the Lobby until the Match starts. Time control is two Host picks: main time (default none; none, Bullet 1 minute, Blitz 5 minutes, Rapid 15 minutes, Standard 30 minutes, or Extended 120 minutes) and increment (none, 1, 2, 5, 10, 15, 30, or 60 seconds). When main time is none, increment is none and that control is hidden. 0+0 is none. Host color (White, Black, or Random; White or Black shows in the Lobby immediately, Random resolves at Start before Setup), Versus AI strength (Easy, Medium, Hard; the AI uses the same Vision as a human — strength is play quality, not omniscience), and whether a Side may End Turn with zero Moves this Turn (default off).
_Avoid_: Base Settings, Time Pressure, Options, delay clock, hourglass, 0+0 as a second None, combined 10+5 labels, custom minutes

**Options**:
App settings that are free and ungated: audio volumes, animation speed, and similar accessibility/feel controls. Main-menu only — not available mid-Match or mid-Replay. Language lives on Language Select, not here. Show notation is an Unlocks purchase (1 Merit), not a free Options toggle. History cap is purchased in Unlocks, not an Options slider. Not a Match and not Customization. With Fog of War, notation is per-Side: your Moves are full; opponent Moves are full only if you had Vision on the relevant Squares, otherwise a generic line. After the Match, a full true PGN is available.
_Avoid_: Match Settings, Customization, live true notation under Fog, Options mid-Match, language inside Options, History size in Options

**Unlocks**:
The Merit catalog. Buys Modes, History tiers, show notation, Customization (board themes, piece sets, BGM packs), and other gated presentation. Buy happens on a covering popup, not on the row. Core, Versus AI, Versus Friend, Join, Campaign, Language Select, Legal, Credits, Exit, Feedback, and free Options are not Bought here. No Steam DLC in EA. Mode prices (Merit): Fog of War 6, Powerful Pieces 12, Martyr 15, Action Economy 13, Complex Terrain 10, Randomizer 15. History tiers 1/3/5/10 → caps 5/10/20/50. Show notation 1. Board themes 2–5 each; piece sets 6–20 each; BGM packs 8–15 each. Versus Friend: Host must have unlocked the Mode set to author it; the friend need not.
_Avoid_: Shop, store, DLC menu, real-money Mode packs, Chess Rating

**Demo**:
Steam demo build. First 10 Campaign levels. Mode Unlocks offered: Fog of War, Powerful Pieces, Martyr only. Campaign-only Merit is tuned so a player can unlock at most two of those three. Progress (Merit, Stars, owned Unlocks) migrates into the full game when possible.
_Avoid_: full Mode catalog in demo, all-three Modes from Campaign alone

**Game**:
The product, Modular Chess, on Steam Early Access ($6 base, no DLC). Not a playthrough.
_Avoid_: using "game" for a Match

**Customization**:
Account-level presentation bought with Merit: unit sets, colors, board themes, BGM, and SFX packs. Not a Mode. Each player's unit set, colors, and icons appear on that player's pieces for both people. BGM and SFX stay on the local machine. Basic colors are cheap; chess sets are expensive.
_Avoid_: client-side, mod, skin pack, free Customize in EA

**Display name**:
On Steam builds, the visible name in Versus AI and Versus Friend is the player’s Steam persona. Not a local account stem.
_Avoid_: account creation name as the Steam-facing label

**Type scale**:
UI uses a 12-pixel-based pixel font. Font sizes are multiples of 12 only. Default body size is 24. Default button and panel chrome for text backgrounds is 200×32. Author prefabs to that grid; do not invent off-grid sizes in new UI.
_Avoid_: arbitrary point sizes, non-multiple-of-12 fonts, freeform button heights

**Piece**:
A specific occupant of the Board: identity, Side, and current PieceType. Two knights of the same Side are two Pieces.
_Avoid_: unit, token, PieceType

**PieceType**:
The kind of a Piece. Core ships Pawn, Knight, Bishop, Rook, Queen, and King. A Mode may introduce additional kinds for a Match.
_Avoid_: Piece, class, role

**Empower cost**:
Points spent from the Powerful Pieces budget to Empower one Piece of that PieceType: Queen 3, King 2, Rook 2, Knight 2, Bishop 1, Pawn 1.
_Avoid_: equal cost per Piece, pick slots

**Empowered**:
A Piece chosen in Powerful Pieces Setup. Permanent for this Match, keyed by that Piece’s identity. Each Core PieceType has one power. The Queen’s power is one combined pattern: Queen plus Knight in a single Move, not an extra Move. The Rook’s power is passing through allied Pieces when moving; Vision follows that pattern. Empowered King: after that King Moves (spending 1 action point if Action Economy is on), the Turn stays open for an optional second Move by that same King at 0 action-point cost, or End Turn. Castling is a King Move and opens the extra step (the King may then step, not castle again). Check is tested after each Move. Super Pawn: cannot Capture (forward Quiet only, including the double-step and promotion by advancing). May be Captured from any Square except the 3 adjacent Squares in front (forward and both forward diagonals). Sides, behind, Knights, distant front sliders, and en passant may Capture it. Super Pawn applies only while PieceType is Pawn; promotion or any change off Pawn drops it. Knight Extra Life: the first Capture of that Knight is negated (both Pieces stay, Move spent); then Extra Life is gone and the Empowered mark comes off — it is a normal Knight. Legality uses the result after Extra Life — if that Capture was the only Check escape, it is Checkmate. Unused Extra Life still carries through a type change. A Super Pawn on a Square in your pattern is shown identified even from a direction that cannot Capture it; that Capture is simply not legal. Empowered mark: shown on allied Pieces and on enemy Pieces that are identified in Vision; not on Hidden. Empowered Bishop: may swap with an allied Pawn on any of the 8 neighboring Squares (King neighborhood), in lieu of a normal Move. Orthogonal swap flips the Bishop’s color; diagonal swap does not. Swap follows Core Check: illegal if it leaves your King in Check.
_Avoid_: Amazon, extra Queen Move, two patterns, buff, upgrade, Super Queen, King follow-up spending an action point

**Side**:
White or Black. Who owns a Piece, and who is to move. Each player sees their Side at the bottom of the screen. No flip toggle in EA. After the Match, review keeps that orientation.
_Avoid_: color, player, team, always White at bottom

**Square**:
One file and rank on the Board (a1–h8 in Core).
_Avoid_: tile, cell, position

**Board**:
Occupancy of Pieces on Squares, plus terrain markers when Complex Terrain is on. Core is 8×8 until a Mode changes it. A King is not replaced by placing another Piece on its Square. Not a Match Setting. During a live Match, Square clicks: invalid-click SFX only when the player attempts an illegal Move (not on casual empty clicks or other non-Move taps). Clicking a Piece (yours or the opponent’s) plays `Assets/Audio/tap.wav` and may show that Piece’s status. Clicking an empty Square — including one that only holds a Hidden Piece — plays `Assets/Audio/piece-move.wav`. Replay uses the same inspect/empty-tile SFX and never plays invalid-click.
_Avoid_: map, grid, invalid-click on every empty tap

**Pattern**:
A Piece’s movement and capture geometry on the Board. Legal Moves, Attack, and Vision use it. Not legal Moves. Not Vision. Not a Mode.
_Avoid_: ray as the name, attack map, MoveGenerator

**Vision**:
Under Fog of War, the Squares a Side is shown. Each Piece grants Vision from its Pattern, not from Core legal Moves. Check and pins do not shrink Vision. A Pawn sees its forward push Squares and its capture Squares, not capture-only. A blocker on a Pawn’s forward Square is shown identified, same as a slider seeing the first occupied Square; that Pawn does not grant Vision behind the blocker (an unmoved Pawn does not see the double-step Square if the Square in front is occupied). An Empowered Rook’s Vision continues through allied Pieces and stops on the first enemy (identified). Forest and Mountain block vision through their Squares per Complex Terrain. En passant: for the rest of that Side’s Turn, the capturing Pawn also sees the jumped enemy Pawn, identified. Allied Pieces are always shown. Home Vision: each Side always sees its own back 2 ranks, identified (empty or enemy). Under Fog, the HUD shows this Side’s castling rights only. Hidden opponent Moves play one generic SFX; identified Moves (Vision on from/to) and your own Moves use full SFX. Do not animate a Piece across Hidden Squares; a Piece that enters Vision pops in with no path (Fog may fade as Vision advances). Fully Hidden Moves have no Piece motion. Last-move markers only on Squares in Vision. Opponent en passant is not a HUD flag; the capturing Pawn’s Vision window is the tell. Legal-move highlights are Core-legal destinations (those Squares are in Vision). Recomputed after every Move. No Shadow.
_Avoid_: attack-range-only, legal-move vision, memory, trails, Shadow, Chess.com pawn-sensor, opponent castle HUD, unique Hidden SFX, ghost slides, opponent EP HUD, last-move through Fog

**Hidden**:
A Square not in Vision. Enemy occupancy is unknown.
_Avoid_: Shadow, fog (as the Mode name)

**Move**:
One Core state change: a Piece from–to, including castle, en passant, promotion, and Mode-registered Moves such as an empowered Bishop swapping with an allied Pawn on a neighboring Square or a Rook Bombard. Castling only with original unmoved Rooks, not Pieces that became Rooks. No premove in EA. Under Action Economy a normal Move spends 1 action point unless a Mode says that Move costs 0.
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
Martyr’s power pick after Lost Material crosses a threshold (default 6). Happens at the start of this Side’s next Turn, before they Move. The opponent finishes their Turn first, including extra King Moves. One Draft per this Side’s Turn; extra crossings queue and do not expire. Each Draft offers up to 3 unique options drawn from powers that are still obtainable and relevant. Situational powers are excluded from that Draft’s options when their trigger is absent (same pattern as Stasis Field needing an enemy Queen) — not dead-drawn. Durability: a power leaves the bag when this Side has obtained it up to its max. Never two copies of the same card in one Draft. If fewer than 3 obtainable relevant powers remain, show fewer cards. Cannot skip. This Side sees a description above the options when the Draft opens. 60s; timeout picks uniformly at random among the shown cards (and a random legal target if the power needs one). The Match clock pauses during Draft; Draft has its own 60s. The other player does not see the cards or that description. They see only a wait line: “Other player drafting in progress: 60 seconds left.” Versus AI Drafts instantly; that wait line is not shown. When the pick applies, it is public and appended to that Side’s obtained-power list on the HUD. Obtain limits: Fleet Pawns, Untouchable King, Stasis Field, Rally, Iron Curtain, Turncoat, Overload, Reserve Call, Fog Vision, Bombard, Phalanx max 1; Second Front, Vanishing Act, Blood Debt, Rearguard, Dust Cloud max 2; Revival, Exile, Landmine max 3; Reinforcements, Battlefield Promotion, Knight Ascension infinite. Infinite and remaining-count powers re-trigger when drawn again. EA pool: Reinforcements, Fleet Pawns, Untouchable King, Stasis Field, Knight Ascension, Battlefield Promotion, Rally, Revival, Exile, Second Front, Iron Curtain, Turncoat, Vanishing Act, Blood Debt, Rearguard, Overload, Landmine, Reserve Call, Fog Vision, Dust Cloud. Bombard and Phalanx are not in use.
_Avoid_: Setup, shop roll, interrupt mid-Turn, dump all Drafts at once, pad with irrelevant, duplicate cards, exhaust-then-wrap, hidden permanent power list, dead-draw situational cards

**Status**:
A timed overlay on a Piece (Stasis, Invulnerable, Rearguard). Side-level Martyr effects (Iron Curtain, Blood Debt, Fog Vision, Dust Cloud, Landmine, Reserve Call, Overload) are not Piece Status. Summoned is a tag, not a timed Status. Duration counts the **affected Side’s** Turns and includes the Turn of application if that Side is to move. Ticks at the **end** of each affected Side’s Turn; apply now does not tick. N means N complete Turns. Untouchable King Drafted now: this Turn is 1 of 3. Stasis on the enemy Queen: your current Turn does not count; their next Turn is 1 of 3.
_Avoid_: global priority number, both-Sides Turn count, tick-at-start, Rooted

**Invulnerable**:
A Status. Other Pieces cannot target this Piece, so it cannot be in Check if it is a King. The owner may still Move it. It still occupies its Square, grants Vision, and may attack. Untouchable King applies Invulnerable to that King for 3 of that Side’s Turns. Checkmate cannot end the Match through this King while Invulnerable. Stalemate still can.
_Avoid_: checkmate shield as a second rule, untargetable-but-in-Check, 5-Turn Untouchable King

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
A Martyr power. One-shot on the Turn of that Draft: grants one extra Move at 0 action-point cost (with Action Economy) / does not end the Turn after the first Move (without Action Economy). Any Piece may make that extra Move, or End Turn. Stacks with Empowered King follow-up; no global Move cap. Not a second Turn: increment and Status wait until the Turn ends. Later Turns are normal. Max 1 obtain.
_Avoid_: extra Turn, skip opponent, persistent extra Move, hard two-Move cap, King and Rally mutually exclusive

**Revival**:
A Martyr power. Brings back the last friendly Piece in the Capture tray (the last that physically left; Extra Life bounce never entered the tray). Skips Exiled Pieces still waiting to return. Removes it from the tray and places a new Summoned identity of that PieceType, not Empowered. Placement: uniformly random among empty Squares on the first rank from this Side’s back toward the opponent that still has an empty Square, then the next rank, and so on. Dud if the Board has no empty Square. Max 3 obtains. Not the Square it died on.
_Avoid_: Return of the Fallen, graveyard pick, last Lost Material only, tile, death Square, first empty file, farm Lost Material

**Exile**:
A Martyr power. Choose any enemy Piece except the King (one target → apply immediately). It leaves the Board and sits in the Capture tray with a chain. Not Stasis, and not Lost Material. Duration 2 of that Side’s Turns; your current Turn does not count. Ticks at the end of each of their Turns. Returns at the end of their second Turn, so they have it for their third Turn. Return Square: the Square it left if empty, otherwise the first empty Square walking from that Side’s back rank toward the opponent. Max 3 obtains. Animate off and back in an arc. Chain sprite later.
_Avoid_: freeze in place, Stasis, destroy forever, exile the King, Lost Material farm

**Phalanx**:
A Martyr power. Max 1 obtain. Not in use. Intended: this Side’s Pawns cannot Move and cannot be Captured for 2 of this Side’s Turns.
_Avoid_: freeze the whole Board

**Second Front**:
A Martyr power. After the card is picked, still in the Draft 60s, this Side clicks one empty Square in their back 2 ranks to place a Summoned Knight. Timeout fills at random among those empties. Dud if none. Max 2 obtains.
_Avoid_: airdrop past rank 2, overwrite occupants

**Iron Curtain**:
A Martyr power. For 2 of this Side’s Turns, enemy Pieces cannot end a Move on this Side’s back 2 ranks. Situational — offered only if this Side has at least one Piece on their own back 2 ranks. Max 1 obtain. Public on the HUD.
_Avoid_: blocks leaving those ranks, Fog-required

**Turncoat**:
A Martyr power. Choose an enemy Pawn; it becomes a Summoned Pawn of this Side on its current Square. Max 1 obtain.
_Avoid_: convert non-Pawns, keep enemy identity

**Vanishing Act**:
A Martyr power. Choose a threatened allied Piece worth more than a Pawn, then an empty Square in this Side’s half; teleport it there. Situational — offered only if such a threatened Piece exists. Max 2 obtains.
_Avoid_: teleport Pawns, enemy half, unthreatened Pieces

**Blood Debt**:
A Martyr power. Side-level charge: the next time an allied Piece is Captured, the enemy Piece that Captured it is immediately Captured in return (Extra Life and other Capture resolution still apply to that return Capture). Opponent is shown that Blood Debt is active. Max 2 obtains (charges stack).
_Avoid_: hidden Blood Debt, mechanical cap instead of transparency

**Rearguard**:
A Martyr power. Choose 2 Pawns on this Side’s back 2 ranks; for 3 of this Side’s Turns they cannot be Captured (Rearguard Status). Timeout fills at random among legal Pawns. If fewer than 2, apply to those available. Max 2 obtains.
_Avoid_: any-rank Pawns, Invulnerable as the name

**Overload**:
A Martyr power. Choose a non-King allied Piece; for the rest of this Turn it may make one extra Move (two Moves total by that Piece), then is removed from the Board when the Turn ends or after its second Move. Those Moves may never deliver Check. Situational — offered only when this Side’s King is not in Check. Max 1 obtain.
_Avoid_: King Overload, Check-giving Overload Moves, survives past the Turn

**Landmine**:
A Martyr power. Choose an empty Square on this Side’s half; the first enemy Piece to end a Move there is Captured. Visible only to the owner. Max 3 obtains.
_Avoid_: public Landmine markers, enemy-half mines

**Reserve Call**:
A Martyr power. Side-level arm: the next time this Side loses a non-Pawn Piece, place a Summoned Pawn on the Square it was lost on and move the capturing enemy Piece back to its prior Square. Max 1 obtain.
_Avoid_: triggers on Pawn loss, stacks with Extra Life bounce as a second arm

**Fog Vision**:
A Martyr power. For 1 of this Side’s Turns, this Side sees all enemy Piece positions identified. Situational — offered only when Fog of War is in the Match’s Mode set. Max 1 obtain.
_Avoid_: requires Dust Cloud, permanent reveal

**Dust Cloud**:
A Martyr power. For 2 of this Side’s Turns, Vision of this Side’s half of the Board is Hidden to the opponent (allied Pieces remain visible to their owner). Offered in Versus AI and Versus Friend; does not require Fog of War. Max 2 obtains.
_Avoid_: Fog-required Dust Cloud, hides own Pieces from owner

**Turn**:
One Side's opportunity to make one or more Moves. FIDE default is one Move. Action Economy replaces that with an action-point pool. A Mode may grant further 0-cost Moves. Extra Moves are optional. That Side's clock runs for the whole Turn. Increment is added when the Turn actually ends, not after a middle extra Move (Empowered King or Rally).
_Avoid_: action, sub-move, round

**Check**:
A Core fact: the Side to move's King is attacked on the Board. Fog does not change this. Show Check when the King is attacked, even if the attacker is Hidden. Whether Check forces a reply or can end the Match is Law. Invulnerable: the owner may still Move that King; no other Piece may target it, so it cannot be in Check. That King still occupies its Square. Sliding is blocked unless a Mode already allows passing through that occupant.
_Avoid_: threat, attack (as the noun), hide Check banner, Check as FIDE obligation

**Checkmate**:
FIDE Law: the Side to move is in Check and has no legal Move. Ends the Match. An Invulnerable King cannot be in Check, so cannot be Checkmated; Stalemate still can. Not Core. Other Law may omit Checkmate.
_Avoid_: Check, checkmate-shield as a second pipeline, Checkmate as Core

**Stalemate**:
FIDE Law: the Side to move is not in Check and has no legal Move. Ends the Match as a Draw. Invulnerable and Stasis do not prevent Stalemate. Not Core. Other Law may omit Stalemate.
_Avoid_: Checkmate, skip Turn, Stalemate as Core

**Draw**:
FIDE Law: threefold, 50-move, and insufficient material use the true Board and are automatic. Fog does not change them. Summoned Pawns still prevent insufficient material. Not Core. Other Law may omit these.
_Avoid_: per-Side draw, disable FIDE draws in Fog, Draw as Core

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
Versus AI only. Stops the Match clock if any, and the AI (including Autoplay). Resume continues the same Match. Not during Setup or Draft. Versus Friend has no Pause. Escape Pauses Versus AI when no popup is open and it is not Setup or Draft. No takebacks, hints, or engine bar in EA.
_Avoid_: Friend Pause, Pause as Resign, takeback, hint, Escape as Exit during a Match

**Autoplay**:
A Debug-only toggle that drives the local PlayerSide with the same Move AI as Versus AI (Setup/Draft autopick as that AI). Difficulty is chosen on the Debug menu and applies only to Autoplay’s Side — not the Versus AI opponent — so the two strengths can differ. Default Easy. Arms from the main menu and engages in a Match; mid-Match Start takes over on the next PlayerSide action. Pause freezes it; Leave clears it; game over and Rematch do not. After game over, if still on, Rematch fires after about 10 seconds when Rematch is available (Versus AI always; Versus Friend only if that path is available). Does nothing in Replay. Not compiled into release builds. Not Auto Battler.
_Avoid_: Auto Battler, autobattler, idle chess, controlling both Sides

**Setup**:
After Start, before Turn 1. Powerful Pieces: each Side spends the Host-tuned Empower budget (default 4; range 3–20; Campaign may override). Must spend exactly the full budget. Duplicate types allowed. Pieces whose Empower cost exceeds remaining budget are not selectable (greyed). Shared 30s clock. Versus AI already selected; your Confirm starts Turn 1 immediately. Versus Friend: Confirm becomes interactable when both Sides have spent exactly the budget. The first Confirm does not start the Match; the other Side’s button reads Confirm (opponent ready). The second Confirm starts Turn 1. Timeout → autopick remaining spend uniformly at random among affordable unselected own Pieces, then reveal. Unconfirm of picks is allowed until both have Confirmed or time runs out; leaving the exact budget clears Confirm. The clock does not reset. Picks stay hidden until both have Confirmed or time runs out; then marks apply and Fog still hides enemy marks on Hidden Squares. Your own picks are visible to you. Resign is not allowed. Menu Leave during Setup aborts: no winner, both return to Activity selection. Versus AI Setup Leave returns to the menu.
_Avoid_: draft, pick phase, exactly-N picks, live enemy picks, partial budget spend

**End Turn**:
Closes a Turn that did not end after a Move. Shown when a Move left the Turn open (remaining action points, Empowered King extra, Rally, or other 0-cost extras), on that Side’s HUD. Hidden otherwise. Slides in from the right. By default a Side cannot End Turn with zero Moves this Turn. End Turn is illegal while that Side is in check, unless a Mode changes that.
_Avoid_: Pass, skip, Options drawer, always-on End Turn
