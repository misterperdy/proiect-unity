using UnityEngine;

public class Medkit : MonoBehaviour
{
    public int healAmount = 25;

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth player = other.GetComponent<PlayerHealth>();

        if (player != null && player.currentHealth < player.maxHealth)
        {
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.PlaySfx(MusicManager.Instance.playerMedkitHealingSfx);
            }

            player.Heal(healAmount);
            Destroy(gameObject); 
        }
    }
}
