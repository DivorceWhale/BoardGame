using System.Collections.Generic;
using System.Text;

// Every rule from Quaker Rulesheet v2 (one shared tower) that a program can mark as satisfied or violated.
// The ids match the Rules section on the Index Cards Padlet. Refusals always name one of these ids.
public static class RuleBook
{
    public struct Rule
    {
        public string id;
        public string section;   // rulesheet section the wording comes from
        public string text;
        public string brief;     // one short line for the on-screen panel
    }

    public static readonly List<Rule> All = new List<Rule>
    {
        R("R-01", "3",    "Opening placement: before round 1 of every game, each player places exactly 1 block of their choice, without a card, from a style still in the supply.", "Opening: 1 block of your choice each"),
        R("R-02", "4",    "Each action happens only in its own phase: opening placement, then flip in Event, draw in Draw, place in Build, shake in Shake.", "Each action only in its own phase"),
        R("R-03", "4",    "Only the first player flips the event card, and only once per round.", "Only the first player flips the event"),
        R("R-04", "4, 5", "Each player draws exactly 1 block card per round.", "Draw exactly 1 block card per round"),
        R("R-05", "7",    "If the block deck has fewer cards than players, nobody draws or places this round.", "Deck too small: nobody draws or places"),
        R("R-06", "3, 4", "Players place in turn order, clockwise from the first player, only on their own turn. The tallest player is the first player in round 1; after every round the first player passes clockwise. In the Unity build the names are entered clockwise at setup.", "Place clockwise from the first player"),
        R("R-07", "5",    "In the Build phase you may place only the block style shown on your block card, and only while one of that style is left in the supply.", "Place only the style on your card"),
        R("R-08", "4, 5", "Each player places exactly 1 block per round (event card 17: the last player places a second block).", "1 placement per round (card 17: +1)"),
        R("R-09", "5",    "Blocks go on the one shared tower base: on its 16 spaces or on top of blocks there. Every part of the block must be over the 16 spaces; nothing may hang off the base.", "Place on one of the 16 shared spaces"),
        R("R-10", "5",    "Event card 6 (Union Rep Visits): every block this round must go on top of another block.", "Event 6: must go on top of a block"),
        R("R-11", "5",    "Event card 11 (Crane Swings Overhead): no part of a block may go on a Center Hub space this round.", "Event 11: no Center Hub this round"),
        R("R-12", "5",    "Event card 7 (Scaffolding Creaks): your block must touch the grid edge (at least one part over an outer space).", "Event 7: must use a grid-edge space"),
        R("R-13", "5, 7", "Event card 4 (Coffee Break): everyone skips placing. Event card 18 (Gas Line Scare): no draws, placements or shake.", "Event 4: skip. Event 18: full pause"),
        R("R-14", "4",    "The shake starts only after every player has placed or skipped.", "Shake only after everyone has placed"),
        R("R-15", "4",    "Only the first player runs the shake.", "Only the first player shakes"),
        R("R-16", "1, 4", "When the tower collapses (at least half of its blocks, and at least 2, came down in one placement or shake), the player who placed the most recent block loses; every other player scores 1 point.", "Tower collapses: last placer loses, rest +1"),
        R("R-17", "5",    "Event card 12 (Wind Picks Up): no part of a block may go on a Corner Zone space this round.", "Event 12: no Corner Zone this round"),
        R("R-18", "1, 7", "The match ends when a player has 3 points; players who reach 3 together share the win.", "First to 3 points wins"),
        R("R-19", "7",    "If the block deck ran short, rounds continue with only Shake and Check until the tower collapses: no event card is flipped, nobody draws or places. Each of these shake-only rounds shakes harder than the last.", "Deck short: harder shakes until it collapses"),
        R("R-20", "6",    "Event card 20 (Inspector's Final Walk-Through): if the tower falls this round, the loser also loses 1 point.", "Event 20: loser loses 1 point"),
        R("R-21", "Event deck", "Event card 1 (Site Inspector): on your turn you may redraw your block card once. Event card 13 (Permit Renewed): each player draws 2 block cards and places one of them.", "Events 1, 13: redraw / extra option"),
        R("R-22", "Event deck", "Event card 9 (Second Crew Arrives): before anyone places, the first player picks a rival, who redraws their block card.", "Event 9: first player picks a rival"),
        R("R-23", "Event deck", "Shake events: cards 5, 10 and 19 shake the tower twice this round; card 15 runs the shake at double speed; card 16 shakes twice before any new placement.", "Events 5, 10, 15, 16, 19: shakes"),
        R("R-24", "Event deck", "Event card 14 (Foreman Tightens the Schedule): place within a ten-count, or skip your placement.", "Event 14: place within 10 seconds"),
        R("R-25", "Event deck", "Event card 3 (Rebar Delivery): every block this round is placed blind.", "Event 3: place blind"),
        R("R-26", "5, 7", "If your block card can't be placed (none of that style is left in the supply, or no space on the tower fits it), discard it and draw again. If the block deck is empty, you skip.", "Dead card: discard and redraw"),
    };

    static Rule R(string id, string section, string text, string brief)
    {
        return new Rule { id = id, section = section, text = text, brief = brief };
    }

    // Rules from the rulesheet the build does not check, and why.
    public static readonly string[] NotChecked =
    {
        "Who is tallest (section 3): measured at the table. The Unity build makes the first name entered the first player and seats the rest clockwise in the order entered.",
        "Event card 14's ten-count and event card 3's blind placement are timed and hidden on screen in the Unity build, but at the table they rely on the players.",
        "A collapse on the real tower base is judged by eye; the Unity build counts it as half of the blocks (at least 2) coming down in one placement or shake.",
    };

    public static Rule Get(string id)
    {
        foreach (Rule r in All) if (r.id == id) return r;
        return new Rule { id = id, section = "?", text = "(unknown rule)", brief = "" };
    }

    public static string Describe()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<b>RULES CHECKED BY THE BUILD</b>  (rulesheet section in brackets)");
        foreach (Rule r in All) sb.AppendLine("<b>" + r.id + "</b>  " + r.text + "  [" + r.section + "]");
        sb.AppendLine();
        sb.AppendLine("<b>NOT CHECKED, AND WHY</b>");
        foreach (string n in NotChecked) sb.AppendLine("- " + n);
        return sb.ToString();
    }

    public static string DescribeShort()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<b>RULES CHECKED</b>");
        foreach (Rule r in All) sb.AppendLine("<b>" + r.id + "</b>  " + r.brief);
        sb.AppendLine();
        sb.AppendLine("<b>NOT CHECKED</b> (physical): whether the tower fell,");
        sb.AppendLine("fallen blocks, angle, who is tallest,");
        sb.AppendLine("event cards 3, 14 and shake speed.");
        return sb.ToString();
    }
}
