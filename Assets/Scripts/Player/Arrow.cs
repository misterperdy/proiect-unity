using UnityEngine;

public class Arrow : MonoBehaviour
{
    // These values will be set by the PlayerAttack script when the arrow is fired
    public float damage;
    public float speed = 30f;

    private Rigidbody rb;
    private float lifeTime = 5f; // Arrow will destroy itself after 5 seconds if it hits nothing

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Give the arrow its initial forward velocity
        rb.velocity = transform.forward * speed;

        // Destroy the arrow after a set time to clean up the scene
        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Try to get the EnemyAI component from the object we hit
        EnemyAI enemy = collision.gameObject.GetComponent<EnemyAI>();

        // If the object has an EnemyAI component, it's an enemy
        if (enemy != null)
        {
            // Deal damage
            enemy.TakeDamage((int)damage);
        }

        // Destroy the arrow as soon as it hits anything (enemy, wall, floor, etc.)
        Destroy(gameObject);
    }
}