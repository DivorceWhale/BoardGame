# Quaker - Rules list (post to the Rules section of the Index Cards Padlet)

Source: Quaker Rulesheet v2 - One Shared Tower (November 2026), as played in the Unity build (`Assets/Scenes/Play.unity`).
Code: `Assets/Scripts/Rules/RuleBook.cs` (ids and text), `RuleCheck.cs` (the checks), `QuakerPlay.cs` (the 3D table: aiming, physics, shake and collapse).
The in-game Help (H) shows the full rulesheet from `Assets/Art/Rules_v2.txt`.

## Mechanically checkable rules

| Id | Rule | Rulesheet section |
|---|---|---|
| R-01 | Opening placement: before round 1 of every game, each player places exactly 1 block of their choice, without a card, from a style still in the supply. | 3 |
| R-02 | Each action happens only in its own phase: opening placement, then flip in Event, draw in Draw, place in Build, shake in Shake. | 4 |
| R-03 | Only the first player flips the event card, and only once per round. | 4 |
| R-04 | Each player draws exactly 1 block card per round. | 4, 5 |
| R-05 | If the block deck has fewer cards than players, nobody draws or places this round. | 7 |
| R-06 | Players place in turn order, only on their own turn. The table lines up tallest to shortest; in the Unity build the players enter their names in that order at setup. | 3, 4 |
| R-07 | In the Build phase you may place only the block style shown on your block card, and only while one of that style is left in the supply. | 5, 7 |
| R-08 | Each player places exactly 1 block per round (event card 17: the last player places a second block). | 4, 5 |
| R-09 | Blocks go on the one shared tower base: on its 16 spaces or on top of blocks there. Every part of the block must be over the 16 spaces; nothing may hang off the base. | 5 |
| R-10 | Event card 6 (Union Rep Visits): every block this round must go on top of another block. | 5 |
| R-11 | Event card 11 (Crane Swings Overhead): no part of a block may go on a Center Hub space this round. | 5 |
| R-12 | Event card 7 (Scaffolding Creaks): your block must touch the grid edge (at least one part over an outer space). | 5 |
| R-13 | Event card 4 (Coffee Break): everyone skips placing. Event card 18 (Gas Line Scare): no draws, placements or shake. | 5, 7 |
| R-14 | The shake starts only after every player has placed or skipped. | 4 |
| R-15 | Only the first player runs the shake. | 4 |
| R-16 | When the tower collapses (see "When the tower has fallen" below), the player who placed the most recent block loses; every other player takes 1 point marker. | 1, 4 |
| R-17 | Event card 12 (Wind Picks Up): no part of a block may go on a Corner Zone space this round. | 5 |
| R-18 | The match ends when a player holds 3 point markers; players who reach 3 together share the win. | 1, 7 |
| R-19 | If the block deck ran short, rounds continue with only Event, Shake and Check until the tower collapses. Each of these shake-only rounds shakes harder than the last. | 7 |
| R-20 | Event card 20 (Inspector's Final Walk-Through): if the tower falls this round, the loser also returns 1 point marker. | 6 |
| R-21 | Event card 1 (Site Inspector): on your turn you may redraw your block card once. Event card 13 (Permit Renewed): each player draws 2 block cards and places one of them. | Event deck |
| R-22 | Event card 9 (Second Crew Arrives): before anyone places, the first player picks a rival, who redraws their block card. | Event deck |
| R-23 | Shake events: cards 5, 10 and 19 shake the tower twice this round; card 15 runs the motor at double speed; card 16 shakes twice before any new placement. | Event deck |
| R-24 | Event card 14 (Foreman Tightens the Schedule): place within a ten-count, or skip your placement. | Event deck |
| R-25 | Event card 3 (Rebar Delivery): every block this round is placed blind. | Event deck |

## When the tower has fallen (collapse)

The tower base has a groove, so the bottom layer of blocks almost never leaves the base. The game therefore does not wait for every block to come off; it ends when the tower **collapses**.

- After every placement and after every shake, once the board is still, each block is compared with where it was before.
- A block has **come down** if it dropped half a block or more, or fell off the tower base.
- The tower **has collapsed** if, in that one placement or shake, at least **half** of its blocks came down (and at least 2 did).
- Fewer than that: the tower is still standing and the game goes on. Blocks that fell off the base are **set aside** beside the table until the game ends; they do not go back on the tower or into the supply.
- A collapse while a player is placing ends the game at once, and that player loses. A collapse in the shake: the player who placed the most recent block loses (counting earlier rounds).

## Placing a block (Unity build)

- Aim with the mouse; the block snaps to the 4x4 grid and the preview is green where the placement is legal, red where it is not.
- **A / D** turn the block 90 degrees left or right; **W / S** tip it 90 degrees up or down. Together they reach every way a block can face.
- **Q / E**: where there is a gap under an overhang (for example under a Z-Block), Q slides the block in underneath and E brings it back up on top.
- Left click drops the block. Once dropped, nobody may move it or any other block.

## The shake (Unity build settings)

| Setting | Value | What it means |
|---|---|---|
| Shake time | 5 s | as in the rulesheet |
| Shake strength | 0.6 x gravity | peak push of the board. Block friction holds about 0.6, so a well-placed low tower stays up; tall, top-heavy or overhanging towers wobble and collapse |
| Speed | 2.5 back-and-forths per second, easing in and out over 0.5 s | |
| Event card 15 | twice as fast (twice as hard) | |
| Shake-only rounds | +0.15 strength each round, up to 1.5 | 0.75, 0.90, 1.05, ... so every game ends |
| Collapse | 50% of the blocks, at least 2 | |

Tuned with 10 simulated games per setting (QUAKER > Play > Run Shake Tests): at 0.3 almost no tower collapsed; at 0.7 small towers collapsed in round 3; at 0.6 no shake knocked a block off in rounds 1-4, and with the shake-only escalation 9 of 10 games ended in a collapse.

## Block styles

5 of each: The Cube (2x2x1), Long Block (4 in a row), L-Block, BL-Block, T-Block and Z-Block (4 cubes: 2 on top, 2 below shifted one space). Every block has the same weight.

## Left out, and why

- Who is tallest (section 3): measured at the table. The Unity build takes the turn order from the order the names are entered.
- Event card 14's ten-count and event card 3's blind placement are timed and hidden on screen in the Unity build, but at the table they rely on the players.
- A collapse on the real shake board is judged by eye; the 50% rule above is how the Unity build decides it.

## Stranger test ambiguities, fixed in both media

| # | What the stranger test found | Wording the team settled on (rulesheet) | Check in code | Commit |
|---|---|---|---|---|
| 1 | "What if block towers fail? Does the round result in a tie?" | Section 1: a game always has exactly one loser. When the shared tower falls, the player who placed the most recent block loses and every other player takes 1 point marker. | R-16 in TurnStateMachine.ResolveTowerFall | (fill in hash) |
| 2 | "When and where in the game does your tower first shake?" | Section 4: the shake happens every round from round 1, after every player has placed; the first player runs the motor for 5 seconds. | R-14 and R-15 in RuleCheck | (fill in hash) |

## Playtest situation reproduced

Build-7 scene, steps 12-13: one block is down and Player 1 tries to start the shake. The option list does not offer it, and the direct attempt is refused with R-14. Step 18 resolves a fallen tower with R-16.

Play scene (3D): a full game with the shake board, the collapse check and the game-result screen; QUAKER > Play > Run Shake Tests plays 10 games with computer players and writes `Logs/QuakerShakeTests.txt`.
