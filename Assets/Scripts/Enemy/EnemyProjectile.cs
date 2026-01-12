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
        // safety check for rigidbody
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false; // bullets fly straight
            rb.isKinematic = true;
        }

        // auto delete after x seconds
        Destroy(gameObject, lifeTime);

    }

    void Update()
    {
        // move forward
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        // ignore other bullets and shooter
        if (other.GetComponent<EnemyProjectile>() != null) return;
        if (other.GetComponent<ShooterEnemy>() != null) return;

        // friendly fire check
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1 && other.gameObject.layer == enemyLayer) return;

        // hit player
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
            Destroy(gameObject);
        }

        // hit walls
        else if (other.gameObject.layer == LayerMask.NameToLayer("Default") || other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}