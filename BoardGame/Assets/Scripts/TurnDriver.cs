using UnityEngine;

// Driver scene: plays whole turns through every phase and prints the legal action list at each phase.
public class TurnDriver : MonoBehaviour
{
    public DeckManager blockDeck;
    public DeckManager eventDeck;

    [Range(TableNumbers.MinPlayers, TableNumbers.MaxPlayers)]
    public int playerCount = 3;

    [Tooltip("Full rounds to play. The assignment asks for at least 2.")]
    public int roundsToRun = 2;

    public bool runOnStart = true;

    void Start()
    {
        if (runOnStart) Run();
    }

    [ContextMenu("Run Driver")]
    public void Run()
    {
        TurnStateMachine machine = new TurnStateMachine(blockDeck, eventDeck, playerCount);
        machine.Log += message => Debug.Log(message);

        int guard = 0;
        while (machine.State.round <= roundsToRun && !machine.MatchOver && guard++ < 1000)
        {
            machine.StepPhase();
        }
        Debug.Log("Driver finished after " + (machine.State.round - 1) + " completed round(s).");
    }
}
