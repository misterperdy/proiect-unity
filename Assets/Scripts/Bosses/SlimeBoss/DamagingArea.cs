using UnityEngine;

public class DamagingArea : MonoBehaviour
{
    public int damage = 10;
    public float duration = 10f; // How long the puddle stays
    public float damageInterval = 1f; // Take damage every 1 second

    private float destroyTimer;

    void Start()
    {
        destroyTimer = duration;
    }

    void Update()
    {
        destroyTimer -= Time.deltaTime;
        if (destroyTimer <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Assuming you have the PlayerHealth script from before
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
        }
    }
}