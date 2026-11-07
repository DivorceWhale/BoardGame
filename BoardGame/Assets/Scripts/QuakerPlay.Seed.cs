using UnityEngine;

// The random seed for the Play scene, like the earlier builds: one GameRandom object in the scene holds the
// seed, and every shuffle of the block and event decks comes from it. It is for testing only and is never
// shown to players; the seed each game used is written to the Console so a game can be replayed.
//
//   New Seed Every Game off: the match starts from GameRandom's Seed, so the same seed deals the same cards
//                            in the same order every time (the later games of the match follow on from it).
//   New Seed Every Game on:  every game of the match gets a fresh random seed.
public partial class QuakerPlay
{
    [Header("Random seed (testing only, never shown to players)")]
    [Tooltip("Off: every match starts from the Seed on the GameRandom object, so it can be replayed exactly. "
        + "On: every game gets a fresh random seed. The seed used is written to the Console either way.")]
    public bool newSeedEachGame;

    int currentSeed;

    // Seeds the shuffles for game number `game` (1 = the start of the match).
    void SeedGame(int game)
    {
        GameRandom r = GameRandom.Instance;
        if (newSeedEachGame)
        {
            currentSeed = new System.Random().Next(1, 1000000);
            r.Reseed(currentSeed);
            Debug.Log("[Quaker] Game " + game + " seed: " + currentSeed + " (new seed every game). To replay it, set GameRandom's Seed to "
                + currentSeed + " and untick New Seed Every Game.");
        }
        else if (game == 1)
        {
            currentSeed = r.seed;
            r.Reseed(currentSeed);
            Debug.Log("[Quaker] Match seed: " + currentSeed + " (GameRandom's Seed). The same seed deals the same cards.");
        }
        // Fixed seed, later games: keep drawing from the same sequence, so the whole match repeats exactly.
    }
}
