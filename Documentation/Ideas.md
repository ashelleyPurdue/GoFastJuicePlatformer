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

* Use "speed tiers" to reward players instead of directly increasing HSpeed
    * Speed tiers are remeniscent like the various tiers of "boost" in Crash Team Racing
    * Each tier has its own maximum HSpeed
    * You stay in your current speed tier until:
        * You get a boost with a tier higher than your current one
        * Your invisible "reserve tank" runs out
        * Your HSpeed drops below a certain threshold
        * You perform certain actions that forcibly set you to a particular
            tier
    * Every time you do something that gives you a boost, it adds a little to
        your "reserve tank".
    * Your "reserve tank" depletes a little for every frame that you're on the
        ground.  Bunny hopping is therefore a good strategy to make your
        reserves last longer
    * Certain actions count as a "boost"
        * Each boost has a tier.  If the boost's tier is higher than your
            current speed tier, then your current speed tier is upgraded to the
            boost's tier.
        * Each boost adds a little bit to your reserves

# Refactorings
* A separate state for skidding
* Less disgusting state machine
    * Rename the concept of "states" to something else("actions"?) because "state"
        is overloaded.