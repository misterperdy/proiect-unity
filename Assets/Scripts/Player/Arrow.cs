using UnityEngine;
using System.Collections;

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
        
        if (collision.gameObject.tag != "Player" && collision.gameObject.tag != "Projectile") // so it doesnt collide with the player itself
        {
            Debug.Log("Arrow collided with: " + collision.gameObject.name);
            // Try to get the EnemyAI component from the object we hit
            EnemyAI enemy = collision.gameObject.GetComponent<EnemyAI>();
            DashBoss boss = collision.gameObject.GetComponent<DashBoss>();
            ShooterEnemy shooter = collision.gameObject.GetComponent<ShooterEnemy>();

            bool isEnemyHit = (enemy != null) || (boss != null) || (shooter != null);
            // If the object has an EnemyAI component, it's an enemy
            if (isEnemyHit)
            {
                if (enemy != null) enemy.TakeDamage((int)damage);
                if (boss != null) boss.TakeDamage((int)damage);
                if (shooter != null) shooter.TakeDamage((int)damage);

                if (remainingBounces > 0)
                {
                    remainingBounces--;

                    Vector3 currentVelocity = rb.velocity;
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

            //also check if it's explosive enemy
            KamikazeEnemyAI explosive = collision.gameObject.GetComponent<KamikazeEnemyAI>();
            if (explosive != null)
            {
                explosive.TakeDamage((int)damage);
            }

            // se the arrow as  returned soon as it hits anything (enemy, wall, floor, etc.)
            BulletPool.Instance.ReturnBullet(gameObject);
        }
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