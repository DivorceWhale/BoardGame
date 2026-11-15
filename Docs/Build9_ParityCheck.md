# Quaker: parity check (Build 9)

Set up the physical game and the digital game side by side, follow `Build9_SetupProcedure.md` on both, and compare the
starting positions at the moment the first player is about to make the opening placement.

- **Digital** column: from a cold start of `Build-9.unity` on 2026-11-12, 3 players, Standard scenario, checked by script
  for all six setup choices (Standard and the five scenarios).
- **Table** column: from the team's Paper Prototype Complete package (v1.0, Nov 15, 2026): the component manifest,
  counted by two people, and Setup in the rulesheet v1.0. Lid off to the first legal turn took 3:23 at the table (Nov 5,
  Rotation 2) and 4:12 for a new player reading only the rulesheet.

## Starting position

| # | Check | Digital | Table | Same? |
|---|-------|---------|-------|-------|
| 1 | Tower base on the shake board, empty | Yes, 16 spaces, no blocks | Yes: board on the shake platform, one tower base plate on the board, no blocks | Same |
| 2 | Blocks in the supply | 30: 5 of each of 6 styles (Loose Change 60) | 30: 5 of each of 6 styles (Tray 3) | Same |
| 3 | Block deck, face down | 30 cards (Loose Change 60) | 30 by the rulesheet; 12 printed so far | Different |
| 4 | Event deck, face down | 20 cards | 20 cards | Same |
| 5 | Cards in hand | None: nobody is dealt a card; block cards are drawn each round | None dealt (with the crew module, one crew card face up each) | Same |
| 6 | Event card face up | None until round 1 | None until round 1 | Same |
| 7 | Points | 0 for every player | 0 on every point tracker | Same |
| 8 | Seating and first player | Names in the order entered; the first (tallest) is the first player | Tallest to shortest; the tallest is the first player | Same |
| 9 | First action | The first player's opening placement: any block from the supply | Each player places 1 block of their choice, starting with the first player | Same |
| 10 | Closed spaces (The Sliver) | Spaces 9-16 grey; a block over them is refused (SC-1) | Not in the box | Digital only |
| 11 | Opening spaces (Four Corners) | Gold space for the player placing; first player 6, then 1, 4, 16; any other spot is refused (SC-2) | Not in the box | Digital only |
| 12 | Rounds announced (Fuse Lit) | Header shows "Round 1 of 4"; round 5 is shake-only (SC-3) | Not in the box | Digital only |
| 13 | Player aid at each seat | None shown; the prompt at the top of the screen and Help (H) do the job | 1 per player, 1 spare in the box | Different |
| 14 | Crew cards (optional) | Not in the build | Dealt 1 face up each, only if the table agrees | Different |

## Where the table judges and the code needs a rule

These are the places the two can disagree. The build follows the rule in the right-hand column.

| At the table | In the build |
|--------------|--------------|
| Players see who is tallest and line up | Players enter their names in height order; the code can't measure height, so the order typed is the order used |
| Shuffling by hand | A seeded shuffle (`GameRandom`). The same seed deals the same cards, for testing; the seed is never shown to players |
| "Has the tower fallen?" is a call the players make by eye | Collapse is counted: half of the tallest tower this game has come down (a third in Thin Margins), at least 2 blocks |
| Placing a block "on the base" with fingers, at any angle | The block is aimed with the mouse and turned with the keys; any part hanging off the base is refused (R-09) |
| A dead card (no space fits) is noticed by the players | The code checks whether the block fits anywhere, and if not, discards and redraws for the player (R-26) |
| The Build 5 scenarios were written for a tower per player | Each one is read for the shared tower; see "The scenarios on one shared tower" in `Build9_SetupProcedure.md` |

## Open items

List anything that came out Different, and what was done about it.

| Check | Difference | Fixed in the build, or why not |
|-------|------------|--------------------------------|
| 3 Block deck | The box has 12 of the 30 block cards; the rest are being printed | Not a build change: the build follows the rulesheet's 30. The box is fixed when printing finishes |
| 10-12 Scenarios | The five Build 5 scenarios are only in the build; the box has no scenario sheet | Kept as digital only. Standard (the default) is the game in the box |
| 13 Player aid | The box has a player aid per seat; the build doesn't show one, and `Art/PlayerAid.txt` still describes a tower per player | Not fixed yet: update `PlayerAid.txt` to the shared tower or drop it |
| 14 Crew cards | Optional module in the box, not in the build | Not built: optional, and off unless the table agrees before setup |
| Tower base | The box has a board and a separate tower base plate; the build has one Tower Base prefab | No change: the starting position is the same, an empty 16-space base on the shake platform |
