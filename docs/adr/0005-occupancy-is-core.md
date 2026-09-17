# Occupancy is Core, not a Mode hook

Empty vs occupied vs King-locked is Core occupancy, not a Mode attachment (ADR-0004). Callers ask two questions: is this Square empty, and may a Piece be placed here (must be empty; never onto a King). Board refuses to replace a King. Martyr placement (Reinforcements, Revival, Exile return) and Match targeting use those answers; Match does not invent empty-Square policy. Tests hit Core, not click handling.
