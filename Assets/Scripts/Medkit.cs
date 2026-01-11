using UnityEngine;

public class Medkit : MonoBehaviour
{
    public float healAmount = 0.25f;

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth player = other.GetComponent<PlayerHealth>();

        if (player != null && player.currentHealth < player.maxHealth)
        {
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.PlaySfx(MusicManager.Instance.playerMedkitHealingSfx);
            }

            player.HealPercent(healAmount);
            Destroy(gameObject); 
        }
    }
}
