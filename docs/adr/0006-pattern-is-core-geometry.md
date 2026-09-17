# Pattern is Core geometry

Movement and capture geometry lives in one Core Pattern module. Legal Moves, Attack, and Vision call it. Super Pawn’s front corridor is Pattern; whether a Piece is a Super Pawn is Powerful Pieces. Empowered Rook pass-through is a slider flag from that Mode, not a second slider. Pins and Check stay out of Pattern. Pattern is not a Mode hook (ADR-0004). Three copies of rays are rejected; a new PieceType does not add a fourth loop. Extract by strangler: shared primitives first, not a MoveGenerator rewrite, not a Fog rewrite in the same pass.
