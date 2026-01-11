using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleporterBoss : MonoBehaviour
{
    [Header("Settings")]
    public Transform player;
    public Transform destination; // Can be set via Inspector or Code
    public float teleportCooldown = 1f;

    public int targetLevelIndex = -1;

    private bool playerInZone = false;
    private bool isTeleporting = false;
    public void SetDestination(Vector3 pos)
    {
        // We create a temporary empty object to act as the transform target
        GameObject destObj = new GameObject("TeleportTarget_" + gameObject.name);
        destObj.transform.position = pos;
        destination = destObj.transform;
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    private void Update() // Changed from FixedUpdate for better Input response
    {
        // Removed PauseManager check for simplicity, add back if needed

        if (playerInZone && Input.GetKeyDown(KeyCode.F) && !isTeleporting) // Changed to F key or Input Button
        {
            if (destination != null)
            {
                StartCoroutine(TeleportPlayer());
            }
            else
            {
                Debug.LogWarning("Teleporter has no destination!");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = true;
            // You can add UI prompt here
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
        }
    }

    private IEnumerator TeleportPlayer()
    {
        isTeleporting = true;

        int indexToLoad = targetLevelIndex;

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlaySfx(MusicManager.Instance.bossTeleporterSfx);
        }

        if (gameObject.name.Contains("NextLevel"))
        {
            // The newest level is always the last one in the history list
            indexToLoad = DungeonGenerator.instance.levelHistory.Count - 1;
        }

        if (indexToLoad >= 0)
        {
            DungeonGenerator.instance.LoadLevelMap(indexToLoad);
        }

        // Disable Physics/Collisions briefly
        if (player != null)
        {
            if (targetLevelIndex >= 0 && DungeonGenerator.instance != null)
            {
                DungeonGenerator.instance.LoadLevelMapSimple(targetLevelIndex);
            }

            DungeonGenerator dungeon = DungeonGenerator.instance;

            if (dungeon != null)
            {

                int currentMapIndex = -1;
                for (int i = 0; i < dungeon.levelHistory.Count; i++)
                {
                    if (dungeon.levelHistory[i].worldOffset == dungeon.minimapController.currentLevelOffset)
                    {
                        currentMapIndex = i;
                        break;
                    }
                }


                if (targetLevelIndex >= 0)
                {
                    dungeon.LoadLevelMapSimple(targetLevelIndex);
                }
            }

            CharacterController cc = player.GetComponent<CharacterController>();
            Rigidbody rb = player.GetComponent<Rigidbody>();

            if (cc != null) cc.enabled = false;
            if (rb != null) rb.isKinematic = true;

            player.position = destination.position;

            yield return new WaitForSeconds(0.1f); // Short delay to let physics catch up

            if (cc != null) cc.enabled = true;
            if (rb != null) rb.isKinematic = false;
        }

        yield return new WaitForSeconds(teleportCooldown);
        isTeleporting = false;
    }
}