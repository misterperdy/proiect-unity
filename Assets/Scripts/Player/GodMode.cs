using UnityEngine;

public class GodMode : MonoBehaviour
{
    private PlayerHealth playerHealth;

    // start with false so we are not god immediately
    private bool isGodModeActive = false;

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
        // check if user pressed V to toggle god mode
        if (Input.GetKeyDown(KeyCode.V))
        {
            isGodModeActive = !isGodModeActive;
            Debug.Log("god mode is now: " + isGodModeActive);
        }

        if (playerHealth != null && isGodModeActive)
        {
            // forcing health to max every single frame
            // so player cant die
            playerHealth.currentHealth = playerHealth.maxHealth;

            // logic comment: usually this prevents death unless something deals more dmg than max hp in one hit
        }
    }
}