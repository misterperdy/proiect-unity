using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelingSystem : MonoBehaviour
{
    [Header("Stats")]
    public int currentLevel = 1;
    public int currentXP = 0;
    public int xpRequiredForNextLevel;


    public int currentAmountGained = 0;
    public PerkManager perkManager;

    private PlayerStats _playerStats;

    [Header("Debug")]
    public int previousLevelCost = 0; // memory for math formula

    [Header("Debug XP Spawn (DEV ONLY)")]
    public bool enableDebugXpSpawn = true;
    public GameObject xpOrbPrefab;
    public int debugOrbCount = 60;
    public int debugXpPerOrb = 5;
    public float debugSpawnRadius = 3f;
    public KeyCode debugSpawnKey = KeyCode.N;

    void Start()
    {
        _playerStats = GetComponent<PlayerStats>();

        // setup first level cost
        // basic formula setup
        CalculateNextLevelCost();
    }

    void Update()
    {
        // --- DEBUG CHEATS ---
        if (!enableDebugXpSpawn) return;
        if (!Input.GetKeyDown(debugSpawnKey)) return;
        if (!Input.GetKey(KeyCode.LeftControl)) return;

        Debug.Log("[DEV] Spawning debug XP orbs...");

        if (xpOrbPrefab == null)
        {
            // fallback: try to steal prefab from enemies if i forgot to assign it
            EnemyAI enemy = FindObjectOfType<EnemyAI>();
            if (enemy != null) xpOrbPrefab = enemy.xpOrbPrefab;
            ShooterEnemy shooter = FindObjectOfType<ShooterEnemy>();
            if (xpOrbPrefab == null && shooter != null) xpOrbPrefab = shooter.xpOrbPrefab;
        }

        // if still null, just give xp directly
        if (xpOrbPrefab == null)
        {
            int xpFallback = Mathf.Max(1, debugOrbCount) * Mathf.Max(1, debugXpPerOrb);
            Debug.LogWarning($"[DEV] XP orb prefab not found; granting {xpFallback} XP directly instead.");
            GainXP(xpFallback);
            return;
        }

        // spawn loop for orbs
        int count = Mathf.Max(1, debugOrbCount);
        int xpAmount = Mathf.Max(1, debugXpPerOrb);
        float radius = Mathf.Max(0.1f, debugSpawnRadius);

        for (int i = 0; i < count; i++)
        {
            Vector2 offset2D = Random.insideUnitCircle * radius;
            Vector3 spawnPos = transform.position + new Vector3(offset2D.x, 0.5f, offset2D.y);

            GameObject orb = Instantiate(xpOrbPrefab, spawnPos, Quaternion.identity);
            XPOrb xp = orb.GetComponent<XPOrb>();
            if (xp != null) xp.Initialize(xpAmount);
        }
    }

    public void GainXP(int amount)
    {
        if (amount <= 0) return;

        // apply multiplier from stats
        float mult = (_playerStats != null) ? _playerStats.GetXpMultiplier() : 1f;
        int finalAmount = Mathf.RoundToInt(amount * mult);
        if (finalAmount < 1) finalAmount = 1;

        currentXP += finalAmount;

        currentAmountGained = finalAmount;

        // loop in case we gain enough xp for multiple levels at once
        while (currentXP >= xpRequiredForNextLevel)
        {
            LevelUp();
        }
    }

    void LevelUp()
    {
        currentXP -= xpRequiredForNextLevel; // keep extra xp
        currentLevel++;

        // save old cost
        previousLevelCost = xpRequiredForNextLevel;

        CalculateNextLevelCost();

        Debug.Log("LEVEL UP! New Level: " + currentLevel);

        // open ui
        if (perkManager != null)
            perkManager.NotifyLevelUp();
    }

    void CalculateNextLevelCost()
    {
        // linear growth formula
        xpRequiredForNextLevel = (currentLevel * 5) + previousLevelCost;
    }
}