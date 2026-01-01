using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerkHUD : MonoBehaviour
{
    [Header("References")]
    public Transform container;
    public GameObject iconPrefab; // Must have PerkHUDItem script

    // We need a reference to read the list
    private PlayerStats playerStats;

    void Start()
    {
        playerStats = FindObjectOfType<PlayerStats>();
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (playerStats == null) return;

        // 1. Clear existing icons
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        // 2. Count Duplicates using a Dictionary
        // Key = The Perk, Value = How many we have
        Dictionary<PerkSO, int> perkCounts = new Dictionary<PerkSO, int>();

        foreach (PerkSO perk in playerStats.acquiredPerks)
        {
            if (perkCounts.ContainsKey(perk))
            {
                perkCounts[perk]++; // Increment count
            }
            else
            {
                perkCounts.Add(perk, 1); // Add new entry
            }
        }

        // 3. Instantiate Icons
        foreach (KeyValuePair<PerkSO, int> entry in perkCounts)
        {
            PerkSO perk = entry.Key;
            int count = entry.Value;

            GameObject newIcon = Instantiate(iconPrefab, container);
            newIcon.GetComponent<PerkHUDItem>().Setup(perk, count);
        }
    }
}