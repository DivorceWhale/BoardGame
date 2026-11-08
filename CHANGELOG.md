# Quaker Changelog

Balance changes made at the table, mirrored in the Unity build. Shared balance numbers live on
`BoardGame/Assets/Resources/QuakerBalance.asset` (Build 8). Change them in the Inspector, not in code,
and only after the table has changed them. This file should agree with the Index Cards Padlet changelog.

## Build 8: balance surgery and playtest 2 (week of 2026-11-02)

### Playtest 2: what the testers said

- The game was about shaking: place blocks, then shake.
- They knew how to win from turn 0.
- The fun part was shaking the board.
- The boring part was not having enough blocks to continue: turns where you just keep drawing cards because there aren't enough blocks.
- They still felt they could win at the halfway point.
- They had to look up the tiebreaker rules.
- The cards were the hardest part to handle.
- They would have decided the player order differently.
- They would cut the boards down from 4 to 1.
- The game should have more blocks (the prototype only had 6, one per style); they would play again once there are more blocks.

### Table change 1: one shared tower base instead of four

- **Date:** 2026-11-03
- **Before:** 4 tower bases, one per player; each player built their own tower and the last tower standing won.
- **After:** 1 tower base. Every player adds blocks to one shared tower. When it falls, the player who placed the most recent block loses and every other player scores.
- **Reason from the table:** the shaking was the fun part, and the testers said they would cut the boards from 4 to 1.
- **In the build:** one tower base (`GameState.ownBases = false`), and the loser and scoring rule in `TurnStateMachine.ResolveTowerFall` (R-16). This was a change of structure, not a number, so it needed code; the points it uses (`pointsPerGameWin`, `pointsToWinMatch`) are on the balance asset.

### Table change 2: turn order by height (physical game only)

- **Date:** 2026-11-03
- **Before:** a random first player, then clockwise.
- **After:** players go from tallest to shortest; the tallest player is the first player in round 1.
- **Reason from the table:** the testers said they would have decided the player order differently.
- **In the build:** not applied. Turn order by height is a rule for the physical game only, since Unity
  can't tell who is tallest. The build plays in the order the names are entered (`GameState.turnOrder`).

### Table change 3: opening placement before every game

- **Date:** 2026-11-03
- **Before:** the tower started empty every game.
- **After:** before round 1 of every game, each player places 1 block of their choice from the supply, without a card.
- **Reason from the table:** to give each game's tower a bit of randomness from the start.
- **What playtest 2 showed:** not tested yet; added after playtest 2.
- **In the build:** the Opening placement phase (`TurnPhase.Opening`, rule R-01).

### Table change 4: the first player passes clockwise every round

- **Date:** 2026-11-07
- **Value:** `firstPlayerSeatsPerRound`, **before 0, after 1**
- **Before:** the first player stayed the same all match.
- **After:** after every round, once the tower has shaken, the first player passes one seat clockwise. That player flips the next event card, places first and runs the shake.
- **Reason:** first tried in the Unity build, then agreed by the team; it shares out flipping the event card and running the shake.
- **What playtest 2 showed:** not tested yet; to check at the next playtest.
- **In the build:** `TurnStateMachine.PassFirstPlayer` reads the value. 0 brings back the old rule.

### Table change 5: collapse means half of the tower has come down

- **Date:** 2026-11-07
- **Value:** `collapseShare`, **before 1.0, after 0.5**
- **Before:** the tower only counted as fallen when every block had come off the tower base.
- **After:** the tower has collapsed once half of the tallest it got this game has come down.
- **Reason:** first tried in the Unity build, then agreed by the team. The tower base has a groove, so the blocks almost never all leave it, and games ran into endless shake-only rounds.
- **What playtest 2 showed:** not tested yet; to check at the next playtest.
- **In the build:** `QuakerPlay.IsCollapse` and `QuakerPlay.Collapse.cs` read the value.

### Where the table and the code disagreed

The table treats "the tower collapsed" as one judgement call. The code needs it as **two numbers**: the share
of the tower that came down (`collapseShare` = 0.5) and a minimum number of blocks (`collapseMinBlocks` = 2),
so one block tipping off a two-block tower isn't a collapse. The minimum is on the asset for the table to
confirm at the next playtest.

### Not decided at the table yet (raise at the next playtest)

- `collapseMinBlocks` = **2** (see above).
- `shakeStrength` = **0.6.** Tuned in the build; the physical shake board needs a setting the table agrees on.

### Also for the table, from playtest 2

- More blocks: the physical prototype has only 6 blocks, one of each style, so playtest 2 ran out of blocks
  almost at once. The rulesheet and the build both use 30 blocks, 5 of each style (`copies` on each block
  card asset). The physical set needs the other 24 blocks before the next playtest, so the table and the
  build match.
- The tiebreaker: testers had to look it up, so it needs to be easier to find on the rulesheet and player aid.
