# Quaker Changelog

Balance changes made at the table, mirrored in the Unity build. Every value named here lives on
`BoardGame/Assets/Resources/QuakerBalance.asset` (Build 8). Change it in the Inspector, not in code,
and only after the table has changed it. This file should agree with the Index Cards Padlet changelog.

## Build 8: balance surgery and playtest 2 (week of 2026-11-02)

### 1. The first player passes clockwise every round

- **Date:** 2026-11-05 (balance surgery)
- **Value:** `firstPlayerSeatsPerRound`, **before 0, after 1**
- **Before:** the tallest player went first and stayed first player for the whole match.
- **After:** at the end of every round, once the tower has shaken, the first player passes one seat
  clockwise. That player flips the next event card, places first and runs the shake. A game that ends
  mid-round also passes it on. A tower that falls during the opening placement, before round 1, doesn't.
- **Reason from the table:** _TODO: copy the reason from the Padlet changelog._
- **What playtest 2 showed:** _TODO: copy the playtest 2 result from the Padlet changelog._
- **In the build:** `TurnStateMachine.PassFirstPlayer` reads the value. 0 brings back the old rule.

### 2. Collapse means half of the tallest tower has come down

- **Date:** 2026-11-05 (balance surgery)
- **Value:** `collapseShare`, **before 1.0, after 0.5** (plus `collapseMinBlocks` = 2, see below)
- **Before:** the tower only counted as fallen when every block had come off the base.
- **After:** the tower has collapsed once half of the tallest the tower got this game has come down,
  counted over the whole game. A block has come down if it fell off the base or dropped half a block
  or more below where it settled.
- **Reason from the table:** the tower base has a groove, so the blocks on the base almost never leave
  it. "Every block off the base" basically never happened in real life, so games couldn't end on a
  collapse. They ran into endless shake-only rounds instead.
- **What playtest 2 showed:** _TODO: copy the playtest 2 result from the Padlet changelog._
- **In the build:** `QuakerPlay.IsCollapse` and `QuakerPlay.Collapse.cs` read the value.

### Where the table and the code disagreed

The table treats "the tower collapsed" as one judgement call. The code needs it as **two numbers**:
the share of the tower that came down (`collapseShare` = 0.5), and a minimum number of blocks
(`collapseMinBlocks` = 2), so one block tipping over on a two-block tower isn't a collapse. The table
hasn't set the minimum yet. It's on the asset so the table can confirm or change it at the next playtest.

### Not decided at the table (raise at the next playtest)

- `shakeStrength` = 0.6 (the dial setting). It was tuned in the build with the shake tests so a single
  block stays put and tall towers wobble. The physical base has a dial, so the table should agree on a
  setting and the asset should match it.
