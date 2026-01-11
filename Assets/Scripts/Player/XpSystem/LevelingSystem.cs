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

    [Header("Debug")]
    public int previousLevelCost = 0; // Tracks "Cost(n-1)" for formula

    [Header("Debug XP Spawn (DEV ONLY)")]
    public bool enableDebugXpSpawn = true;
    public GameObject xpOrbPrefab;
    public int debugOrbCount = 60;
    public int debugXpPerOrb = 5;
    public float debugSpawnRadius = 3f;
    public KeyCode debugSpawnKey = KeyCode.N;

    void Start()
    {
        // Initial setup for Level 1 -> 2
        // Formula: currentLevel(1) * 10 + previous(0) = 10
        CalculateNextLevelCost();
    }

    void Update()
    {
        if (!enableDebugXpSpawn) return;
        if (!Input.GetKeyDown(debugSpawnKey)) return;

        Debug.Log("[DEV] Spawning debug XP orbs...");

        if (xpOrbPrefab == null)
        {
            // Fallback: grab the prefab reference from any enemy in the scene.
            EnemyAI enemy = FindObjectOfType<EnemyAI>();
            if (enemy != null) xpOrbPrefab = enemy.xpOrbPrefab;
            ShooterEnemy shooter = FindObjectOfType<ShooterEnemy>();
            if (xpOrbPrefab == null && shooter != null) xpOrbPrefab = shooter.xpOrbPrefab;
        }

        if (xpOrbPrefab == null)
        {
            int xpFallback = Mathf.Max(1, debugOrbCount) * Mathf.Max(1, debugXpPerOrb);
            Debug.LogWarning($"[DEV] XP orb prefab not found; granting {xpFallback} XP directly instead.");
            GainXP(xpFallback);
            return;
        }

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
        currentXP += amount;

        currentAmountGained = amount;

        // Check for Level Up (While loop in case we get huge XP and skip multiple levels)
        while (currentXP >= xpRequiredForNextLevel)
        {
            LevelUp();
        }
    }

    void LevelUp()
    {
        currentXP -= xpRequiredForNextLevel; // Carry over excess XP
        currentLevel++;

        // Save the cost of the level we just finished to use in the formula
        previousLevelCost = xpRequiredForNextLevel;

        CalculateNextLevelCost();

        Debug.Log("LEVEL UP! New Level: " + currentLevel);

        if (perkManager != null)
            perkManager.NotifyLevelUp();
    }

    void CalculateNextLevelCost()
    {
        // Formula: currentLevel * 10 + previousLevelCost
        xpRequiredForNextLevel = (currentLevel * 10) + previousLevelCost;
    }
}