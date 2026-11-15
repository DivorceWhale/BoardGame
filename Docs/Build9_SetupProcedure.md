# Quaker: setup procedure (Build 9)

From a closed box to the first turn: "Setting up a game" in the rulebook. Each step is one method in
`BoardGame/Assets/Scripts/QuakerPlay.Setup.cs`, named after the step and run in this order by `StartMatch`. Each step
also writes "Setup step N of 8" to the Console, so the order can be checked while the game runs.

| Step | At the table (rulebook) | In the code |
|------|-------------------------|-------------|
| 0 | Digital only, before setup: **choose the scenario.** Standard is the rulebook; the five Build 5 scenarios are extras. | `Step0_ChooseTheScenario`: the choice from the setup screen becomes the rules for the match. Not a rulebook step, so it is not numbered in the Console |
| 1 | **Tower base.** Put the tower base flat in the middle of the table, grid side up. | `Step1_PutTheTowerBaseInTheMiddle`: clears the last match and resets the base; in The Sliver, spaces 9-16 turn grey |
| 2 | **Supply.** Sort the 30 blocks into 6 piles by style, where every player can reach them. | `Step2_SortTheBlocksIntoTheSupply` (Loose Change: 10 of each) |
| 3 | **Block deck.** Shuffle the 30 block cards and put them face down beside the supply. | `Step3_ShuffleTheBlockDeck`: nobody is dealt a card; everyone draws in the Draw phase |
| 4 | **Event deck.** Shuffle the 20 event cards and put them face down beside the block deck. | `Step4_ShuffleTheEventDeck` |
| 5 | **Scores.** Each player keeps count of their own points, starting at 0. In later games, keep your points. | `Step5_EveryoneStartsAtZeroPoints`: the build keeps the count in the Turn order panel |
| 6 | **Player aid.** Leave the last page of the rulebook open where every player can see it. | `Step6_LeaveThePlayerAidOpen`: the rules are in Help (H) and the prompt at the top says whose step it is |
| 7 | **First player.** Sit around the table. In the first game, the tallest player is the first player. | `Step7_TheTallestPlayerIsTheFirstPlayer`: the names come from the setup screen, entered tallest first; the first name is the first player |
| 8 | **Opening placement (the first action).** Clockwise from the first player, each player picks any 1 block from the supply and places it on the tower base. No block card is needed. In Four Corners, it must cover the player's own space: 6 for the first player, then 1, 4 and 16. | `Step8_OpeningPlacement`: starts the opening placement and hides the setup screen |

After step 8, round 1 starts with the first player flipping the event card (the rulebook, "Playing a round").

## Cold start

1. Open `Scenes/Menu.unity` and press Play (or run a build). Press **Play** on the start menu.
2. On the setup screen, pick the number of players, enter the names from tallest to shortest, and pick a scenario
   (Standard is the rulebook).
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
