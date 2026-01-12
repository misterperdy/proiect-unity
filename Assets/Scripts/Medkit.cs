using UnityEngine;

public class Medkit : MonoBehaviour
{
    public float healAmount = 0.25f; // how much hp to give back (percentage usually)

    private void OnTriggerEnter(Collider other)
    {
        // try to get health script from the object that touched the medkit
        PlayerHealth player = other.GetComponent<PlayerHealth>();

        // check if player exists and is actually hurt
        if (player != null && player.currentHealth < player.maxHealth)
        {
            // play the healing sound if manager exists
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.PlaySfx(MusicManager.Instance.playerMedkitHealingSfx);
            }

            // apply the heal
            player.HealPercent(healAmount);
            Destroy(gameObject); // remove the medkit from the world so it cant be used again
        }
    }
}