# Modular Chess
## Game Design Document — v0.1

---

## 1. Vision & Core Concept

**Modular Chess** is a classic chess ruleset wrapped in a plug-in "Add-on" architecture. The base game is complete, balanced, standard chess — fully playable on its own. Add-ons are self-contained rule modules that layer new mechanics on top of the base game. Each Add-on:

- Can be toggled on/off independently
- Declares its own **compatibility rules** (which other Add-ons it can run alongside, and which it locks out)
- Is designed as a future purchasable DLC unit

The long-term vision is a chess "platform" rather than a single ruleset — players assemble their own match type from a library of Add-ons, similar to modded tabletop games or DLC-driven strategy titles.

---

## 2. Base Game — Core Chess Module

Standard FIDE chess rules, unmodified:

- 8x8 board, standard starting position
- Standard movement/capture rules for Pawn, Knight, Bishop, Rook, Queen, King
- Check, checkmate, stalemate, castling, en passant, pawn promotion
- Standard point values used by other Add-ons for scoring: Pawn = 1, Knight/Bishop = 3, Rook = 5, Queen = 9, King = untradeable

The Core module owns the **authoritative game state** (board, turn order, legal move generation, win/draw conditions). Every Add-on hooks into this state rather than replacing it — this is what keeps the base game playable standalone and keeps Add-ons composable.

---

## 3. Add-on Architecture

