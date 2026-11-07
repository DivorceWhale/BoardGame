using System.Collections.Generic;
using UnityEngine;

// When has the tower fallen? It has collapsed once half of the tallest tower of this game has come down
// (and at least Collapse Min Blocks): counted over the whole game, not per shake, so a tower that wears down a
// block or two at a time ends too, not only one that topples in one go.
//   Tallest tower: the most blocks the tower has had at once this game.
//   Come down: set aside (fell off the base), or dropped half a block or more below where it first settled,
//              even if it is still lying on the base in the groove.
public partial class QuakerPlay
{
    readonly Dictionary<Rigidbody, float> restBottom = new Dictionary<Rigidbody, float>();   // where each block first settled
    int towerPeak;          // the most blocks on the tower at once this game
    int setAsideThisGame;   // blocks that fell off the base this game (counted even if the demo removes them)

    // New game: forget the old tower.
    void ResetTowerRecord()
    {
        restBottom.Clear();
        towerPeak = 0;
        setAsideThisGame = 0;
    }

    // After every settle: remember where new blocks came to rest, and how tall the tower got.
    // blocksBefore: blocks on the tower before anything was set aside this time (the new block included).
    void RecordTower(int blocksBefore)
    {
        towerPeak = Mathf.Max(towerPeak, blocksBefore);
        foreach (Rigidbody rb in placed)
            if (rb != null && !restBottom.ContainsKey(rb)) restBottom[rb] = RenderBounds(rb.gameObject).min.y;
    }

    // Blocks that have come down this game.
    int BlocksDown()
    {
        int n = setAsideThisGame;
        foreach (Rigidbody rb in placed)
        {
            float rest;
            if (rb != null && restBottom.TryGetValue(rb, out rest) && RenderBounds(rb.gameObject).min.y < rest - cell * 0.5f) n++;
        }
        return n;
    }

    bool TowerCollapsed(out int down)
    {
        down = BlocksDown();
        return IsCollapse(down, towerPeak);
    }
}
