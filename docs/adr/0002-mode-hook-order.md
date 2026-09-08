# Mode behavior attachments run in order

When several Modes attach behavior to the same Core hook, Core runs all of them. It does not reject the stack. Default order is the Host’s selected Mode list. An attachment may pass a priority; higher priority runs earlier (setup). Equal priority falls back to list order. IncompatibleWith remains for Modes that must not be selected together at all.
