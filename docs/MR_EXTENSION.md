# Mixed-Reality Extension Design – "Code the Park on your table"

AR/MR is not compulsory in CS 732, so we did not build it. This is our **extension design** for the Meta Quest 3.

## Idea
The player sees their **real room** (colour passthrough). A **small carousel (about 40 cm)** and Codey stand on their **real table**. The same Level 2 challenges (variables) appear on a floating code screen next to the table. Every correct line changes the small carousel: its lights turn on, the name sign appears, and `speed = 3;` makes it turn. This is good for a classroom: the student sits at a desk, there is no walking, and the teacher can see the student's room.

## How it would work (Quest 3, Unity)
| Step | Technology |
|---|---|
| See the real room | OpenXR + **Unity OpenXR: Meta** passthrough (camera background transparent, AR Camera Manager) – AR Foundation |
| Find the table | **Plane detection** (AR Plane Manager), horizontal planes; or the room setup from the Quest (Scene / MR Utility Kit) with the "Table" label |
| Place the carousel | ray + Trigger on the table plane → **AR Anchor**, so it stays in place when the player moves |
| Size | carousel scaled to 0.04 of the park model; Codey 0.25 m |
| Code screen | the same `CodeStation` prefab, 0.6 m wide, 40 cm above the table, facing the player |
| Code blocks | the same grabbable blocks and socket; they lie on the real table |
| Feedback | the same green / red screen, sounds and Codey; the small carousel lights and turns |
| AI | the same Codey AI buttons and backend |

## What we reuse
`CodeStation.cs`, `CodeBlock.cs`, `ChallengeData.cs` (Level 2), `RideController.cs` (carousel effects), `RobotTeacher.cs`, `CodeyAI.cs`. New: one script that places the scene on the chosen table and an MR scene with passthrough.

## Comfort and safety
- The player sits; no locomotion, so less motion sickness.
- The real room is always visible, so the player does not hit furniture.
- Large text even at small scale (the screen stays 0.6 m wide).

## Tests we would run
1. The carousel stays on the table when the player walks around it (anchor test).
2. Text is readable at 1 m.
3. Plane detection works on light and dark tables.
4. FPS stays at 72 with passthrough on.

## Limitations
Quest 3 only (colour passthrough). Room setup must be done once in the headset.
