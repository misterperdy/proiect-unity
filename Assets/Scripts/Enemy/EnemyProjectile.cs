using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public int damage = 10;
    public float speed = 10f;
    public float lifeTime = 5f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        // Ensure we have a Rigidbody for movement
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false; // Usually bullets don't drop immediately
        }
        
        // Destroy after lifetime
        Destroy(gameObject, lifeTime);
        
        // Apply initial velocity
        rb.velocity = transform.forward * speed;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if we hit an enemy (friendly fire prevention)
        // We use Layer check because the user specified they use the "Enemy" layer.
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1 && collision.gameObject.layer == enemyLayer) return;

        // Also check for ShooterEnemy component just in case
        if (collision.gameObject.GetComponent<ShooterEnemy>() != null) return;

        // Ignore other projectiles to prevent self-collision when shooting multiple bullets
        if (collision.gameObject.GetComponent<EnemyProjectile>() != null) return;

        // Check for player
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }

        // Destroy on impact with anything else (walls, etc.)
        Destroy(gameObject);
    }
}
