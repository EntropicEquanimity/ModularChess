# Modes register kinds and behavior with Core

Core ships the six FIDE PieceTypes and a registration surface. For a given Match, selected Modes may register extra PieceTypes and attach behavior (legal moves, capture, vision, scoring, and similar hooks) without editing Core. Instance-only effects (empowerment, spent ignore-death, stasis) stay Mode-owned and keyed by Piece identity, not a new PieceType. A Match with no Modes has only the six FIDE kinds.

Vanilla play stays Core. New kinds and overlays exist only when that Mode is in the Match’s Mode set. The first three Modes register no new kinds; they only attach behavior.
