using UnityEngine;
using System.Collections;

public class Arrow : MonoBehaviour
{
    // These values will be set by the PlayerAttack script when the arrow is fired
    public float damage;
    public float speed = 30f;

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

    public void Fire(float bulletSpeed, float bulletDamage)
    {
        speed = bulletSpeed;
        damage = bulletDamage;

        //apply speed
        rb.velocity = transform.forward * speed;

        StartCoroutine(DeactivateAfterTime()); //timer until deactivation
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag != "Player") // so it doesnt collide with the player itself
        {
            // Try to get the EnemyAI component from the object we hit
            EnemyAI enemy = collision.gameObject.GetComponent<EnemyAI>();
            DashBoss dashBoss = collision.gameObject.GetComponent<DashBoss>();
            SlimeBoss slimeBoss = collision.gameObject.GetComponent<SlimeBoss>();
            ShooterEnemy shooter = collision.gameObject.GetComponent<ShooterEnemy>();

            bool isEnemyHit = (enemy != null) || (dashBoss != null) || (slimeBoss != null) || (shooter != null);
            // If the object has an EnemyAI component, it's an enemy
            if (enemy != null)
            {
                if (enemy != null) enemy.TakeDamage((int)damage);
            }

            if (dashBoss != null) dashBoss.TakeDamage((int)damage);
            if (slimeBoss != null) slimeBoss.TakeDamage((int)damage);

            if (shooter != null)
            {
                shooter.TakeDamage((int)damage);
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