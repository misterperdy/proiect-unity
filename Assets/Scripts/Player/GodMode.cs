using UnityEngine;

public class GodMode : MonoBehaviour
{
    private PlayerHealth playerHealth;

    void Start()
    {
        // getting the health component
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
            // forcing health to max every single frame
            // so player cant die
            playerHealth.currentHealth = playerHealth.maxHealth;

            // logic comment: usually this prevents death unless something deals more dmg than max hp in one hit
        }
    }
}