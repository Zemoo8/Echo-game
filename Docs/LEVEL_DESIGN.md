# ECHO: Level Design

**Every dash is a decision.** A dash costs one energy charge, and every level ends with a door
that only a dash can break, so the player always has to keep a charge in reserve.

Levels are built in code by `EchoLevelFactory` (**ECHO > Setup > Rebuild Level 1 / 2 / 3**,
**Rebuild Boss Arena**, **Rebuild Ending**); this document explains what each section is for.
Stills: `Docs/Screenshots/level1_*.png`, `level2_*.png`, `level3_*.png`, `boss_1.png` and
`ending_1.png`.

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

## Level 3 · Energy Core

Deep navy machinery around the reactor. Start with **0 charges** (max 3). The level adds two ideas:
cracked plates that don't let the player stand still, and **core locks** that only open while
the player *holds* enough charge, so a dash spent on the way can lock the way ahead.

| # | x | Section | Teaches | Hint |
|---|---|---------|---------|------|
| 1 | −3 → 18 | Entry hall, a cell | | |
| 2 | 18 → 39 | Three cracked plates over the abyss (3 wide, 3-unit gaps); a cell above the middle one | Plates give way 0.5 s after you land: keep moving | "Cracked plates give way a moment after you land. Keep moving." |
| 3 | 42 | **Checkpoint 1** | | |
| 4 | 46 → 64 | Cell, a drone, cell, then a core lock that needs **2** | Jump the drone: dashing through it spends a charge the lock needs | "A core lock opens only while you hold enough charge…" |
| 5 | 70 → 78 | 8-unit pit | Jump + air dash | |
| 6 | 81 | **Checkpoint 2** | | |
| 7 | 90, 95 | Two lasers (1.4 s on, 1.2 s off, out of step); a cell | | |
| 8 | 112 → 140 | A rising bridge of four plates (heights 0.8, 1.6, 1.6, 0.8) under the reactor | Plates and height together | |
| 9 | 143, 146 | **Checkpoint 3**, a cell | | |
| 10 | 150 → 158 | 8-unit pit | Spend a charge… | |
| 11 | 161 → 175 | Three cells (one on a ledge), then a core lock that needs a **full meter (3)** | …then fill the meter again before the lock | |
| 12 | 179 → 187 | 8-unit pit | | "The exit only opens to a dash. Keep one charge for it." |
| 13 | 190 → 202 | Cover, a turret, the exit door | As in Level 2 | "No charge left…" if empty |

**Never stuck.** Each lock can be filled from the checkpoint before it: two cells wait between
checkpoint 1 and the first lock, and between checkpoint 3 and the second lock there is a cell
before the pit (so the pit can always be crossed) and three after it. A player who wastes charges
can press **R** and try the room again with its cells back.

**Energy budget.** The route needs 3 dashes (pits at 70, 150 and 179) plus the exit, and the locks
need 2 and 3 held. Arriving at the second lock with a full meter leaves exactly one spare charge
after the last pit and the exit.

## Finale · The Guardian

One fixed screen inside the reactor chamber (x −10 → 10). The Guardian is the reactor's last
defence: an armoured orb that hovers in front of the reactor. Start with **1 charge** (max 3).

**The loop.** The Guardian attacks with its shell closed (its seam and the reactor glow red), then
sinks to platform height and opens its shell for a short window (core cyan). A dash into it while
open is a hit. A dash into the closed shell bounces off, and that charge is gone: the fight's
decision is when to spend. Touching the Guardian without dashing kills, as with every machine.
Its shots, waves and beams can't touch a dashing player, so a dash is also an escape.

| Phase | Hits | Attacks | Shell open for |
|-------|------|---------|----------------|
| 1 | 1–2 | A volley of 4 bolts from the wall ports on one side, alternating low (floor height) and high (platform height); or a wave along the floor | 2.6 s |
| 2 | 3–4 | The same, plus 3 beams from the ceiling, each aimed where the player stands (a flickering line for 0.75 s first) | 2.2 s |
| 3 | 5–6 | A wave from each wall with a volley from the other side; or bolts from both sides with 3 beams | 1.8 s |

