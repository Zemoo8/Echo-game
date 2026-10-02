# ECHO: Level Design

**Every dash is a decision.** A dash costs one energy charge, and every level ends with a door
that only a dash can break, so the player always has to keep a charge in reserve.

Levels are built in code by `EchoLevelFactory` (**ECHO > Setup > Rebuild Level 1**); this document
explains what each section is for. Stills: `Docs/Screenshots/level1_*.png`.

## Movement metrics

All spacing comes from the player tuning (`Assets/Settings/Gameplay/PlayerTuning.asset`) and was
proved in the movement sandbox before any level used it.

| Move | Numbers | Level rule |
|------|---------|------------|
| Run | 8.5 units/s | |
| Jump | apex 3.2 units (held); tap for a short hop | Steps up to 1.2 units high; a running jump clears a **4-unit gap** comfortably |
| Dash | 4.5 units in 0.15 s, no gravity, 1 per jump | Breaks cyan barriers and exit doors |
| Jump + air dash | about 9.5 units | Clears an **8-unit gap**, and nothing wider |
| Forgiveness | coyote time 0.1 s, jump buffer 0.12 s, dash buffer 0.1 s | |

The player is 0.7 × 1.3 units. Lasers and doorways are 5 units tall, higher than any jump
(the head peaks at about 4.6), so they can't be jumped over.

## Level 1 · Awakening (abandoned lab)

The hero wakes in a cryo pod with **no energy** (max 3). The level teaches one idea at a time, each
with a hint that matches the player's controls (keyboard, gamepad or touch).

| # | x | Section | Teaches | Hint |
|---|---|---------|---------|------|
| 1 | −3 → 22 | Wake room, cryo pods | Moving | "A / D or the arrow keys to move" |
| 2 | 22 → 36 | Stairs up (1, 2.2, 3); the last stair is 6 deep so a full jump from below can't overshoot | Jumping, holding for height | "SPACE to jump. Hold it to jump higher." |
| 3 | 36 → 40 | 4-unit pit at height 3 | A running jump over danger (red stripes, red glow) | |
| 4 | 48 → 60 | Energy cell, then a cyan barrier in a doorway | Energy, then the dash | "Each energy cell holds one charge…", "SHIFT to dash…" |
| 5 | 67 | **Checkpoint 1** | | "Checkpoint reached…" note |
| 6 | 72 → 84 | Cell, then an 8-unit pit | Jump, then dash in mid-air | "Jump, then SHIFT in mid-air…" |
| 7 | 88 → 92 | Two cells | Building a reserve | |
| 8 | 97 | **Checkpoint 2** | | |
| 9 | 104, 111 | Two timed lasers (1.6 s on, 1.4 s off; the second runs 1 s out of step) | Reading a rhythm: wait, then go | "Red means danger. Wait for a laser to switch off, then go." |
| 10 | 118 | **Checkpoint 3** | | |
| 11 | 122 → 136 | Cell, then the final 8-unit pit | Spending a charge to cross | "The exit only opens to a dash. Keep one charge for it." |
| 12 | 136 → 162 | The exit door | The rule of the game | If the meter is empty: "No charge left. Press R…" |

**Energy budget.** The route needs 4 dashes (barrier, two wide pits, the exit door) and offers 5
cells, so a player who takes the cells reaches the exit with a charge to spare. Level 1 is
forgiving on purpose; later levels tighten the budget so skipping a cell or wasting a dash matters.

**Getting stuck.** Arriving at the exit with no energy can't be fixed by playing on (the pit behind
needs a dash), so the hint tells the player to press **R** (keyboard) or **Select** (gamepad) to go
back to the last checkpoint, or to use **Restart** from the pause menu on touch.

## Level 2 · Security Wing

Cold steel halls guarded by machines. Start with **1 charge** (max 3). The route needs 3 dashes
(two 8-unit pits and the exit door) and offers 3 cells, so there is exactly **one spare charge**:
spend it destroying a machine, or keep it as a safety margin.

