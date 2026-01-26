# gendash
Solver and Generator for Boulder Dash/Supaplex style "caves"

In C#, using Net 8. Coded in Visual Studio 2022.

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

Custom, non-BD related blocks are also planned.

Usage:
|Switch         |Description                                              |
|:--------------|:--------------------------------------------------------|
|-patterns      |Room patterns file                                       |
|-database      |Database file name (default: GenDashDB.xml)              |
|-format        |Output format: xml, binary (default: xml)                |
|-playback      |Hash of a cave in the database to display                |
|-playspeed     |Playback speed                                           |
|-seed          |Seed that controls the generationof the cave's seeds     |
|-minmove       |Minimum moves to accept                                  |
|-maxmove       |Maximum moves to accept                                  |
|-minscore      |Minimum score to accept                                  |
|-tasks         |Number of Threads to start (alias: -cpu)                 |
|-maxtime       |Maximum time spent looking for a solution, in seconds    |
|-idle          |Number of folds to skip at the cave's opening            |

For detailed file format specifications instructions, see [FORMAT_SYSTEM.md](FORMAT_SYSTEM.md).

Remarks:

* I do not recommand generating caves larger than 15x15. 
* The outer steel walls are implied during solving. The caves will be created in the whole space specified, Exits will never be in the outer walls without an explicit pattern in Pattern.xml.
* The -MaxTime switch is in seconds. The maxtime is for one fold of each cave, and reset during generation when a fold is sucessful

See GenDashDB.xml for example of the output. The caves can be played.

EDIT: 6 years later, migrated to .NET 8 and Visual Studio 2022. I had to revise the code too, I got better at C#. The solver was allocating too much of everything before, the GC was crazy.
I plan on using generated maps for another project.
