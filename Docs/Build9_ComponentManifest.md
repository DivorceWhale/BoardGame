# Quaker: component manifest checklist (Build 9)

One row per component in "What's in the box" in the rulebook (the most up-to-date description of the game): its
count, and the Unity asset, prefab or data that stands for it. Paths are under `BoardGame/Assets/`. The game scene is
`Scenes/Build-9.unity`, opened from `Scenes/Menu.unity`.

**Count** is the count in the rulebook, which the build uses. **Box** is what is in our box now. Rows where the two
disagree are listed under "Open items" in `Build9_ParityCheck.md`.

| # | Component | Count | Box | Unity asset / prefab / data | Status |
|---|-----------|-------|-----|------------------------------|--------|
| 1 | Tower base (4 x 4 grid of 16 spaces, a groove around the grid) | 1 | 1 | `Prefabs/Tower Base.prefab` (16 `BoardTile` tiles, ids 1-16); Corner Zone, Edge Zone and Center Hub in `Prefabs/Tower Base.asset` (`BoardLayout`) | Done |
| 2 | Blocks: The Cube | 5 | 5 | `Prefabs/Blocks/The Cube.prefab` | Done |
| 3 | Blocks: Long Block | 5 | 5 | `Prefabs/Blocks/Long Block.prefab` | Done |
| 4 | Blocks: L-Block | 5 | 5 | `Prefabs/Blocks/L-Block.prefab` | Done |
| 5 | Blocks: BL-Block | 5 | 5 | `Prefabs/Blocks/BL-Block.prefab` | Done |
| 6 | Blocks: T-Block | 5 | 5 | `Prefabs/Blocks/T-Block.prefab` | Done |
| 7 | Blocks: Z-Block | 5 | 5 | `Prefabs/Blocks/Z-Block.prefab` | Done |
| 8 | Block cards (one style per card, 5 of each style) | 30 | 12; the other 18 are being printed | `Prefabs/Cards/Blocks/*.asset` (`CardData`, `copies: 5` on each; the supply count comes from the same number) and `Prefabs/Cards/Blocks/* Card.prefab`; art in `Art/BlockArt/` | Done in the build; the box is short until printing finishes |
| 9 | Event cards 1-20 (Light 1-8, Medium 9-16, Heavy 17-20) | 20 | 20 | `Prefabs/Cards/Event/EC1.asset` to `EC20.asset` (`CardData`), shown with `Prefabs/Cards/Event/EventCard.prefab` and `Prefabs/Cards/CardUI.prefab` | Done |
| 10 | Rulebook | 1 | 1 | `Art/Rules_v2.txt`, shown in Help (H) in the game and in How to Play on the start menu; rule ids in `Scripts/Rules/RuleBook.cs` | Done |
| 11 | Player aid (the last page of the rulebook, one shared copy) | 1 | 1 (part of the rulebook) | `Art/PlayerAid.txt` (the Quick reference page). In play, Help (H) and the prompt at the top of the screen do its job | Done |

## In the build but not a component in the box

| Item | Rulebook | In the build |
|------|----------|--------------|
| The shake | No shake platform: the first player shakes the tower base by hand for 5 seconds | "Shake Board" object in `Build-9.unity` under the tower base (kinematic Rigidbody), built by `QuakerPlay` (QUAKER > Play > Rebuild Scene Objects). Length 5 s (`shakeSeconds`), strength and speed in `Resources/QuakerBalance.asset` |
| Points | No score tokens: players keep count themselves | `PlayerState.matchPoints`, shown as "0 / 3 points" in the Turn order panel |
| First player | The tallest player: a physical rule, judged at the table | Not in the build: names are entered clockwise on the setup screen and the first name is the first player (setup step 7) |
| Player colours | None | Colour swatch next to each name on the setup screen (`QuakerPlay.Turns.cs`, `FixPlayerColors`) |
| Table | The table you play on | "Table" object in `Build-9.unity`, `Art/Table.mat`; fallen blocks are set aside on it |
| Scenarios (the five from Build 5) | Not in the rulebook | `Prefabs/Scenarios/*.asset` (`ScenarioConfig`): Four Corners, Fuse Lit, Loose Change, The Sliver, Thin Margins; chosen before setup. Standard is the rulebook |
| Sounds, random seed | None | `Scripts/GameSounds.cs`; `Scripts/GameRandom.cs` (testing only, never shown to players) |
| Box | Packaging only | None: nothing is played with it |
