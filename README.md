# gendash
Solver and Generator for Boulder Dash/Supaplex style "caves"

In C#, using Dot Net Core 2.1. Coded in VSCode.

The Solver is an implementation of Iterative Deepening A*

The Generator is random for now, based on a  pattern file. This might be changed to a wave collapse 
function once I have enough working caves to based it on.

While the caves generated are compatible with Boulder Dash's mechanics, they are not compatible with the game itself.
Boulder Dash uses a deterministic random generator for it's cave, but not for gameplay. Two examples comes to mind:

* Boulder pushing/grabbing: In Boulder Dash there is a 1 in 8 chance of actually pushing a rock when the player move.
This cause the character to "skid" randomly when trying to push a rock, which can't be done in puzzle form,
not without some burdensome graphical cues and rules.

* Amoeba grow randomly. While I didn't implement them in the puzzle yet, they will likely be completely different
than in the base game once I do because of this property.

Implemented so far:

* Empty space (!)
* Dirt
* Rocks
* Diamonds
* Brick Walls
* Steel Walls
* Explosions
* Butterflies
* Fireflies

Magic walls are next, as they work well in a puzzle. Custom, non-BD related blocks are also planned.

Usage:
-patterns       Room patterns file
-playback       Hash of a cave in the database to display
-playspeed      Playback speed
-seed           Seed that controls the generationof the cave's seeds
-minmove        Minimum moves to accept
-maxmove        Maximum moves to accept
-cpu            Number of Threads to start
-maxtime        Maximum time spent looking for a solution, in seconds
-maxempty       Maximum number of folds spent without the player moving
-idle           Number of folds to skip at the cave's opening

Remarks:

* I do not recommand generating caves larger than 15x15. 
* The outer steel walls are implied during solving. The caves will be created in the whole space specified.
* The -MaxTime switch is in seconds. The maxtime is for the whole process for one cave, and never reset during generation, which can cause some frustration when we get unlucky and the timeout kicks in right after a success message but before finding the final solution...

See GenDashDB.xml for example of the output.
