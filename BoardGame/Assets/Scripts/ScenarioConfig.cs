using System;
using System.Collections.Generic;
using UnityEngine;

// Add values here only when a Padlet scenario post needs them, then fill the field for ALL five assets.
public enum VictoryType
{
    LastTowerStanding,     // Loose Change, Four Corners, The Sliver
    TargetHeight,
    TallestAtRoundLimit,   // Fuse Lit
    LeadByMargin           // Thin Margins
}

[Serializable]
public class DeckEntry
{
    public CardData card;
    [Min(0)] public int copies = 1;
}

[Serializable]
public class StartingBlock
{
    public int playerIndex;
    public int spaceId;
    public CardData block;
    public int angle;
}

// Forces where a player's FIRST block of a game must go (Four Corners).
[Serializable]
public class FirstPlacement
{
    public int playerIndex;
    public int spaceId;
}

// A scenario is a configuration, not a scene. Everything that varies between scenarios lives here.
[CreateAssetMenu(fileName = "NewScenario", menuName = "QUAKER/Scenario")]
public class ScenarioConfig : ScriptableObject
{
    [Header("Identity (name the asset exactly as the Padlet post does)")]
    public string displayName;
    public string experienceGoal;
    [TextArea(2, 4)] public string description;

    [Header("Players")]
    [Range(TableNumbers.MinPlayers, TableNumbers.MaxPlayers)]
    public int playerCount = 3;

    [Header("Board")]
    public BoardLayout board;
    [Tooltip("Space ids that are blocked in this scenario. Every other space is open.")]
    public List<int> blockedSpaceIds = new List<int>();

    [Header("Starting state")]
    public List<StartingBlock> startingBlocks = new List<StartingBlock>();
    public List<FirstPlacement> firstPlacementSpaces = new List<FirstPlacement>();
    [Min(0)] public int startingPointsPerPlayer = 0;

    [Header("Cards in play")]
    public List<DeckEntry> blockDeck = new List<DeckEntry>();
    public List<DeckEntry> eventDeck = new List<DeckEntry>();
    [Tooltip("Off = no refills: used cards never return to the draw pile.")]
    public bool refillWhenEmpty = true;

    [Header("Victory")]
    public VictoryType victoryType = VictoryType.LastTowerStanding;
    [Tooltip("TargetHeight only: blocks needed on a standing tower.")]
    public int targetHeight = 0;
    [Tooltip("LeadByMargin only: blocks a standing tower must be above every other standing tower.")]
    public int leadMargin = 0;
    [Min(1)] public int pointsPerGameWin = 1;
    [Min(1)] public int pointsToWinMatch = 3;

    [Header("Consolation (0 = off)")]
    [Tooltip("A player whose tower reaches this height at any point banks consolation points at game end.")]
    public int consolationHeight = 0;
    public int consolationPoints = 0;

    [Header("Limits")]
    [Tooltip("0 = no limit.")]
    public int roundLimit = 0;

    [Header("Harder shake after a fall (empty tags = off)")]
    [Tooltip("Once a tower based on a space with this tag has fallen...")]
    public string shakeBoostTriggerTag = "";
    [Tooltip("...towers based on a space with this tag face a harder shake.")]
    public string shakeBoostTargetTag = "";
    [Range(0f, 1f)] public float shakeBoostChance = 0f;

    [Header("PLACEHOLDER until the table decides (the real shake is physical)")]
    [Range(0f, 1f)] public float placeholderCollapseChance = 0.15f;

    public bool IsOpen(int spaceId)
    {
        return !blockedSpaceIds.Contains(spaceId);
    }
}
