using UnityEngine;

public class Medkit : MonoBehaviour
{
    public int healAmount = 25;

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth player = other.GetComponent<PlayerHealth>();

        if (player != null)
        {
            player.Heal(healAmount);
            Destroy(gameObject); // remove medkit after use
        }
    }
}
