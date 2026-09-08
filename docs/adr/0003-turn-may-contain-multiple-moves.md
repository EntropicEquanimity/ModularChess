# A Turn may contain more than one Move

Core’s default is FIDE: Apply a Move, then the Turn ends. Modes may keep the same Side to move after a Move (for example Powerful Pieces: that King may Move again this Turn). Core does not implement action points. A future Action Points Mode would use the same “does this Move end the Turn?” registration, not a new Core resource. Fog of War recomputes after every Move.
