# gendash
Solver and Generator for Boulder Dash/Supaplex style "caves"

The Solver is an implementation of Iterative Deepening A*

The Generator is random for now, based on the frequency of certain characters uin a string. This will be changed to a Wave Collapse 
function once I have enough working caves to based it on.

While the caves generated are compatible with Boulder Dash's mechanics, they are not compatible with the game itself.
Boulder Dash uses a deterministic random generator for it's cave, but not for gameplay. Two examples comes to mind:

* Boulder pushing/grabbing: Boulder Dash has a 1 in 8 chance of actually pushing a rock when the player move.
This cause the character to "skid" randomly when trying to push a rock, which can't be done in puzzle form,
not without some burdensome graphical cues.

* Amoeba grow randomly. While I didn't implement them in the puzzle yet, they will likely be completely different
than in the base game once I do.

Implemented so far:

* Empty space
* Dirt
* Rocks
* Diamonds
* Brick Walls
* Steel Walls
* Explosions
* Butterflies
* Fireflies

Magic walls are next, as they work well in a puzzle. Custom, non-BD related blocks are also planned.
