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
            rb.isKinematic = true; // Use Kinematic for triggers so they don't get pushed by physics
        }
        
        // Destroy after lifetime
        Destroy(gameObject, lifeTime);
        
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        // Ignore Self and other Projectiles
        if (other.GetComponent<EnemyProjectile>() != null) return;
        if (other.GetComponent<ShooterEnemy>() != null) return;

        // Friendly Fire Check (Layer)
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1 && other.gameObject.layer == enemyLayer) return;

        // Check for player
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
            Destroy(gameObject);
        }

        // Destroy on walls 
        else if (other.gameObject.layer == LayerMask.NameToLayer("Default") || other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}
