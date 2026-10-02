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
