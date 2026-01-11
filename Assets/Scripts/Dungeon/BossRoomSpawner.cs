using UnityEngine;

public class BossRoomSpawner : MonoBehaviour
{
    private GameObject bossPrefab;
    private GameObject activeBoss;
    private bool hasSpawned = false;
    private bool levelGenerated = false;
    private BoxCollider triggerCollider;

    private GameObject exitTeleporter;

    public void Initialize(GameObject bossToSpawn, int roomWidth, int roomHeight, float tileSize)
    {
        bossPrefab = bossToSpawn;
        triggerCollider = gameObject.AddComponent<BoxCollider>();
        triggerCollider.isTrigger = true;

        float sizeX = (roomWidth * tileSize) - 2.0f;
        float sizeZ = (roomHeight * tileSize) - 2.0f;
        triggerCollider.size = new Vector3(sizeX, 10f, sizeZ);
        triggerCollider.center = new Vector3(0, 5f, 0);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasSpawned && other.CompareTag("Player"))
        {
            SpawnBoss();
        }
    }

    void SpawnBoss()
    {
        if (bossPrefab == null) return;
        hasSpawned = true;

        Vector3 spawnPos = transform.position + new Vector3(0, 0.2f, 0); //CHANGED HEIGHT WHERE IT SPAWNS
        activeBoss = Instantiate(bossPrefab, spawnPos, Quaternion.identity);

        Destroy(triggerCollider);
    }
    void Update()
    {
        // Logic: Boss was spawned, but is now null (destroyed/died), and we haven't made the portal yet.
        if (hasSpawned && activeBoss == null && !levelGenerated)
        {
            SpawnExitTeleporter();
        }
    }

    void SpawnExitTeleporter()
    {
        levelGenerated = true;
        Debug.Log("Boss Dead. Spawning Portal...");

        if (DungeonGenerator.instance != null && DungeonGenerator.instance.teleporterPrefab != null)
        {
            Vector3 portalPos = transform.position + new Vector3(0f,0.3f,0f);
            exitTeleporter = Instantiate(DungeonGenerator.instance.teleporterPrefab, portalPos, Quaternion.identity);
            exitTeleporter.name = "Teleporter_NextLevel";

            if(DungeonGenerator.instance.medkitPrefab != null)
            {
                //spawn a  medkit above teleporter
                GameObject medkit = Instantiate(DungeonGenerator.instance.medkitPrefab, portalPos + new Vector3(0, 0.28f, 7f), Quaternion.Euler(0,90,0));
                medkit.name = "Medkit";
            }

            Vector3 nextLevelStart = DungeonGenerator.instance.GenerateNextLevel(portalPos, false);

            exitTeleporter.GetComponent<TeleporterBoss>().SetDestination(nextLevelStart);
            exitTeleporter.GetComponent<TeleporterBoss>().targetLevelIndex = DungeonGenerator.instance.currentBiomeIndex;
        }
    }

}