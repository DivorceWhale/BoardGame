# Quaker: parity check (Build 9)

Set up the physical game and the digital game side by side, follow `Build9_SetupProcedure.md` on both, and compare the
starting positions at the moment the first player is about to make the opening placement.

- **Digital** column: from a cold start of `Build-9.unity` on 2026-11-12, 3 players, Standard scenario, checked by script
  for all six setup choices (Standard and the five scenarios).
- **Table** column: fill in with the box open. Mark each row Same or Different; list every Different under "Open items".

## Starting position

| # | Check | Digital | Table | Same? |
|---|-------|---------|-------|-------|
| 1 | Tower base on the shake board, empty | Yes, 16 spaces, no blocks | | |
| 2 | Blocks in the supply | 30: 5 of each of 6 styles (Loose Change 60) | | |
| 3 | Block deck, face down | 30 cards (Loose Change 60) | | |
| 4 | Event deck, face down | 20 cards | | |
| 5 | Cards in hand | None: nobody is dealt a card; block cards are drawn each round | | |
| 6 | Event card face up | None until round 1 | | |
| 7 | Points | 0 for every player | | |
| 8 | Seating and first player | Names in the order entered; the first (tallest) is the first player | | |
| 9 | First action | The first player's opening placement: any block from the supply | | |
| 10 | Closed spaces (The Sliver) | Spaces 9-16 grey; a block over them is refused (SC-1) | | |
| 11 | Opening spaces (Four Corners) | Gold space for the player placing; first player 6, then 1, 4, 16; any other spot is refused (SC-2) | | |
| 12 | Rounds announced (Fuse Lit) | Header shows "Round 1 of 4"; round 5 is shake-only (SC-3) | | |

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
| | | |
