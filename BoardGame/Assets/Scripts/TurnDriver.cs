using System.Collections.Generic;
using UnityEngine;

// Game-scene driver. Initializes everything from the chosen scenario asset, then plays rounds
// through every phase and prints the legal action list at each one.
public class TurnDriver : MonoBehaviour
{
    public DeckManager blockDeck;
    public DeckManager eventDeck;
    public BoardBuilder boardBuilder;

    [Tooltip("Used when the game scene is played directly, without the start-up scene.")]
    public ScenarioConfig defaultScenario;

    [Tooltip("All five scenario assets, for the Run All Scenarios check.")]
    public List<ScenarioConfig> allScenarios = new List<ScenarioConfig>();

    [Tooltip("Full rounds to play. The assignment asks for at least 2.")]
    public int roundsToRun = 2;
    public bool runOnStart = true;

    void Start()
    {
        if (runOnStart) Run();
    }

    [ContextMenu("Run Chosen Scenario")]
    public void Run()
    {
        ScenarioConfig sc = ScenarioSession.Current != null ? ScenarioSession.Current : defaultScenario;
        if (sc == null)
        {
            Debug.LogError("No scenario chosen. Start from the start-up scene or set Default Scenario.");
            return;
        }
        RunScenario(sc);
    }

    [ContextMenu("Run All Scenarios")]
    public void RunAll()
    {
        int passed = 0;
        foreach (ScenarioConfig sc in allScenarios)
        {
            if (RunScenario(sc)) passed++;
        }
        Debug.Log("Scenarios that loaded, rendered and ran: " + passed + " of " + allScenarios.Count);
    }

    bool RunScenario(ScenarioConfig sc)
    {
        if (sc.board == null)
        {
            Debug.LogError(sc.name + ": no board assigned.");
            return false;
        }
        if (!sc.board.Validate()) return false;

        GameRandom.Instance.Reseed(GameRandom.Instance.seed);   // each run is repeatable
        if (boardBuilder != null) boardBuilder.Build(sc.board, sc);
        blockDeck.allowRefill = sc.refillWhenEmpty;
        eventDeck.allowRefill = true;
        blockDeck.Configure(sc.blockDeck);
        eventDeck.Configure(sc.eventDeck);

        TurnStateMachine machine = new TurnStateMachine(blockDeck, eventDeck, sc);
        machine.Log += message => Debug.Log(message);

        Debug.Log("##### Scenario: " + sc.displayName + " (" + sc.playerCount + " players, "
            + sc.board.spaces.Count + " spaces) #####");

        int guard = 0;
        while (machine.State.round <= roundsToRun && !machine.MatchOver && guard++ < 1000)
        {
            machine.StepPhase();
        }
        bool ok = guard < 1000;
        Debug.Log("Scenario " + sc.displayName + (ok ? " ran " : " DID NOT FINISH ")
            + (machine.State.round - 1) + " completed round(s).");
        return ok;
    }
}
