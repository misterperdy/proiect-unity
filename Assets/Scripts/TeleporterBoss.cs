using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TeleporterBoss : MonoBehaviour
{
    [Header("Settings")]
    public Transform player;
    public Transform destination; // this is where the player will land
    public float teleportCooldown = 1f;

    public int targetLevelIndex = -1; // id for the level we want to load

    private bool playerInZone = false; // check if player is inside the trigger
    private bool isTeleporting = false; // flag to prevent double teleport

    public bool endGameTeleporter = false; // if true, this ends the game
    public string sceneToLoad = "WinScene";

    public void SetDestination(Vector3 pos)
    {
        // create a temporary empty object just to hold the target position
        // so we dont lose the coordinates
        GameObject destObj = new GameObject("TeleportTarget_" + gameObject.name);
        destObj.transform.position = pos;
        destination = destObj.transform;
    }

    private void Start()
    {
        // auto find player if i forgot to set it in inspector
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    private void Update() // checking inputs here
    {
        // verify if this is a normal teleporter or the winning one
        if (!endGameTeleporter)
        {
            // if player is close, pressed F and is not already moving
            if (playerInZone && Input.GetKeyDown(KeyCode.F) && !isTeleporting) // using F key for interaction
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
        else if (playerInZone && Input.GetKeyDown(KeyCode.F))
        {
            // load the win scene directly
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // detect when player walks into the teleporter area
        if (other.CompareTag("Player"))
        {
            playerInZone = true;
            // TODO: show "Press F" text on screen here
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // detect when player leaves the area
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
        }
    }

    private IEnumerator TeleportPlayer()
    {
        isTeleporting = true; // lock the teleport so we dont spam it

        int indexToLoad = targetLevelIndex;

        // play sound effect if the music manager exists
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlaySfx(MusicManager.Instance.bossTeleporterSfx);
        }

        // special check if the object name says nextlevel
        if (gameObject.name.Contains("NextLevel"))
        {
            // take the last level from the list because its the new one
            indexToLoad = DungeonGenerator.instance.levelHistory.Count - 1;
        }

        // tell the generator to prepare the map data
        if (indexToLoad >= 0)
        {
            DungeonGenerator.instance.LoadLevelMap(indexToLoad);
        }

        // disable physics on player so he doesnt glitch through walls
        if (player != null)
        {
            // double check for dungeon generator existence
            if (targetLevelIndex >= 0 && DungeonGenerator.instance != null)
            {
                DungeonGenerator.instance.LoadLevelMapSimple(targetLevelIndex);
            }

            DungeonGenerator dungeon = DungeonGenerator.instance;

            if (dungeon != null)
            {
                // find the current map index in the list
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

            // get the components to disable them
            CharacterController cc = player.GetComponent<CharacterController>();
            Rigidbody rb = player.GetComponent<Rigidbody>();

            if (cc != null) cc.enabled = false; // turn off controller
            if (rb != null) rb.isKinematic = true; // stop physics calculations

            // actually move the player to new position
            player.position = destination.position;

            yield return new WaitForSeconds(0.1f); // wait a small delay for unity to process position

            // turn everything back on
            if (cc != null) cc.enabled = true;
            if (rb != null) rb.isKinematic = false;
        }

        yield return new WaitForSeconds(teleportCooldown);
        isTeleporting = false; // unlock teleport ability
    }
}