# Quaker Build 7 - Rules list (post to the Rules section of the Index Cards Padlet)

Source: Quaker Rulesheet - Rewrite (Oct 29). Code: `Assets/Scripts/Rules/RuleBook.cs` (ids and text) and `RuleCheck.cs` (the checks).

## Mechanically checkable rules

| Id | Rule | Rulesheet section | Checked in |
|---|---|---|---|
| R-01 | A player whose tower has collapsed is out: they do not flip, draw or place for the rest of the game. | 4, 7 | RuleCheck |
| R-02 | Each action happens only in its own phase: flip in Event, draw in Draw, place in Build, shake in Shake. | 4 | RuleCheck |
| R-03 | Only the first player flips the event card, and only once per round. | 4 | RuleCheck |
| R-04 | Each standing player draws exactly 1 block card per round. | 4, 5 | RuleCheck |
| R-05 | If the block deck has fewer cards than standing players, nobody draws or places this round. | 7 | RuleCheck |
| R-06 | The Build phase goes clockwise from the first player; you place only on your turn. | 4 | RuleCheck |
| R-07 | You may place only the block style shown on your block card. | 5 | RuleCheck |
| R-08 | Each standing player places exactly 1 block per round. | 4, 5 | RuleCheck |
| R-09 | You may place only on your own tower base. | 5 | RuleCheck |
| R-10 | A block goes on one of the 16 spaces of the tower base. | 5 | RuleCheck |
| R-11 | Event card 11: no block on a Center Hub space this round. | 5 | RuleCheck |
| R-12 | Event card 7: your block must go on a grid-edge space. | 5 | RuleCheck |
| R-13 | Event card 4: everyone skips placement. Event card 18: no draws, placements or shake. | 5, 7 | RuleCheck |
| R-14 | The shake starts only after every standing player has placed or skipped. | 4 | RuleCheck |
| R-15 | The first player runs the shake; if any player is out, an out player runs it. | 4 | RuleCheck |
| R-16 | A game ends after a shake that leaves 1 or 0 towers standing; the last tower standing takes 1 point marker. | 1, 4 | TurnStateMachine |
| R-17 | If every remaining tower collapses in the same shake, most blocks before the shake wins; equal towers each take 1 point marker. | 1, 7 | TurnStateMachine |
| R-18 | The match ends when a player holds 3 point markers; players reaching 3 together share the win. | 1 | TurnStateMachine |
| R-19 | If the block deck ran short, the game ends after the shake; the standing tower with most blocks wins. | 7 | TurnStateMachine |
| R-20 | After each round the first-player role passes clockwise to the next standing player. | 4 | TurnStateMachine |

## Left out, and why (depends on something physical or not yet decided at the table)

- Collapse itself (sections 4, 8): towers collapse on the real shake board; the build has no physical shake system, so it does not decide who collapsed.
- Fallen blocks set aside (section 7): a physical event the build cannot see.
- Never touch another player's tower, and putting knocked blocks back (sections 5, 7): physical, and the put-back rule is still [confirm] at the table.
- Any angle; final once you let go (section 5): physical hand actions.
- Negotiate the tallest tower (section 5, cards 6 and 17): decided by eye; the fallback is still [confirm].
- Event cards 3, 12, 14 and the shake modifiers (5, 10, 15, 16, 17, 19): blind placement, wind, a ten-count and motor speed are physical.

## Stranger test ambiguities, fixed in both media

| # | What the stranger test found | Wording the team settled on (rulesheet) | Check in code | Commit |
|---|---|---|---|---|
| 1 | "What if block towers fail? Does the round result in a tie?" The tiebreaker was only in section 7 and "falls" was undefined. | Section 1 states the tiebreaker: if every remaining tower collapses in the same shake, most blocks before that shake wins; equal towers each take 1 point marker. "Collapsed" = every block has fallen off its tower base. | R-17 in TurnStateMachine.DoEndCheck (shared points on a tie) | tag build-7 |
| 2 | "When and where in the game does your tower first shake?" The draft never said when the first shake happens or who runs it. | Section 4: the shake happens every round from round 1, after every standing player has placed; the first player (or an out player) runs the motor for 5 seconds. | R-14 and R-15 in RuleCheck (StartShake is now a checked action) | tag build-7 |

## Playtest situation reproduced

Build-7 scene, steps 10-11: one tower has a block and Player 1 tries to start the shake. The option list does not offer the shake, and the direct attempt is refused with R-14.
