# A Turn may contain more than one Move

Core’s default is FIDE: Apply a Move, then the Turn ends. Modes may keep the same Side to move after a Move (for example Powerful Pieces: that King may Move again this Turn; extra King Moves are optional). Core does not implement action points. A future Action Points Mode would use the same “does this Move end the Turn?” registration, not a new Core resource. Fog of War recomputes after every Move.

When a Move leaves the Turn open, the player is offered End Turn. They cannot End Turn with zero Moves this Turn unless Match Settings allow it (default off). End Turn is illegal while that Side is in check, unless a Mode changes that. The Side’s clock runs for the whole Turn, including extra Moves.
