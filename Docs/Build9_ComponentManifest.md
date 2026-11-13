# Quaker: component manifest checklist (Build 9)

One row per physical component in the finished box: its count, and the Unity asset, prefab or data that stands for it.
Paths are under `BoardGame/Assets/`. The game scene is `Scenes/Build-9.unity`, opened from `Scenes/Menu.unity`.

**Count** is the count the rules and the build use. **Box** is the count in the finished box; fill it in with the box
open, and fix any row where the two disagree (see `Build9_ParityCheck.md`).

| # | Component | Count | Box | Unity asset / prefab / data | Status |
|---|-----------|-------|-----|------------------------------|--------|
| 1 | Tower base (4 x 4 grid of 16 spaces, groove, edge rails) | 1 | | `Prefabs/Tower Base.prefab` (16 `BoardTile` tiles, ids 1-16); space layout and zones (Center Hub, Corner Zone, Edge Zone) in `Prefabs/Tower Base.asset` (`BoardLayout`) | Done |
| 2 | Shake board (the platform the tower base sits on) | 1 | | "Shake Board" object in `Build-9.unity` (kinematic Rigidbody, the tower base is its child), built by `QuakerPlay` (QUAKER > Play > Rebuild Scene Objects). Shake length, strength and speed: `Resources/QuakerBalance.asset` | Done |
| 3 | Blocks: The Cube | 5 | | `Prefabs/Blocks/The Cube.prefab` | Done |
| 4 | Blocks: Long Block | 5 | | `Prefabs/Blocks/Long Block.prefab` | Done |
| 5 | Blocks: L-Block | 5 | | `Prefabs/Blocks/L-Block.prefab` | Done |
| 6 | Blocks: BL-Block | 5 | | `Prefabs/Blocks/BL-Block.prefab` | Done |
| 7 | Blocks: T-Block | 5 | | `Prefabs/Blocks/T-Block.prefab` | Done |
| 8 | Blocks: Z-Block | 5 | | `Prefabs/Blocks/Z-Block.prefab` | Done |
| 9 | Block cards (one style per card, 5 of each style) | 30 | | `Prefabs/Cards/Blocks/*.asset` (`CardData`, `copies: 5` on each; the supply count comes from the same number) and `Prefabs/Cards/Blocks/* Card.prefab`; art in `Art/BlockArt/` | Done |
| 10 | Event cards 1-20 (one of each) | 20 | | `Prefabs/Cards/Event/EC1.asset` to `EC20.asset` (`CardData`), shown with `Prefabs/Cards/Event/EventCard.prefab` and `Prefabs/Cards/CardUI.prefab`; copies per card: `eventCardCopies` on `Resources/QuakerBalance.asset` | Done |
| 11 | Point markers | 1 per point scored | | No model: each player's points are `PlayerState.matchPoints`, shown as "0 / 3 points" in the Turn order panel | Out of scope as a 3D piece: the counter on screen does the same job and can't be miscounted |
| 12 | Rulesheet v2 | 1 | | `Art/Rules_v2.txt`, shown in Help (H) in the game and in How to Play on the start menu; rule ids R-01 to R-26 in `Scripts/Rules/RuleBook.cs` | Done |
| 13 | Scenarios (the five from Build 5) | 5 | | `Prefabs/Scenarios/*.asset` (`ScenarioConfig`): Four Corners, Fuse Lit, Loose Change, The Sliver, Thin Margins; chosen on the setup screen | Done (if the box has a scenario sheet; otherwise the scenarios are digital only) |
| 14 | Player aid | 1 per player | | `Art/PlayerAid.txt` | Out of date: it still describes a tower per player (Rulesheet v1) and is not shown in the game. The prompt at the top of the screen tells each player their step. Update or drop it, to match the box |
| 15 | Box | 1 | | none | Out of scope: packaging only, nothing is played with it |

## Setup that is not a component

| Item | In the build |
|------|--------------|
| First player (the tallest player) | The setup screen: names are entered clockwise from the tallest player; the first name is the first player (setup step 6) |
| Player colours | Colour swatch next to each name on the setup screen (`QuakerPlay.Turns.cs`, `FixPlayerColors`) |
| Table | "Table" object in `Build-9.unity`, `Art/Table.mat`; fallen blocks are set aside on it |

## Not in the box, digital only

Sounds (`Scripts/GameSounds.cs`), the day and night cycle (`Scripts/DayNightCycle.cs`) and the random seed (`Scripts/GameRandom.cs`,
testing only, never shown to players) have no physical counterpart.
