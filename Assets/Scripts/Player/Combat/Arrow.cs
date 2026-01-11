using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class Arrow : MonoBehaviour
{
    // These values will be set by the PlayerAttack script when the arrow is fired
    public float damage;
    public float speed = 30f;
    public int maxBounces; 

    private int remainingBounces;

    private float fixedYPosition;

    private Rigidbody rb;
    private float lifeTime = 5f; // Arrow will destroy itself after 5 seconds if it hits nothing

    private PlayerStats ownerStats;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        //reset speed
        if( rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void Fire(float bulletSpeed, float bulletDamage, int bounceCount, PlayerStats owner = null)
    {
        speed = bulletSpeed;
        damage = bulletDamage;
        maxBounces = bounceCount;

        ownerStats = owner;

        remainingBounces = maxBounces;

        fixedYPosition = transform.position.y;

        //apply speed
        rb.velocity = transform.forward * speed;

        StartCoroutine(DeactivateAfterTime()); //timer until deactivation
    }

    void FixedUpdate()
    {
        if (transform.position.y != fixedYPosition)
        {
            transform.position = new Vector3(
                transform.position.x,
                fixedYPosition,
                transform.position.z
            );
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Projectile"))
        {
            return;
        }

        IDamageable damageableTarget = collision.gameObject.GetComponent<IDamageable>();

        bool isEnemyHit = (damageableTarget != null);

        if (isEnemyHit)
        {
            int dealt = (int)damage;
            damageableTarget.TakeDamage(dealt);
            if (ownerStats != null) ownerStats.ReportDamageDealt(dealt);
            Debug.Log("Arrow hit an enemy: " + collision.gameObject.name);

            // Bounce perk is meant for walls/obstacles, not enemies.
            BulletPool.Instance.ReturnBullet(gameObject);
            return;
        }
        else
        {
            Debug.Log("Arrow hit object: " + collision.gameObject.name);
        }

        if (remainingBounces > 0)
        {
            remainingBounces--;

            if (collision.contacts.Length > 0)
            {
                ContactPoint contact = collision.contacts[0];

                Vector3 normal = contact.normal;
                normal.y = 0;
                normal = normal.normalized;

                Vector3 incomingDir = transform.forward;
                incomingDir.y = 0;
                incomingDir = incomingDir.normalized;

                Vector3 reflectedDir = Vector3.Reflect(incomingDir, normal);
                reflectedDir.y = 0;
                reflectedDir = reflectedDir.normalized;

                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                transform.position = contact.point + normal * 0.1f;

                transform.rotation = Quaternion.LookRotation(reflectedDir);
                rb.velocity = reflectedDir * speed;

                return;
            }
        }

        BulletPool.Instance.ReturnBullet(gameObject);
    }

    private IEnumerator DeactivateAfterTime()
    {
        yield return new WaitForSeconds(lifeTime);
        BulletPool.Instance.ReturnBullet(gameObject); // return bullet
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }
}