using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        [Range(0, 1)] public float spawnChance; // spawn probability
    }

    [Header("TeleportSpawn")]
    public GameObject teleporter;

    // Data passed from DungeonGenerator
    private List<GameObject> enemyPrefabs;
    private float difficultyMultiplier; // scales with distance
    private float biomeStatMultiplier;
    private RaritySettings[] rarities;
    private int minEnemies;
    private int maxEnemies;
    private int nrEnemies;

    // Tracking
    private List<GameObject> activeEnemies = new List<GameObject>();
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

        // Creating trigger via code
        triggerCollider = gameObject.AddComponent<BoxCollider>();
        triggerCollider.isTrigger = true;

        // sizing the collider to fit the room almost perfectly
        float sizeX = (wTiles * tileSize) - 1.0f;
        float sizeZ = (hTiles * tileSize) - 1.0f;

        triggerCollider.size = new Vector3(sizeX, 5f, sizeZ);
        triggerCollider.center = new Vector3(0, 2.5f, 0);
    }

    private void OnTriggerEnter(Collider other)
    {
        // spawn when player walks in
        if (!hasSpawned && other.CompareTag("Player"))
        {
            SpawnEnemies();
        }
    }

    void SpawnEnemies()
    {
        hasSpawned = true;
        activeEnemies.Clear();
        if (enemyPrefabs == null || enemyPrefabs.Count == 0) return;

        // randomizing count
        int count = Random.Range(minEnemies, maxEnemies + 1);

        nrEnemies = count;

        for (int i = 0; i < count; i++)
        {
            SpawnSingleEnemy();
        }

        Destroy(triggerCollider); // destroy trigger so we dont spawn again

        StartCoroutine(TrackEnemies());
    }

    IEnumerator TrackEnemies()
    {
        // wait for init
        yield return null;

        while (activeEnemies.Count > 0)
        {
            // removing nulls (dead enemies)
            activeEnemies.RemoveAll(x => x == null);

            if (activeEnemies.Count == 0)
            {
                // room clear
                SpawnTeleporter();
                yield break;
            }

            // efficient check every 0.5s
            yield return new WaitForSeconds(0.5f);
        }

        // fallback if 0 enemies
        SpawnTeleporter();
    }

    void SpawnTeleporter()
    {
        Debug.Log("Room Cleared!");
        if (teleporter != null)
        {
            // spawn reward in middle of room
            Vector3 centerPos = new Vector3(transform.position.x, transform.position.y + 0.33f, transform.position.z);
            Instantiate(teleporter, centerPos, Quaternion.identity);
        }
    }

    void SpawnSingleEnemy()
    {
        // 1. Pick Type
        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];

        // 2. Pick Pos
        Bounds b = triggerCollider.bounds;

        // random pos inside box
        float randomX = Random.Range(b.min.x, b.max.x);
        float randomZ = Random.Range(b.min.z, b.max.z);

        Vector3 spawnPos = new Vector3(randomX, transform.position.y + 1.0f, randomZ);

        // 3. Spawn
        GameObject enemyObj = Instantiate(prefab, spawnPos, Quaternion.identity);

        activeEnemies.Add(enemyObj);

        // 4. Stats logic
        EnemyRarity rarity = DetermineRarity();
        RaritySettings settings = GetRaritySettings(rarity);

        float statMult = difficultyMultiplier * biomeStatMultiplier * settings.statMult;

        // trying to find which script the enemy has to set stats
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
        // rolling dice for rarity
        float boost = (difficultyMultiplier - 1.0f) * 0.1f;

        // check rare
        RaritySettings rare = GetRaritySettings(EnemyRarity.Rare);
        if (Random.value < (rare.spawnChance + boost)) return EnemyRarity.Rare;

        // check magic
        RaritySettings magic = GetRaritySettings(EnemyRarity.Magic);
        if (Random.value < (magic.spawnChance + boost)) return EnemyRarity.Magic;

        return EnemyRarity.Normal;
    }

    RaritySettings GetRaritySettings(EnemyRarity r)
    {
        // finding settings in array
        foreach (var s in rarities) if (s.name == r.ToString()) return s;
        return rarities[0];
    }
}