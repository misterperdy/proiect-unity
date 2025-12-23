using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerkManager : MonoBehaviour
{
    [Header("References")]
    public GameObject perkMenuCanvas;
    public Transform cardsContainer;
    public GameObject perkCardPrefab; // Needs PerkUIItem script
    public PlayerStats playerStats;

    [Header("Data")]
    public List<PerkSO> allPerks;
    public int optionsToOffer = 3;

    private int pendingPerks = 0;

    private void Start()
    {
        perkMenuCanvas.SetActive(false);
        // Find player stats automatically if not assigned
        if (playerStats == null) playerStats = FindObjectOfType<PlayerStats>();
    }

    public void NotifyLevelUp()
    {
        pendingPerks++;

        // Only open the menu if it's not already open
        if (!perkMenuCanvas.activeSelf)
        {
            ShowPerkSelection();
        }
    }

    // Call this function from your LevelingSystem when CurrentXP >= RequiredXP
    public void ShowPerkSelection()
    {
        Time.timeScale = 0f; // Pause the game
        perkMenuCanvas.SetActive(true);

        // Clear old cards
        foreach (Transform child in cardsContainer)
        {
            Destroy(child.gameObject);
        }

        // Pick random perks
        List<PerkSO> choices = GetRandomPerks();

        // Create UI cards
        foreach (PerkSO p in choices)
        {
            GameObject card = Instantiate(perkCardPrefab, cardsContainer);
            card.GetComponent<PerkUIItem>().Setup(p, this);
        }
    }

    public void SelectPerk(PerkSO perk)
    {
        // Apply the effect
        playerStats.ApplyPerk(perk);

        pendingPerks--;

        if (pendingPerks > 0)
        {
            // Don't close the menu!
            // Just refresh the cards for the next choice.
            ShowPerkSelection();
        }
        else
        {
            // No more levels? Now we resume.
            perkMenuCanvas.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    List<PerkSO> GetRandomPerks()
    {
        List<PerkSO> deck = new List<PerkSO>(allPerks);
        List<PerkSO> selection = new List<PerkSO>();

        for (int i = 0; i < optionsToOffer; i++)
        {
            if (deck.Count == 0) break;

            int randomIndex = Random.Range(0, deck.Count);
            selection.Add(deck[randomIndex]);
            deck.RemoveAt(randomIndex); // Don't pick the same one twice
        }

        return selection;
    }
}