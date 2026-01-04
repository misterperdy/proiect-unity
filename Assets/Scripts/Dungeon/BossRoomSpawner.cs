using UnityEngine;

public class BossRoomSpawner : MonoBehaviour
{
    private GameObject bossPrefab;
    private bool hasSpawned = false;
    private BoxCollider triggerCollider;

    public void Initialize(GameObject bossToSpawn, int roomWidth, int roomHeight, float tileSize)
    {
        bossPrefab = bossToSpawn;

        // Create a Trigger that covers the room
        triggerCollider = gameObject.AddComponent<BoxCollider>();
        triggerCollider.isTrigger = true;

        // Calculate size based on your 10x10 room settings
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
        Debug.Log("BOSS FIGHT STARTED!");

        // Spawn in the exact center of the room object
        // Add slight Y offset so it doesn't clip into floor
        Vector3 spawnPos = transform.position + new Vector3(0, 2f, 0);

        Instantiate(bossPrefab, spawnPos, Quaternion.identity);

        Destroy(triggerCollider); // Disable trigger
    }
}