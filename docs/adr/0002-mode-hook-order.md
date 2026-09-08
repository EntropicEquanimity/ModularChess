# Mode behavior attachments run in order

When several Modes attach behavior to the same Core hook, Core runs all of them. It does not reject the stack. Default order is the Host’s selected Mode list. An attachment may pass a priority for cases that must run before or after the others; equal priority falls back to list order. IncompatibleWith remains for Modes that must not be selected together at all.
