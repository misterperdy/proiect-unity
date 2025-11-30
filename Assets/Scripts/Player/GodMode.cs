using UnityEngine;

public class GodMode : MonoBehaviour
{
    private PlayerHealth playerHealth;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("GodMode script requires a PlayerHealth component on the same object.");
            enabled = false;
        }
    }

    void Update()
    {
        if (playerHealth != null)
        {
            // Keep health at max every frame
            playerHealth.currentHealth = playerHealth.maxHealth;
            
            // Optional: Ensure not dead
            // If PlayerHealth has an 'isDead' flag that's public, we could reset it, 
            // but PlayerHealth.isDead is private. 
            // Preventing damage by keeping health full is usually enough unless one-shot kill logic exists.
        }
    }
}
