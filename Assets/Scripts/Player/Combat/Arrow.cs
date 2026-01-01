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

    public void Fire(float bulletSpeed, float bulletDamage, int bounceCount)
    {
        speed = bulletSpeed;
        damage = bulletDamage;
        maxBounces = bounceCount;

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
            damageableTarget.TakeDamage((int)damage);
            Debug.Log("Arrow hit an enemy: " + collision.gameObject.name);
        }
        else
        {
            Debug.Log("Arrow hit object: " + collision.gameObject.name);
        }

        if (remainingBounces > 0 && isEnemyHit)
        {
            remainingBounces--;

            Vector3 currentVelocity = rb.velocity;

            if (collision.contacts.Length > 0)
            {
                Vector3 surfaceNormal = collision.contacts[0].normal;

                surfaceNormal.y = 0f;
                surfaceNormal.Normalize();

                Vector3 reflectedDirection = Vector3.Reflect(currentVelocity.normalized, surfaceNormal);
                reflectedDirection.y = 0f;
                reflectedDirection.Normalize();

                rb.velocity = reflectedDirection * currentVelocity.magnitude;
                transform.rotation = Quaternion.LookRotation(reflectedDirection);

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