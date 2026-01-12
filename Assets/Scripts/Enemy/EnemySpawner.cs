using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int numberOfEnemies = 10;
    public GameObject floor; // reference to floor to get bounds

    private Camera mainCamera;

    public float spawnDelay = 2f;

    void Start()
    {
        mainCamera = Camera.main;
        StartCoroutine(SpawnEnemiesSlowly());
    }

    System.Collections.IEnumerator SpawnEnemiesSlowly()
    {
        // spawning loop
        for (int i = 0; i < numberOfEnemies; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    void SpawnEnemy()
    {
        if (enemyPrefab == null || floor == null)
        {
            Debug.LogError("Enemy prefab or floor not assigned in the EnemySpawner script.");
            return;
        }

        Renderer floorRenderer = floor.GetComponent<Renderer>();
        if (floorRenderer == null)
        {
            Debug.LogError("Floor does not have a Renderer component.");
            return;
        }

        Bounds floorBounds = floorRenderer.bounds;

        Vector3 randomSpawnPoint;
        int attempts = 0;
        // trying to find a valid spot
        do
        {
            float randomX = Random.Range(floorBounds.min.x, floorBounds.max.x);
            float randomZ = Random.Range(floorBounds.min.z, floorBounds.max.z);
            float yPos = floorBounds.max.y + enemyPrefab.transform.localScale.y / 2;
            randomSpawnPoint = new Vector3(randomX, yPos, randomZ);
            attempts++;
        } while (IsVisibleByCamera(randomSpawnPoint) && attempts < 100);

        if (attempts < 100)
        {
            Instantiate(enemyPrefab, randomSpawnPoint, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("Could not find a valid spawn point outside the camera's view after 100 attempts.");
        }
    }

    // check if point is on screen
    bool IsVisibleByCamera(Vector3 position)
    {
        Vector3 viewportPoint = mainCamera.WorldToViewportPoint(position);
        return viewportPoint.x > 0 && viewportPoint.x < 1 && viewportPoint.y > 0 && viewportPoint.y < 1 && viewportPoint.z > 0;
    }
}