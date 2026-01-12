using UnityEngine;

public class DamagingArea : MonoBehaviour
{
    public int damage = 10;
    public float duration = 10f; // how long puddle stays alive
    public float damageInterval = 1f; // cooldown for damage

    private float destroyTimer;

    void Start()
    {
        destroyTimer = duration;
    }

    void Update()
    {
        // countdown to destroy
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
            // getting player health script
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
        }
    }
}