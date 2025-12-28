using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    [Header("Luck Balancing")]
    // How much weight does 1 Luck point add to these rarities?
    public int luckBonusCommon = 0;
    public int luckBonusRare = 5;      // 10 Luck = +50 Weight
    public int luckBonusEpic = 2;
    public int luckBonusLegendary = 1;

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
        List<PerkSO> choices = GetWeightedRandomPerks();

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

    List<PerkSO> GetWeightedRandomPerks()
    {
        List<PerkSO> selectedPerks = new List<PerkSO>();

        // 1. Create a "Pool" of valid perks
        // Filter out One-Time perks that we already have
        List<PerkSO> validPool = new List<PerkSO>();

        foreach (PerkSO p in allPerks)
        {
            if (p.isOneTimeOnly && playerStats.HasPerk(p))
            {
                continue; // Skip, we already have it
            }
            validPool.Add(p);
        }

        // 2. Pick 'optionsToOffer' amount of perks
        for (int i = 0; i < optionsToOffer; i++)
        {
            if (validPool.Count == 0) break;

            // 2a. Calculate Total Weight based on Luck
            int totalWeight = 0;
            Dictionary<PerkSO, int> currentWeights = new Dictionary<PerkSO, int>();

            foreach (PerkSO p in validPool)
            {
                int modifiedWeight = p.baseWeight + (playerStats.luck * GetLuckBonus(p.rarity));
                if (modifiedWeight < 1) modifiedWeight = 1; // Prevent 0 weight

                currentWeights.Add(p, modifiedWeight);
                totalWeight += modifiedWeight;
            }

            // 2b. Roll the dice
            int randomNumber = Random.Range(0, totalWeight);
            int weightSum = 0;
            PerkSO pickedPerk = null;

            foreach (var kvp in currentWeights)
            {
                weightSum += kvp.Value;
                if (randomNumber < weightSum)
                {
                    pickedPerk = kvp.Key;
                    break;
                }
            }

            // 2c. Add to result and remove from pool so we don't pick it twice in same hand
            if (pickedPerk != null)
            {
                selectedPerks.Add(pickedPerk);
                validPool.Remove(pickedPerk);
            }
        }

        return selectedPerks;
    }

    int GetLuckBonus(PerkRarity rarity)
    {
        switch (rarity)
        {
            case PerkRarity.Common: return luckBonusCommon;
            case PerkRarity.Rare: return luckBonusRare;
            case PerkRarity.Epic: return luckBonusEpic;
            case PerkRarity.Legendary: return luckBonusLegendary;
            default: return 0;
        }
    }
}