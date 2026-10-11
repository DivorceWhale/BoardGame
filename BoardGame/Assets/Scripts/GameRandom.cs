using System.Collections.Generic;
using UnityEngine;

// The ONLY source of randomness in the project. One seed, one generator.
public class GameRandom : MonoBehaviour
{
    private static GameRandom instance;
    public static GameRandom Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new GameObject("GameRandom").AddComponent<GameRandom>();
            }
            return instance;
        }
    }

    [Header("One seed for the whole run")]
    public int seed = 12345;

    private System.Random rng;

    void Awake()
    {
        instance = this;
        rng = new System.Random(seed);
    }

    private System.Random Rng
    {
        get
        {
            if (rng == null) rng = new System.Random(seed);
            return rng;
        }
    }

    public int Next(int maxExclusive)
    {
        return Rng.Next(maxExclusive);
    }

    public void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Rng.Next(i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
