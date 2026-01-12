using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Debug_TeleportToBoss : MonoBehaviour
{
    public KeyCode teleportKey = KeyCode.B; // key to press for cheat
    void Update()
    {
        // checking for input every frame
        if (Input.GetKeyDown(teleportKey) && Input.GetKey(KeyCode.LeftControl))
        {
            JumpToBoss();
        }
    }

    void JumpToBoss()
    {
        // safety check if dungeon generator exists
        if (DungeonGenerator.instance == null)
        {
            return;
        }

        // getting the boss location from the generator script
        Vector3 bossPos = DungeonGenerator.instance.currentBossPosition;

        // if boss pos is zero it means boss room isnt spawned yet
        if (bossPos == Vector3.zero)
        {
            return;
        }

        // getting reference to player transform
        Transform player = DungeonGenerator.instance.player;
        if (player == null) return;


        // calcluating a safe spot slightly above boss position
        Vector3 safeLocation = bossPos + new Vector3(0, 2.0f, 0);

        // move player instantly
        player.position = safeLocation;

        // stop any movement physics so player doesnt keep flying
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.position = safeLocation;
        }
    }
}