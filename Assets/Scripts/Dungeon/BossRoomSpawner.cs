using UnityEngine;

public class BossRoomSpawner : MonoBehaviour
{
    private GameObject bossPrefab;
    private GameObject activeBoss;
    private bool hasSpawned = false; // flag to make sure we only spawn once
    private bool levelGenerated = false;
    private BoxCollider triggerCollider;

    private GameObject exitTeleporter;

    bool finalLevel = false;

    public void Initialize(GameObject bossToSpawn, int roomWidth, int roomHeight, float tileSize, bool finalLevel = false)
    {
        bossPrefab = bossToSpawn;
        // adding a trigger via code so i dont have to do it manually in editor
        triggerCollider = gameObject.AddComponent<BoxCollider>();
        triggerCollider.isTrigger = true;

        // math to make the trigger fit the room size perfectly
        float sizeX = (roomWidth * tileSize) - 2.0f;
        float sizeZ = (roomHeight * tileSize) - 2.0f;
        triggerCollider.size = new Vector3(sizeX, 10f, sizeZ);
        triggerCollider.center = new Vector3(0, 5f, 0);

        this.finalLevel = finalLevel;
    }


    private void OnTriggerEnter(Collider other)
    {
        // check if player entered and boss isnt here yet
        if (!hasSpawned && other.CompareTag("Player"))
        {
            PlayerMovement movement = other.GetComponent<PlayerMovement>();
            if (movement != null)
            {
                // lock camera or do specific boss room stuff
                movement.SetBossRoomState(true);
            }

            SpawnBoss();
        }
    }

    void SpawnBoss()
    {
        if (bossPrefab == null) return;
        hasSpawned = true;

        Vector3 spawnPos = transform.position + new Vector3(0, 0.2f, 0); // lift boss slightly so he doesnt clip floor
        activeBoss = Instantiate(bossPrefab, spawnPos, Quaternion.identity);

        // dont need the trigger anymore
        Destroy(triggerCollider);
    }
    void Update()
    {
        // logic: boss spawned, boss died (is null), and we haven't made the exit portal yet
        if (hasSpawned && activeBoss == null && !levelGenerated)
        {
            PlayerMovement movement = FindObjectOfType<PlayerMovement>();
            if (movement != null)
            {
                // unlock player camera/movement state
                movement.SetBossRoomState(false);
            }

            SpawnExitTeleporter();
        }

    }

    void SpawnExitTeleporter()
    {
        levelGenerated = true;
        Debug.Log("Boss Dead. Spawning Portal...");

        // ensure dungeon generator exists to get the prefab
        if (DungeonGenerator.instance != null && DungeonGenerator.instance.teleporterPrefab != null)
        {
            Vector3 portalPos = transform.position + new Vector3(0f, 0.3f, 0f);
            if (!finalLevel)
            {
                // normal level exit
                exitTeleporter = Instantiate(DungeonGenerator.instance.teleporterPrefab, portalPos, Quaternion.identity);

                exitTeleporter.name = "Teleporter_NextLevel";
            }
            else
            {
                // game over / win portal
                if (DungeonGenerator.instance.finalTeleporterPrefab != null)
                {
                    exitTeleporter = Instantiate(DungeonGenerator.instance.finalTeleporterPrefab, portalPos, Quaternion.identity);
                    exitTeleporter.name = "Teleporter_ending";
                }
            }

            if (DungeonGenerator.instance.medkitPrefab != null)
            {
                // give player a reward for winning
                GameObject medkit = Instantiate(DungeonGenerator.instance.medkitPrefab, portalPos + new Vector3(0, 0.28f, 7f), Quaternion.Euler(0, 90, 0));
                medkit.name = "Medkit";
            }

            // generate the next level data immediately so the portal knows where to link
            Vector3 nextLevelStart = DungeonGenerator.instance.GenerateNextLevel(portalPos, false);

            exitTeleporter.GetComponent<TeleporterBoss>().SetDestination(nextLevelStart);
            exitTeleporter.GetComponent<TeleporterBoss>().targetLevelIndex = DungeonGenerator.instance.currentBiomeIndex;
        }
    }

}