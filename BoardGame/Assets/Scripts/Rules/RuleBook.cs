using System.Collections.Generic;
using System.Text;

// Every rule from the rewritten rulesheet that a program can mark as satisfied or violated.
// The ids match the Rules section on the Index Cards Padlet. Refusals always name one of these ids.
public static class RuleBook
{
    public struct Rule
    {
        public string id;
        public string section;   // rulesheet section the wording comes from
        public string text;
    }

    public static readonly List<Rule> All = new List<Rule>
    {
        new Rule { id = "R-01", section = "4, 7", text = "A player whose tower has collapsed is out: they do not flip, draw or place for the rest of the game." },
        new Rule { id = "R-02", section = "4",    text = "Each action happens only in its own phase: flip the event card in the Event phase, draw in the Draw phase, place in the Build phase, shake in the Shake phase." },
        new Rule { id = "R-03", section = "4",    text = "Only the first player flips the event card, and only once per round." },
        new Rule { id = "R-04", section = "4, 5", text = "Each standing player draws exactly 1 block card per round." },
        new Rule { id = "R-05", section = "7",    text = "If the block deck has fewer cards than there are standing players, nobody draws or places this round." },
        new Rule { id = "R-06", section = "4",    text = "The Build phase goes clockwise from the first player; you place only on your turn." },
        new Rule { id = "R-07", section = "5",    text = "You may place only the block style shown on your block card for this round." },
        new Rule { id = "R-08", section = "4, 5", text = "Each standing player places exactly 1 block per round." },
        new Rule { id = "R-09", section = "5",    text = "You may place only on your own tower base." },
        new Rule { id = "R-10", section = "5",    text = "A block goes on one of the 16 spaces of the tower base (directly or on top of blocks there)." },
        new Rule { id = "R-11", section = "5",    text = "Event card 11 (Crane Swings Overhead): no block may be placed on a Center Hub space this round." },
        new Rule { id = "R-12", section = "5",    text = "Event card 7 (Scaffolding Creaks): your block must go on a grid-edge space (Corner Zone or Edge Zone)." },
        new Rule { id = "R-13", section = "5, 7", text = "Event card 4 (Coffee Break): everyone skips their placement this round. Event card 18 (Gas Line Scare): no draws, placements or shake this round." },
        new Rule { id = "R-14", section = "4",    text = "The shake starts only after every standing player has placed or skipped." },
        new Rule { id = "R-15", section = "4",    text = "The first player runs the shake; if any player is out, an out player runs it instead." },
        new Rule { id = "R-16", section = "1, 4", text = "A game ends after a shake that leaves 1 or 0 towers standing; the last tower standing takes 1 point marker." },
        new Rule { id = "R-17", section = "1, 7", text = "If every remaining tower collapses in the same shake, the tower with the most blocks before that shake wins; equal towers each take 1 point marker." },
        new Rule { id = "R-18", section = "1",    text = "The match ends when a player holds 3 point markers; players who reach 3 together share the win." },
        new Rule { id = "R-19", section = "7",    text = "If the block deck ran short this round, the game ends after the shake and the standing tower with the most blocks wins; equal towers each take 1 point marker." },
        new Rule { id = "R-20", section = "4",    text = "After each round the first-player role passes clockwise to the next player whose tower is standing." },
    };

    // Rules from the rulesheet the build does not check, and why (the assignment asks us to say so).
    public static readonly string[] NotChecked =
    {
        "Collapse itself (sections 4, 8): a tower collapses on the real shake board; the build records it when the table marks it in the Check phase.",
        "Fallen blocks set aside (section 7): blocks that fall without a collapse are a physical event the build cannot see.",
        "Never touch another player's tower; knocked blocks put back (sections 5, 7): physical, and the put-back rule is still [confirm].",
        "Turning a block to any angle; placement final once you let go (section 5): physical hand actions.",
        "Negotiate the tallest tower (section 5, cards 6 and 17): the table decides by eye; the fallback is still [confirm].",
        "Event cards 3, 12, 14 and the shake modifiers (cards 5, 10, 15, 16, 17, 19): blind placement, wind, a ten-count and motor speed are physical.",
    };

    // One short line per rule, for the on-screen panel (full wording stays in All and on the rulesheet).
    static readonly string[] Brief =
    {
        "Out players don't flip, draw or place",
        "Each action only in its own phase",
        "Only the first player flips the event, once",
        "Draw exactly 1 block card per round",
        "Deck too small: nobody draws or places",
        "Place clockwise, only on your turn",
        "Place only the style on your card",
        "Exactly 1 placement per round",
        "Place only on your own tower base",
        "Place on one of the 16 spaces",
        "Event 11: no Center Hub this round",
        "Event 7: must use a grid-edge space",
        "Event 4: skip placing. Event 18: full pause",
        "Shake only after everyone has placed",
        "First player shakes (an out player if any)",
        "1 or 0 towers left: game over, +1 point",
        "Tie: most blocks before the shake wins",
        "First to 3 point markers wins the match",
        "Deck ran short: most blocks wins",
        "First player passes clockwise",
    };

    public static string DescribeShort()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<b>RULES CHECKED</b>");
        for (int i = 0; i < All.Count; i++)
            sb.AppendLine("<b>" + All[i].id + "</b>  " + (i < Brief.Length ? Brief[i] : All[i].text));
        sb.AppendLine();
        sb.AppendLine("<b>NOT CHECKED</b> (physical): collapse, fallen blocks,");
        sb.AppendLine("touching towers, angle, tallest by eye,");
        sb.AppendLine("event cards 3, 12, 14 and shake speed.");
        return sb.ToString();
    }

    public static Rule Get(string id)
    {
        foreach (Rule r in All) if (r.id == id) return r;
        return new Rule { id = id, section = "?", text = "(unknown rule)" };
    }

    public static string Describe()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<b>RULES CHECKED BY THE BUILD</b>  (rulesheet section in brackets)");
        foreach (Rule r in All) sb.AppendLine("<b>" + r.id + "</b>  " + r.text + "  <color=#888>[" + r.section + "]</color>");
        sb.AppendLine();
        sb.AppendLine("<b>NOT CHECKED, AND WHY</b>");
        foreach (string n in NotChecked) sb.AppendLine("- " + n);
        return sb.ToString();
    }
}
