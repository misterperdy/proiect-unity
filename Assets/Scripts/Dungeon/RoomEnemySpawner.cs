using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using Color = UnityEngine.Color;

public class RoomEnemySpawner : MonoBehaviour
{
    public enum EnemyRarity { Normal, Magic, Rare }

    [System.Serializable]
    public struct RaritySettings
    {
        public string name;
        public Color color;
        public float statMult;
        public float lootMult;
        [Range(0, 1)] public float spawnChance; // Chance to upgrade to this rarity
    }

    // Data passed from DungeonGenerator
    private List<GameObject> enemyPrefabs;
    private float difficultyMultiplier; // Based on distance
    private float biomeStatMultiplier;
    private RaritySettings[] rarities;
    private int minEnemies;
    private int maxEnemies;

    private bool hasSpawned = false;
    private BoxCollider triggerCollider;

    public void Initialize(List<GameObject> enemies, float distMult, float biomeMult, RaritySettings[] rarityConfig, int min, int max, int wTiles, int hTiles, float tileSize)
    {
        enemyPrefabs = enemies;
        difficultyMultiplier = distMult;
        biomeStatMultiplier = biomeMult;
        rarities = rarityConfig;
        minEnemies = min;
        maxEnemies = max;

        // Create Trigger
        triggerCollider = gameObject.AddComponent<BoxCollider>();
        triggerCollider.isTrigger = true;

        // Calculate exact size based on the grid
        // We subtract a small padding (e.g. 1.0f) so the trigger is slightly inside the walls
        float sizeX = (wTiles * tileSize) - 1.0f;
        float sizeZ = (hTiles * tileSize) - 1.0f;

        triggerCollider.size = new Vector3(sizeX, 5f, sizeZ);
        triggerCollider.center = new Vector3(0, 2.5f, 0); // Center is 0,0 because RoomGenerator centers the room
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasSpawned && other.CompareTag("Player"))
        {
            SpawnEnemies();
        }
    }

    void SpawnEnemies()
    {
        hasSpawned = true;
        if (enemyPrefabs == null || enemyPrefabs.Count == 0) return;

        // Determine how many to spawn (Scaling with difficulty slightly?)
        int count = Random.Range(minEnemies, maxEnemies + 1);

        for (int i = 0; i < count; i++)
        {
            SpawnSingleEnemy();
        }

        Destroy(triggerCollider); // Don't trigger again
    }

    void SpawnSingleEnemy()
    {
        // 1. Pick Enemy Type
        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];

        // 2. Pick Position 
        // Get the Bounds of the trigger we just created to ensure we spawn inside
        Bounds b = triggerCollider.bounds;

        // Pick a random point inside the bounds
        float randomX = Random.Range(b.min.x, b.max.x);
        float randomZ = Random.Range(b.min.z, b.max.z);

        Vector3 spawnPos = new Vector3(randomX, transform.position.y + 1.0f, randomZ);

        // 3. Instantiate
        GameObject enemyObj = Instantiate(prefab, spawnPos, Quaternion.identity);

        // 4. Calculate Rarity & Stats
        EnemyRarity rarity = DetermineRarity();
        RaritySettings settings = GetRaritySettings(rarity);

        float statMult = difficultyMultiplier * biomeStatMultiplier * settings.statMult;

        // We need to read base stats. This is tricky without a common interface.
        // We will check for each component.

        if (enemyObj.TryGetComponent(out EnemyAI melee))
        {
            int hp = Mathf.RoundToInt(melee.maxHealth * statMult);
            int dmg = Mathf.RoundToInt(melee.damage * statMult);
            float attSpeed = melee.attackSpeed * statMult;
            float spd = melee.moveSpeed + statMult;
            float lootMult = statMult * settings.lootMult;
            melee.SetupEnemy(hp, dmg, attSpeed, spd, settings.color, lootMult, rarity.ToString());
        }
        else if (enemyObj.TryGetComponent(out ShooterEnemy shooter))
        {
            int hp = Mathf.RoundToInt(shooter.maxHealth * statMult);
            int dmg = Mathf.RoundToInt(10 * statMult);
            float fireRateMult = shooter.fireRateMultiplier * statMult;
            int BPS = Mathf.RoundToInt(shooter.bulletsPerShot * statMult);
            float spreadAng = shooter.spreadAngle * statMult;
            int aimErr = Mathf.RoundToInt(shooter.aimError);
            float lootMult = statMult * settings.lootMult;
            shooter.SetupEnemy(hp, dmg, fireRateMult, BPS, spreadAng,
            aimErr, settings.color, lootMult, rarity.ToString());
        }
        else if (enemyObj.TryGetComponent(out KamikazeEnemyAI kami))
        {
            int hp = Mathf.RoundToInt(kami.maxHealth * statMult);
            int dmg = Mathf.RoundToInt(kami.explosionDamage * statMult);
            float lootMult = statMult * settings.lootMult;
            kami.SetupEnemy(hp, dmg, settings.color, lootMult, rarity.ToString());
        }
    }

    EnemyRarity DetermineRarity()
    {
        // Logic: Roll for Rare first, then Magic, else Normal.
        // Chances scale with difficultyMultiplier (distance)

        float boost = (difficultyMultiplier - 1.0f) * 0.1f; // +10% chance per difficulty level

        // Check Rare
        RaritySettings rare = GetRaritySettings(EnemyRarity.Rare);
        if (Random.value < (rare.spawnChance + boost)) return EnemyRarity.Rare;

        // Check Magic
        RaritySettings magic = GetRaritySettings(EnemyRarity.Magic);
        if (Random.value < (magic.spawnChance + boost)) return EnemyRarity.Magic;

        return EnemyRarity.Normal;
    }

    RaritySettings GetRaritySettings(EnemyRarity r)
    {
        // Simple search (Optimization: use Dictionary)
        foreach (var s in rarities) if (s.name == r.ToString()) return s;
        return rarities[0]; // Default Normal
    }
}