### 3.1 Design Goals
- Add-ons must never require editing Core code — they subscribe to Core events and inject rule modifications
- Add-ons must be able to declare hard exclusivity with other Add-ons
- Add-ons must be independently loadable/unloadable per match (supports the DLC vision — a player without an Add-on installed simply can't select it)

### 3.2 Compatibility System
Each Add-on declares metadata:

```
AddonDefinition
├── ID (unique string, e.g. "fog_of_war")
├── DisplayName
├── IncompatibleWith[]  (list of AddonIDs)
├── RequiredWith[]      (optional — for Add-ons that only make sense bundled)
├── ConfigurableParams[] (exposed tunables, see 3.4)
```

Before a match starts, the lobby/setup screen builds the selected Add-on list and validates it against every `IncompatibleWith` entry. If two selected Add-ons conflict, the UI blocks the second selection and explains why (e.g., "Fog of War and Reveal-All are incompatible").

By default, **Fog of War**, **Powerful Pieces**, and **Martyr** are mutually compatible and are the reference case for how multi-Add-on stacking should behave.

**Resolved (see 11.6):** for v1, Add-on selection is symmetric — both players play with the same active Add-on set for a given match. Asymmetric loadouts (different Add-ons per player) are out of scope until a future mode.

### 3.3 Hook Points (what Add-ons are allowed to modify)
To keep this composable, define a fixed set of extension points in Core that Add-ons subscribe to, rather than letting Add-ons touch board state directly:

| Hook | Purpose | Used by |
|---|---|---|
| `OnMoveRequested(piece, from, to)` | Validate/override legality | Powerful Pieces |
| `OnPieceCaptured(piece, capturedBy)` | React to captures | Martyr |
| `OnTurnStart(player)` | Per-turn setup (recompute vision, tick timers) | Fog of War, Martyr |
| `GetVisibleTiles(player)` | Determine what's rendered | Fog of War |
| `GetPieceMoveSet(piece)` | Override a piece's legal move pattern | Powerful Pieces, Martyr |
| `OnMatchStart(players)` | Pre-game setup choices (e.g., picking empowered pieces) | Powerful Pieces |
| `GetPieceScoreValue(piece)` | Override point value (e.g., 0 for summoned units) | Martyr |

### 3.4 Technical Recommendation (Unity)
Given a data-driven ScriptableObject architecture, each Add-on maps naturally to its own `AddonDefinition` ScriptableObject, holding its config values (LOS range modifiers, threshold values, toggles) as serialized fields. A `MatchConfig` ScriptableObject then holds the list of active `AddonDefinition` references for a given match, and Core queries active Add-ons at each hook point via an interface (e.g., `IMoveRule`, `IVisibilityRule`, `ICaptureListener`) that each Add-on's runtime component implements. This keeps Add-ons as swappable, inspector-configurable assets — which also maps cleanly onto a future DLC delivery model (an Add-on becomes an importable asset bundle/package).

---

## 4. Add-on 01: Fog of War

### 4.1 Concept
Players only see what their own pieces can currently observe. The rest of the board is hidden, and intel decays — nothing is remembered once it leaves vision.

### 4.2 Visibility Rules
- **Home vision:** Each player always sees their own back 2 rows, regardless of piece presence or LOS.
- **Line of Sight (LOS):** Each piece grants vision equal to its *attack range*:
  - Pawn: 1 tile diagonally forward (its capture squares)
  - Knight: its 8 possible L-shaped landing tiles
  - Bishop/Rook/Queen: sliding vision along their movement lines, blocked by the first occupied tile in each direction (occupied tile itself is visible; nothing behind it is)
  - King: 1 tile in all directions
- **Shadow tiles:** Any tile exactly 1 tile beyond the edge of a piece's LOS is rendered as an unidentified shadow — the player knows *something* occupies that tile, but not its type or owner. **Resolved (see 11.7):** this ambiguity only applies to enemy pieces. A player always knows the position of their own pieces regardless of LOS — allied pieces are simply rendered normally wherever they are, never as a shadow and never hidden.
- **Everything else** is fully hidden (blank fog) — this applies to enemy positions only, per the above.
- **No memory/no trails:** Visibility is recomputed fresh after every individual sub-move (see 4.3) from current piece positions. A tile that was visible a moment ago but isn't currently in LOS returns to fog immediately — pieces do not "stay revealed" after moving out of view.

### 4.3 Tunable Parameters
| Param | Default | Notes |
|---|---|---|
| `homeVisionRows` | 2 | Rows always visible from the player's own back edge |
| `shadowRingWidth` | 1 tile | Width of the unidentified-shadow band beyond LOS |
| `visionRecalcTiming` | After every individual sub-move | **Resolved (see 11.3).** Recomputed after each sub-move rather than once per turn, so a multi-action turn (e.g. King's double-move under Powerful Pieces) updates fog progressively as it's taken |

### 4.4 Design Notes / Edge Cases
- Check/checkmate detection still uses full board state under the hood (Core's authoritative state) — fog only affects what's *rendered* to each player, not move legality computation. This avoids illegal-move exploits from hidden information.
- A king moving into what would be check from a hidden piece is still illegal — Core knows, even if the player didn't see it coming. This is an intentional tension: fog creates guessing, not information-based cheating.
- Castling and en passant remain legal per standard rules; visibility doesn't gate legality, only player knowledge.
- Because vision recalculates per sub-move, a piece taking multiple actions in one turn (King's double-move) can reveal new fog *between* its own two moves — the player sees the board update live rather than only at turn's end.

---

## 5. Add-on 02: Powerful Pieces

### 5.1 Concept
At match start, each player selects a configurable number of their own pieces (default: **2**, exposed as `empoweredPieceCount`) to imbue with a fixed, type-specific power. The power is tied to piece **type** — selecting a piece grants it that type's predefined ability.

### 5.2 Piece Powers
| Piece | Power |
|---|---|
| Pawn (Super Pawn) | Can only be captured from the 3 tiles directly behind it (i.e., immune to capture from the front/sides) |
| Rook | Can pass through allied pieces when moving (still blocked by enemy pieces) |
| Knight | Ignores death once — the first time it would be captured, it survives and the capture is negated instead |
| Bishop | Can swap positions with any allied Pawn directly adjacent to it (in lieu of a normal move) |
| Queen | Gains Knight movement in addition to standard Queen movement |
| King | May take 2 actions (moves) in a single turn |

### 5.3 Selection Rules
- Selection happens during `OnMatchStart`, before turn 1.
- A player may select multiple pieces of the same type (e.g., empower 2 different pawns) if `empoweredPieceCount` allows.
- Empowered status is permanent and tied to the specific piece instance for the match (if a Knight with "ignore death once" survives a capture, its power is now spent — it reverts to a normal Knight afterward).
- Empowerment is tracked on the piece instance, not its current type — if another Add-on changes a piece's type mid-match (e.g. Martyr's Knight Ascension, see 6.3/11.5), an unused power carries over rather than being stripped.

### 5.4 Tunable Parameters
| Param | Default | Notes |
|---|---|---|
| `empoweredPieceCount` | 2 | How many pieces each player may empower |
| `allowDuplicateTypeSelection` | true | Whether both empowered picks can be the same piece type |

### 5.5 Interaction Notes
- **With Fog of War:** **Resolved (see 11.1).** A piece's granted power fully extends its LOS to match — e.g. an empowered Queen sees in knight-move patterns as well as standard Queen lines, not just her original attack pattern. `GetVisibleTiles` should compute LOS from the *current* effective move/attack set (post-power), not a cached pre-power set.
- King's double-move should still respect check rules after *each* individual move, not just at the end of the turn (prevents moving through check). Combined with Fog of War's per-sub-move recalculation (4.3), this means the King's second move is chosen with updated vision from the first.

---

## 6. Add-on 03: Martyr

### 6.1 Concept
A roguelike power-unlock system driven by loss. As a player accumulates lost material (by point value), they cross thresholds that unlock game-changing powers — turning being behind on material into a comeback mechanic.

### 6.2 Core Loop
- Track cumulative point value of each player's captured pieces (using standard values: Pawn 1, Knight/Bishop 3, Rook 5, Queen 9).
- **Resolved (see 11.2):** the tracked value is **total material ever lost**, a monotonically increasing counter — it does *not* decrease if the player recaptures material later. Recapturing changes the board state and score, but not Martyr progress; once lost, a piece's value has already counted toward the next threshold.
- At each threshold crossing (`martyrThreshold`, default every 6 points lost), the player is offered a choice of **2 random powers** drawn from their remaining unlock pool (roguelike draft-style pick).
- Unlocked powers apply immediately and persist for the rest of the match.

### 6.3 Example Power Pool
| Power | Effect |
|---|---|
| Reinforcements | Summon 3 Pawns onto your back row (summoned units have 0 point value — capturing them grants the opponent no Martyr progress and no material score) |
| Fleet Pawns | All Pawns can always move 2 tiles forward, not just on their first move |
| Wall Formation | Rooks can shift allied pieces one tile out of the way when passing through them |
| Untouchable King | King is invulnerable (cannot be captured, though can still be put in check) for 5 turns. **Resolved (see 11.9):** since standard chess ends by checkmate rather than literal capture, this means the King cannot be checkmated for the duration — a move that would otherwise deliver checkmate still delivers check, but the match does not end; if no legal escape exists, the King simply remains in check (out of danger) until the invulnerability window ends |
| Stasis Field | Enemy Queen is frozen for 3 turns — cannot move and cannot be captured |
| Knight Ascension | All surviving Knights become Rooks. **Resolved (see 11.5):** if an affected Knight was empowered by Powerful Pieces (ignore-death-once), that power carries over onto the resulting Rook — an unused "ignore death once" survives the type change |
| Battlefield Promotion | Turn one surviving Pawn into a Knight or Bishop (player's choice) |

### 6.4 Tunable Parameters
| Param | Default | Notes |
|---|---|---|
| `martyrThreshold` | 6 points | Material lost per unlock tier |
| `draftPoolSize` | 2 | How many random powers are offered per unlock |
| `maxUnlocks` | Uncapped | **Resolved (see 11.4).** Intentionally uncapped — a long, lopsided game should let the losing player keep drafting powers |

### 6.5 Design Notes
- Because summoned units carry 0 point value, they can't be farmed by the opponent to trigger their *own* Martyr progress — this keeps Reinforcements from backfiring.
- Powers should be excluded from the draft pool once unlocked (no duplicate offers) and excluded if contextually irrelevant (e.g., don't offer Knight Ascension if the player has no Knights left) **while any fresh, relevant option remains**.
- **Resolved (see 11.8):** once the pool is exhausted of fresh/relevant options, the draft wraps around and re-offers already-unlocked powers, stacking their effect where the power supports it (e.g., another Reinforcements re-triggers a fresh summon of 3 Pawns; re-drafting Untouchable King or Stasis Field refreshes/extends its duration rather than doing nothing).

---

## 7. Add-on Compatibility Matrix (reference)

| | Fog of War | Powerful Pieces | Martyr |
|---|---|---|---|
| **Fog of War** | — | ✅ Compatible | ✅ Compatible |
| **Powerful Pieces** | ✅ Compatible | — | ✅ Compatible |
| **Martyr** | ✅ Compatible | ✅ Compatible | — |

All three ship compatible with each other. Future Add-ons that redefine the same hook in conflicting ways (e.g., two Add-ons both trying to override King movement) should declare `IncompatibleWith` against each other rather than trying to merge behavior.

---

## 8. Monetization / DLC Framing

- Core Chess: free / included base game.
- Each Add-on: individually purchasable, unlocked per-account, selectable in match setup only if owned (and only if compatible with the rest of the selected set).
- Future Add-on packs could bundle 2-3 thematically related modules (e.g., a "Stealth Pack" bundling Fog of War with a future vision-manipulation Add-on) at a discount versus buying individually.
- Match setup UI should clearly show locked (unowned) Add-ons as previewable-but-greyed-out, to drive interest without blocking base functionality.

---

## 9. Future Add-on Ideas (not yet in scope)
- **Reveal Pulse** — periodic full-board reveal for a few seconds (interacts with Fog of War; would need explicit compatibility handling)
- **Draft Chess** — pre-game piece-value trading, layers under Powerful Pieces' selection step
- **Time Pressure** — chess clock variants as a separate systemic Add-on

---

## 10. Open Design Questions
_All initial open questions have been resolved — see Section 11 for the decision log. New questions will land here as design continues._

---

## 11. Decision Log

Decisions below were reached through design review and are treated as resolved for v0.1. Each entry records the decision and why, so the reasoning doesn't get re-litigated by accident later.

### 11.1 Empowered-piece vision follows the granted power
**Decision:** When a Powerful Pieces power changes a piece's movement/attack pattern, its Fog of War vision extends to match the new pattern, not just its original one.
**Why:** Keeps vision and combat capability consistent — a piece that can *attack* along a new pattern can also *see* along it. Avoids a confusing case where a piece could legally move somewhere it technically couldn't "see" coming.
**Implication:** `GetVisibleTiles` must read a piece's current effective move/attack set (post-power) rather than a cached base pattern.

### 11.2 Martyr thresholds track total-ever-lost, not net deficit
**Decision:** Martyr progress is based on cumulative material lost over the whole match — a counter that only increases. Recapturing material does not roll back Martyr progress.
**Why:** Keeps the comeback mechanic simple and monotonic; a player who fought back to material parity still "earned" their unlocks from the rough patch that got them there, rather than losing access to powers they already drafted.
**Implication:** Track this as a separate running total distinct from net material score — the two numbers can diverge over a match.

### 11.3 Fog of War recalculates after every individual sub-move
**Decision:** Vision recomputes after each sub-move, not once per turn.
**Why:** Multi-action turns (King's double-move under Powerful Pieces) need fog to update *between* those actions so the second move is chosen with current information, not stale end-of-last-turn vision.
**Implication:** `OnTurnStart` is no longer the sole vision-recalc hook — vision recalculation needs to fire on every completed sub-move, which for a single-move turn is functionally the same as recalculating once, but for multi-action turns fires more than once.

### 11.4 Martyr unlocks are intentionally uncapped
**Decision:** No cap on total Martyr unlocks per match.
**Why:** The Add-on exists specifically to give a losing player a comeback path; capping it would blunt that in exactly the long, lopsided games where it matters most.
**Implication:** Balance testing should focus on per-power strength and draft-pool exhaustion behavior (6.5) rather than a hard unlock ceiling.

### 11.5 Powers persist through type-changing effects
**Decision:** An empowered piece's unused power carries over if another Add-on changes its type mid-match (e.g. Martyr's Knight Ascension turning an empowered Knight into a Rook keeps its unused "ignore death once").
**Why:** Consistent with 5.3 — empowerment belongs to the piece instance, not its type. Stripping the power on a type change would make Knight Ascension a trap for a player's own empowered pieces.
**Implication:** Power state needs to live on a per-instance component independent of the piece-type field, so a type swap doesn't implicitly reset it.

### 11.6 Add-on selection is symmetric for v1
**Decision:** Both players must run the same active Add-on set in a given match. Asymmetric loadouts are out of scope for now.
**Why:** Keeps v1 balance and UI scope manageable; asymmetric Add-ons open a much larger design space (fairness, matchmaking, mirrored vs. non-mirrored powers) better tackled as a deliberate future mode rather than a default.
**Implication:** Match setup only needs one shared Add-on selection step, not a per-player one — simplifies the lobby flow in 3.2.

### 11.7 Shadow ambiguity applies to enemy pieces only
**Decision:** A player always sees their own pieces fully, everywhere on the board, regardless of LOS. The shadow/fog system only obscures enemy pieces.
**Why:** A player inherently knows their own board state — hiding your own pieces from yourself would be an artificial memory-test, not a fog-of-war mechanic. Uncertainty should come from the opponent's hidden information, not your own.
**Implication:** `GetVisibleTiles` needs to special-case ownership: allied pieces bypass the LOS/shadow/fog pipeline entirely and render at full fidelity.

### 11.8 Martyr draft wraps around once the pool is exhausted
**Decision:** When every relevant power is already unlocked, further threshold crossings re-offer already-unlocked powers, stacking their effect where the power supports it (e.g. another Reinforcements, or a refreshed/extended duration for a timed power).
**Why:** Keeps the comeback mechanic meaningful all the way through a very lopsided match instead of thresholds eventually doing nothing.
**Implication:** Each power needs a defined "re-trigger" behavior (re-summon, extend duration, etc.), not just a single first-unlock effect.

### 11.9 Untouchable King means immunity to checkmate, not literal capture
**Decision:** During its duration, the King cannot be checkmated — a would-be checkmating move still delivers check, but the match doesn't end. If no legal escape exists, the King stays in check, out of danger, until the window expires.
**Why:** Standard chess has no literal king-capture step to make invulnerable — checkmate is the actual loss condition, so that's what the power needs to suspend.
**Implication:** Win-condition checking needs an explicit "is this player's King currently checkmate-immune" gate, evaluated before declaring game-over, separate from normal legal-move generation.
