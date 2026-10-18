using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Start-up scene: lists the scenario assets, lets a player pick one, then loads the game scene.
// The Start button stays greyed out until a scenario button has been clicked.
public class StartMenu : MonoBehaviour
{
    public List<ScenarioConfig> scenarios = new List<ScenarioConfig>();
    public Button scenarioButtonPrefab;   // needs a TMP_Text child
    public Transform listRoot;
    public TMP_Text descriptionText;
    public Button startButton;
    public string gameSceneName = "Game";

    private ScenarioConfig selected;

    void Start()
    {
        if (EventSystem.current == null)
        {
            Debug.LogWarning("StartMenu: there is no EventSystem in the scene, so no button will respond. Add one with GameObject > UI > Event System.");
        }
        if (scenarioButtonPrefab == null || listRoot == null)
        {
            Debug.LogError("StartMenu: Scenario Button Prefab or List Root is not assigned.");
            return;
        }
        if (scenarios.Count == 0)
        {
            Debug.LogError("StartMenu: the Scenarios list is empty, so there is nothing to click.");
        }

        int spawned = 0;
        foreach (ScenarioConfig s in scenarios)
        {
            if (s == null)
            {
                Debug.LogWarning("StartMenu: an empty entry in the Scenarios list was skipped.");
                continue;
            }
            Button b = Instantiate(scenarioButtonPrefab, listRoot);
            TMP_Text label = b.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = s.displayName; else Debug.LogWarning("StartMenu: the button prefab has no TMP_Text child.");
            ScenarioConfig captured = s;
            b.onClick.AddListener(() => Select(captured));
            spawned++;
        }
        Debug.Log("StartMenu: created " + spawned + " scenario button(s).");

        if (startButton != null)
        {
            startButton.interactable = false;
            startButton.onClick.AddListener(StartGame);
        }
        else
        {
            Debug.LogError("StartMenu: Start Button is not assigned.");
        }
    }

    void Select(ScenarioConfig s)
    {
        selected = s;
        Debug.Log("StartMenu: selected " + s.displayName);
        if (descriptionText != null)
        {
            descriptionText.text = s.displayName + "\n" + s.description + "\nPlayers: " + s.playerCount;
        }
        if (startButton != null) startButton.interactable = true;
    }

    void StartGame()
    {
        if (selected == null) return;
        ScenarioSession.Current = selected;
        SceneManager.LoadScene(gameSceneName);
    }
}