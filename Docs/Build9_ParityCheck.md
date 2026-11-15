# Quaker: parity check (Build 9)

Set up the physical game and the digital game side by side, follow `Build9_SetupProcedure.md` on both, and compare the
starting positions at the moment the first player is about to make the opening placement.

- **Digital** column: from a cold start of `Build-9.unity` on 2026-11-12, 3 players, Standard scenario, checked by script
  for all six setup choices (Standard and the five scenarios).
- **Table** column: our box, set up from "Setting up a game" in the rulebook. Lid off to the first legal turn took 3:23
  at the table (Nov 5, Rotation 2) and 4:12 for a new player reading only the rules.

## Starting position

| # | Check | Digital | Table | Same? |
|---|-------|---------|-------|-------|
| 1 | Tower base in the middle, empty | Yes, 16 spaces, no blocks (on the shake board) | Yes, flat in the middle of the table, no blocks | Same |
| 2 | Blocks in the supply | 30: 5 of each of 6 styles (Loose Change 60) | 30: 5 of each of 6 styles, in 6 piles | Same |
| 3 | Block deck, face down | 30 cards (Loose Change 60) | 30 by the rulebook; 12 printed so far | Different |
| 4 | Event deck, face down | 20 cards | 20 cards | Same |
| 5 | Cards in hand | None: nobody is dealt a card; block cards are drawn each round | None | Same |
| 6 | Event card face up | None until round 1 | None until round 1 | Same |
| 7 | Points | 0 for every player, kept by the build | 0; each player keeps count | Same |
| 8 | Player aid | Help (H) and the prompt at the top of the screen | Last page of the rulebook, open for everyone | Same job |
| 9 | First player | The first name entered (the tallest) | The tallest player | Same |
| 10 | First action | Opening placement, clockwise from the first player: any block from the supply, no card | Same | Same |
| 11 | Closed spaces (The Sliver) | Spaces 9-16 grey; a block over them is refused (SC-1) | Not in the rulebook | Digital only |
| 12 | Opening spaces (Four Corners) | Gold space for the player placing; first player 6, then 1, 4, 16; any other spot is refused (SC-2) | Not in the rulebook | Digital only |
| 13 | Rounds announced (Fuse Lit) | Header shows "Round 1 of 4"; round 5 is shake-only (SC-3) | Not in the rulebook | Digital only |

## Where the table judges and the code needs a rule

These are the places the two can disagree. The build follows the rule in the right-hand column.

| At the table | In the build |
|--------------|--------------|
| Players see who is tallest | Players enter their names tallest first; the code can't measure height, so the order typed is the order used |
| Shuffling by hand | A seeded shuffle (`GameRandom`). The same seed deals the same cards, for testing; the seed is never shown to players |
| The first player shakes the base by hand for 5 seconds | The shake board moves the base for 5 seconds at a set strength (`QuakerBalance.asset`), the same every time |
| "Has the tower collapsed?" is a call the players make by eye | Collapse is counted: half of the tallest tower this game has come down (a third in Thin Margins), at least 2 blocks |
| Players keep count of their points | The build keeps the count and shows it in the Turn order panel |
| Placing a block "on the base" with fingers, at any angle | The block is aimed with the mouse and turned with the keys; any part hanging off the base is refused (R-09) |
| A dead card (no space fits) is noticed by the players, who skip | The code checks whether the block fits anywhere, and if not, the player discards and draws again (R-26); see Open items |

## Open items

| Check | Difference | Fixed in the build, or why not |
|-------|------------|--------------------------------|
| 3 Block deck | The box has 12 of the 30 block cards; the rest are being printed | Not a build change: the build follows the rulebook's 30. The box is fixed when printing finishes |
| 11-13 Scenarios | The five Build 5 scenarios are only in the build; the rulebook has none | Kept as digital only. Standard (the default) is the rulebook game |
| Dead card (R-26) | Rulebook: a player whose block has no legal place, or whose style is out of the supply, skips. Build: discards and draws again; skips only when the block deck is empty | Not fixed yet: the build should follow the rulebook |