| # | x | Section | Teaches | Hint |
|---|---|---------|---------|------|
| 1 | −3 → 24 | A drone patrolling 14 → 24 at hip height | Jump it, or dash through it (−1 charge) | "Drones patrol. Jump over one, or dash through it…" |
| 2 | 27, 29 | Cell, **checkpoint 1** | | |
| 3 | 31 → 38 | A low cover wall, then a turret firing left every 2 s | Hide behind cover, jump the bolts and the turret, or dash into it | "Turrets fire on a rhythm…" |
| 4 | 46 → 54 | 8-unit pit (8 units after the turret, so jumping the turret never lands in the pit) | Jump + air dash | |
| 5 | 57 → 72 | Cell; a console on a ledge; the shutter it opens | Consoles: walk into one | "Walk into a console to use it. Green means it worked." |
| 6 | 76 → 86 | A faster drone | | |
| 7 | 88.5 | **Checkpoint 2** | | |
| 8 | 90 → 110 | A console on a steel platform switches off the three lasers ahead (1.2 s on, 1 s off, staggered) | A choice: climb to the console, or time three lasers | "Red means danger…" |
| 9 | 114, 117 | **Checkpoint 3**, cell | | "The exit only opens to a dash…" |
| 10 | 120 → 128 | 8-unit pit | | |
| 11 | 130 → 142 | Cover wall, a turret guarding the exit door | Cover again, then jump the turret straight into the exit dash | "No charge left…" if empty |

Two layout fixes came from the test bot, and both would have hurt players too: the first turret
originally stood 2 units before a pit (jumping it landed in the pit), and the exit turret's bolts
flew across the second pit, punishing anyone lining up the jump. The turret moved back, and a
cover wall now stops the bolts before the pit.

## Machines and consoles

| Thing | Behaviour | Read as |
|-------|-----------|---------|
| Drone | Glides between two points on a fixed rhythm; touching it kills | Red-eyed machine, red glow |
| Turret | Fires a bolt straight ahead on a fixed rhythm; touching it or a bolt kills; bolts stop at walls and cover | Yellow body, red bolts |
| **Dash vs machines** | A dash destroys a drone or a turret (and passes through bolts), costing the charge it used | The core trade: energy for safety |
| Console | Walk into it: its screen turns from red to green and it opens a shutter or switches lasers off | Red screen = locked, green = done |
| Security shutter | Blocks the way and stops bolts until its console is used | Steel slats with a red lock light |

Checkpoints treat machines and consoles like cells and barriers: anything destroyed or used
after the last checkpoint comes back when the player respawns.

## Checkpoints

A checkpoint saves the run when the player walks past it: the energy at that moment, which cells
were taken and which barriers were broken. After a death (or **R**), the player comes back there
with that energy, cells taken later reappear and barriers broken later close again, so nothing
can be farmed and nothing is lost.

## Hazards

| Hazard | Behaviour | Read as |
|--------|-----------|---------|
| Pit | Bottomless; falling kills | Dark shaft, red warning stripes on both lips, red glow from below |
| Laser gate | Kills on touch while on; flickers faintly for 0.4 s before switching on; pausing freezes it | Red beam between two emitters, red light |

## Checked by tests

`Assets/Tests/PlayMode/Level1Tests.cs`: the level has its pieces; checkpoints restore energy and
later cells; lasers kill only while on; the first hint appears; and a **bot plays Level 1 from the
cryo pod to the exit** using only run, jump and dash (about 21 s, no deaths).

`Assets/Tests/PlayMode/Level2Tests.cs`: the level has its pieces; a dash destroys a drone and a
death brings it back; touching a drone kills; a console opens the shutter and a death closes it;
and the same **bot plays Level 2 to the exit** (16–27 s; its crude timing against moving
machines sometimes costs it a death or two, and it always finishes). The bot lives in
`Assets/Tests/PlayMode/LevelBot.cs`.