Each port glows red 0.35 s before it fires. Between phases the Guardian pauses and the reactor
flashes.

**The arena.** Two steel platforms at height 2.6 (x −7 → −4 and 4 → 7) put the player level with
the open shell, 3 units away: a dash from a platform's inner edge reaches it. Waves run along the
floor, so the platforms are safe from them, but high bolts cross the platforms. Three cells (one
on each platform, one on the floor in the middle) **come back 4 s after they are taken**: a fight
that lasts can't run on a fixed supply.

**Dying** restarts the current phase: hits from earlier phases stay. The player comes back at the
entrance with 1 charge.

**The way out.** When the sixth hit lands, the Guardian bursts apart, the reactor calms to cyan,
and the blast door at the right rolls up. Behind it is the exit, which, like every exit, only
opens to a dash.

## The ending

The reactor chamber, calm. Captions say the facility is quiet and that the reactor's power has
sent out a distress beacon; a transmission answers: a rescue ship is coming. Then **THE END**, and
a press returns to the menu. Finishing the story clears Continue, so the menu offers a new game.
Jump, dash, Enter or a tap skips each line.

## Machines, consoles and core devices

| Thing | Behaviour | Read as |
|-------|-----------|---------|
| Drone | Glides between two points on a fixed rhythm; touching it kills | Red-eyed machine, red glow |
| Turret | Fires a bolt straight ahead on a fixed rhythm; touching it or a bolt kills; bolts stop at walls and cover | Yellow body, red bolts |
| **Dash vs machines** | A dash destroys a drone or a turret (and passes through bolts), costing the charge it used | The core trade: energy for safety |
| Console | Walk into it: its screen turns from red to green and it opens a shutter or switches lasers off | Red screen = locked, green = done |
| Security shutter | Blocks the way and stops bolts until its console is used | Steel slats with a red lock light |
| Cracked plate | Holds until stood on; flashes and shakes, gives way 0.5 s later, re-forms 2.5 s after (never inside the player) | Navy plate with ember cracks that flare orange |
| Core lock | Solid while the player holds fewer charges than its lights; reads the meter live, so spending a charge closes it again, but never on a player inside it. A dash doesn't break it | Steel-blue field (not cyan: it isn't a barrier) with a panel of charge lights; the missing ones blink when you walk up to it |

Checkpoints treat machines and consoles like cells and barriers: anything destroyed or used
after the last checkpoint comes back when the player respawns. Broken plates always come back.

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

`Assets/Tests/PlayMode/Level3Tests.cs`: the level has its pieces; a plate cracks under the player,
gives way and re-forms; falling through one kills and the respawn restores it; a core lock opens
only with enough charge, stays open while the player is inside, and closes again behind them; and
the bot **plays Level 3 to the exit** without being taught about plates or locks (about 26 s; it
jumps every gap, so the plates never catch it, and it never wastes a charge, so the locks open).

`Assets/Tests/PlayMode/BossTests.cs`: the arena has its pieces; the closed shell deflects a dash
and the open shell takes one hit per opening; a death restarts only the current phase; six hits
defeat the Guardian, open the blast door, and the exit dash completes the fight; a **bot
(`BossBot.cs`) wins the fight** with run, jump and dash only (about 50 s); and the ending plays
through to THE END and clears Continue.

The boss bot found two problems. It kept dying when it jumped to climb back onto a platform while
high bolts crossed there (its own mistake, now fixed), and that showed how crowded phase 3 was:
five bolts from both sides plus four beams at once. Phase 3 now uses four bolts and three beams,
which keeps it the hardest phase on a phone screen without being a wall.

`Assets/Tests/EditMode/PresentationConfigTests.cs` checks that each presentation phase leads only
into the levels it presents: in Phase 4 the end of Level 2 goes back to the menu, in Phase 5 it
leads into Level 3.
