# About
This is a place for me to jot down random _unimplemented_ ideas before I forget
them.  Once an idea is implemented, it should be removed from this file and
proper documentation written.

# Documentation
* A file for "undocumented" ideas that have been _implemented.
    * By "undocumented", I mean not documented _with enough detail_.

* Documentation for intended behavior that is _currently_ implemented

* Documentation for how the code/project is laid out.
    * Particularly: the player's finite state machine

# Features
* A minimum height for every type of jump
* Doing a double jump on sloped ground gives a speed boost
* Gaining/losing momentum from sloped surfaces
    * Going downhill gives you momentum
        * Perhaps only while rolling?
    * Going uphill saps momentum
    * Jumping specifically _does not_ sap momentum, so the optimal strategy
        is to jump uphill and run downhill
    * The extra speed gained should carry with you for a while after the ground
        gets flat again
    
* Bumping your head on the ceiling while jumping should take away your VSpeed
    * This used to already be in here, but it doesn't work anymore

* Don't let the camera zoom inside the walls; it makes it seem like the wall
    doesn't exist, which can confuse players.
    * Alternatively: continue allowing this, but make the polygons double-sided
        and make the texture translucent while the camera is inside it

* "Zoom" ramps that launch you somewhere else when you roll into them, kind of
    like launch stars in Mario Galaxy.

# Refactorings
* A separate state for skidding
* Less disgusting state machine
    * Rename the concept of "states" to something else("actions"?) because "state"
        is overloaded.