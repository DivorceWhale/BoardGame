# Quaker: setup procedure (Build 9)

From a closed box to the first turn. Each step is one method in `BoardGame/Assets/Scripts/QuakerPlay.Setup.cs`, named
after the step and run in this order by `StartMatch`. Each step also writes "Setup step N of 8" to the Console, so
the order can be checked while the game runs.

| Step | At the table | In the code |
|------|--------------|-------------|
| 1 | **Choose the scenario.** Standard is the rulesheet as printed, or pick one of the five Build 5 scenarios. | `Step1_ChooseTheScenario`: the choice from the setup screen becomes the rules for the match |
| 2 | **Set the tower base on the shake board** in the middle of the table. In The Sliver, mark spaces 9-16 as closed. | `Step2_SetTheTowerBaseOnTheShakeBoard`: clears the last match, resets the shake board, turns closed spaces grey |
| 3 | **Sort the blocks into the supply** beside the board: 6 styles, 5 of each (Loose Change: 10 of each). | `Step3_SortTheBlocksIntoTheSupply` |
| 4 | **Shuffle the block deck**: all block cards, face down. No cards are dealt; everyone draws in the Draw phase. | `Step4_ShuffleTheBlockDeck` |
| 5 | **Shuffle the event deck**: the 20 event cards, face down. | `Step5_ShuffleTheEventDeck` |
| 6 | **Seat the players, tallest first.** Line up tallest to shortest and sit clockwise in that order. The tallest player is the first player. | `Step6_SeatThePlayersTallestFirst`: the names come from the setup screen in the order entered; the first name is the first player |
| 7 | **Set out the point markers** in a pile. Everyone starts with 0; first to 3 wins the match. | `Step7_SetOutThePointMarkers` |
| 8 | **Opening placement (the first action).** Starting with the first player and going clockwise, each player places 1 block of their choice from the supply on the tower base. In Four Corners, it must cover the player's own space: 6 for the first player, then 1, 4 and 16. | `Step8_FirstPlayerMakesTheOpeningPlacement`: starts the opening placement and hides the setup screen |

After step 8, round 1 starts with the first player flipping the event card (the rulesheet, "Each round").

## Cold start

1. Open `Scenes/Menu.unity` and press Play (or run a build). Press **Play** on the start menu.
2. On the setup screen, pick the number of players, enter the names clockwise from the tallest, and pick a scenario.
3. Press **Start match**. Steps 1-8 run, and the first player is asked for the opening placement.

No scene editing is needed. The scenario list on the setup screen comes from `Prefabs/Scenarios` (QUAKER > Build 9 > Set Up Scenarios).

## The scenarios on one shared tower

The Build 5 scenarios were written when every player had their own tower. With one shared tower (table change 1, Build 8),
each one keeps the part that still means something:

| Scenario | Build 5 post | On the shared tower | What was left out, and why |
|----------|--------------|---------------------|----------------------------|
| Four Corners (cruel) | Asymmetric starts: one player on a centre space, the others on edge spaces | Each player's opening block must cover their own space: 6 (centre) for the first player, then 1, 4 and 16 (rule SC-2) | "Edge towers face a harder shake after a centre tower falls": there is only one tower, so there are no edge towers |
| Fuse Lit (frantic) | A hard cap of 4 rounds, counted out loud | 4 build rounds per game, shown as "Round 2 of 4". After round 4 every round is shake-only, each stronger, until the tower falls (SC-3) | "The tallest tower at the limit wins": there is one tower, so it shakes until it falls instead |
| Loose Change (cozy) | A generous supply, about 60 blocks, nobody runs out | 10 blocks and 10 block cards of each style (60) | Consolation points for reaching height 4: everyone builds the same tower |
| The Sliver (cerebral) | Only a 2 x 4 strip of the grid is legal | Only spaces 1-8 are open; no part of a block may go over spaces 9-16 (SC-1) | Nothing |
| Thin Margins (tense) | Win by being 3 blocks above everyone else's tallest | The tower collapses once a third of its tallest height has come down, not half | "3 above everyone else" needs a tower per player |

These are the team's reading of the old posts for the new tower. Confirm them at the table and change the
`sharedTowerRule`, `blocksPerStyle`, `collapseShare`, `roundLimit`, `blockedSpaceIds` and `firstPlacementSpaces`
fields on the scenario assets if the table plays them differently.
