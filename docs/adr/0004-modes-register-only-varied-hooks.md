# Modes register only the hooks they vary

ADR-0001 is the registration seam; this records how a Mode uses it. A Mode attaches only the Core hooks it actually changes. Unused no-op methods on a fat adapter are rejected: that interface would grow with every future Mode and stay shallow. Selected Modes register at Match start from that Match’s Mode set. `ModeCatalog` stays names and ownership, not behavior. Core remains the FIDE default; FIDE is not a Mode. Modes stay in the Core assembly — the seam is registration, not a new assembly.

The first two hooks are legal Moves and Capture resolution. Vision and occupancy are not this seam. When Capture resolution stacks, attachments declare priority (ADR-0002): Chance Combat (miss) first, then Health (wound vs remove), then Powerful Pieces Extra Life only if the Piece would still leave. A wound is not a Capture Extra Life can negate.
