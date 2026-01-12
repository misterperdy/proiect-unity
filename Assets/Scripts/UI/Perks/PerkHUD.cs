using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerkHUD : MonoBehaviour
{
    [Header("References")]
    public Transform container;
    public GameObject iconPrefab; // needs PerkHUDItem script attached

    // reference to stats to see perks
    private PlayerStats playerStats;

    void Start()
    {
        playerStats = FindObjectOfType<PlayerStats>();
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (playerStats == null) return;

        // 1. delete all old icons first
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        // 2. count duplicates so we stack them
        // Dictionary key is perk, value is count
        Dictionary<PerkSO, int> perkCounts = new Dictionary<PerkSO, int>();

        foreach (PerkSO perk in playerStats.acquiredPerks)
        {
            if (perkCounts.ContainsKey(perk))
            {
                perkCounts[perk]++; // increase count
            }
            else
            {
                perkCounts.Add(perk, 1); // add new
            }
        }

        // 3. spawn icons
        foreach (KeyValuePair<PerkSO, int> entry in perkCounts)
        {
            PerkSO perk = entry.Key;
            int count = entry.Value;

            GameObject newIcon = Instantiate(iconPrefab, container);
            newIcon.GetComponent<PerkHUDItem>().Setup(perk, count);
        }
    }
}