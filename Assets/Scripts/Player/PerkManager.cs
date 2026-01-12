using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PerkManager : MonoBehaviour
{
    [Header("References")]
    public GameObject perkMenuCanvas;
    public Transform cardsContainer;
    public GameObject perkCardPrefab; // prefab for the ui card
    public PlayerStats playerStats;

    [Header("Data")]
    public List<PerkSO> allPerks;
    public int optionsToOffer = 3;

    [Header("Luck Balancing")]
    // changing weights based on luck stat
    public int luckBonusCommon = -5;
    public int luckBonusRare = 5;
    public int luckBonusEpic = 2;
    public int luckBonusLegendary = 1;

    private int pendingPerks = 0;

    private void Start()
    {
        perkMenuCanvas.SetActive(false);
        // auto find player stats if i forgot
        if (playerStats == null) playerStats = FindObjectOfType<PlayerStats>();
    }

    public void NotifyLevelUp()
    {
        pendingPerks++;

        // if menu isnt open, open it now
        if (!perkMenuCanvas.activeSelf)
        {
            ShowPerkSelection();
        }
    }

    // logic to pause game and show perks
    public void ShowPerkSelection()
    {
        Time.timeScale = 0f; // stopping the world
        perkMenuCanvas.SetActive(true);

        // destroy old cards from previous level up
        foreach (Transform child in cardsContainer)
        {
            Destroy(child.gameObject);
        }

        // calculate how many cards to show
        int effectiveOptionsToOffer = Mathf.Max(1, optionsToOffer + (playerStats != null ? playerStats.extraPerkOptionsBonus : 0));

        // getting random perks
        List<PerkSO> choices = GetWeightedRandomPerks(effectiveOptionsToOffer);

        // instantiating the ui cards
        foreach (PerkSO p in choices)
        {
            GameObject card = Instantiate(perkCardPrefab, cardsContainer);
            card.GetComponent<PerkUIItem>().Setup(p, this);
        }
    }

    public void SelectPerk(PerkSO perk)
    {
        // applying perk to player
        playerStats.ApplyPerk(perk);

        pendingPerks--;

        if (pendingPerks > 0)
        {
            // if we have more levels pending, refresh menu instead of closing
            ShowPerkSelection();
        }
        else
        {
            // resume game
            perkMenuCanvas.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    List<PerkSO> GetWeightedRandomPerks(int count)
    {
        List<PerkSO> selectedPerks = new List<PerkSO>();

        // 1. making a temp list of available perks
        List<PerkSO> validPool = new List<PerkSO>();

        foreach (PerkSO p in allPerks)
        {
            // filter out one-time perks we already have
            if (p.isOneTimeOnly && playerStats.HasPerk(p))
            {
                continue;
            }
            validPool.Add(p);
        }

        // 2. picking loop
        for (int i = 0; i < count; i++)
        {
            if (validPool.Count == 0) break; // panic break if no perks left

            // 2a. calculating total weight with luck logic
            int totalWeight = 0;
            Dictionary<PerkSO, int> currentWeights = new Dictionary<PerkSO, int>();

            foreach (PerkSO p in validPool)
            {
                // applying luck modifier to rarity weight
                int modifiedWeight = p.baseWeight + (playerStats.luck * GetLuckBonus(p.rarity));
                if (modifiedWeight < 1) modifiedWeight = 1; // safety min weight

                currentWeights.Add(p, modifiedWeight);
                totalWeight += modifiedWeight;
            }

            // 2b. rng
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

            // 2c. add to result list and remove from pool so we dont get duplicates
            if (pickedPerk != null)
            {
                selectedPerks.Add(pickedPerk);
                validPool.Remove(pickedPerk);
            }
        }

        return selectedPerks;
    }

    // helper to get bonus value based on rarity
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