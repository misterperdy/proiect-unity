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

    private readonly System.Collections.Generic.HashSet<int> _damagedTargets = new System.Collections.Generic.HashSet<int>();

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
        _damagedTargets.Clear();

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

        if (TryGetDamageable(collision.collider, out IDamageable damageableTarget, out Component damageableComponent))
        {
            int id = damageableComponent.GetInstanceID();
            if (!_damagedTargets.Contains(id))
            {
                _damagedTargets.Add(id);

                int dealt = (int)damage;
                damageableTarget.TakeDamage(dealt);
                if (ownerStats != null) ownerStats.ReportDamageDealt(dealt);
            }

            Debug.Log("Arrow hit an enemy: " + collision.gameObject.name);

            // New behavior: if we still have bounces, bounce off enemies too.
            if (remainingBounces > 0 && collision.contacts.Length > 0)
            {
                remainingBounces--;
                if (TryBounceFromContact(collision.contacts[0])) return;
            }

            BulletPool.Instance.ReturnBullet(gameObject);
            return;
        }

        Debug.Log("Arrow hit object: " + collision.gameObject.name);

        if (remainingBounces > 0 && collision.contacts.Length > 0)
        {
            remainingBounces--;
            if (TryBounceFromContact(collision.contacts[0])) return;
        }

        BulletPool.Instance.ReturnBullet(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (other.CompareTag("Player") || other.CompareTag("Projectile")) return;

        if (TryGetDamageable(other, out IDamageable damageableTarget, out Component damageableComponent))
        {
            int id = damageableComponent.GetInstanceID();
            if (!_damagedTargets.Contains(id))
            {
                _damagedTargets.Add(id);

                int dealt = (int)damage;
                damageableTarget.TakeDamage(dealt);
                if (ownerStats != null) ownerStats.ReportDamageDealt(dealt);
            }

            Debug.Log("Arrow hit an enemy (trigger): " + other.gameObject.name);

            if (remainingBounces > 0)
            {
                remainingBounces--;
                if (TryBounceFromTrigger(other)) return;
            }

            BulletPool.Instance.ReturnBullet(gameObject);
        }
    }

    private bool TryBounceFromTrigger(Collider other)
    {
        if (rb == null) return false;

        Vector3 closest = other.ClosestPoint(transform.position);
        Vector3 normal = (transform.position - closest);
        normal.y = 0f;
        if (normal.sqrMagnitude < 0.0001f)
        {
            normal = -transform.forward;
            normal.y = 0f;
        }
        normal = normal.normalized;

        Vector3 incomingDir = rb.velocity.sqrMagnitude > 0.001f ? rb.velocity.normalized : transform.forward;
        incomingDir.y = 0f;
        incomingDir = incomingDir.normalized;

        Vector3 reflectedDir = Vector3.Reflect(incomingDir, normal);
        reflectedDir.y = 0f;
        reflectedDir = reflectedDir.normalized;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = closest + normal * 0.1f;
        transform.rotation = Quaternion.LookRotation(reflectedDir);
        rb.velocity = reflectedDir * speed;
        return true;
    }

    private bool TryBounceFromContact(ContactPoint contact)
    {
        Vector3 normal = contact.normal;
        normal.y = 0;
        if (normal.sqrMagnitude < 0.0001f) return false;
        normal = normal.normalized;

        Vector3 incomingDir = (rb != null && rb.velocity.sqrMagnitude > 0.001f) ? rb.velocity.normalized : transform.forward;
        incomingDir.y = 0;
        incomingDir = incomingDir.normalized;

        Vector3 reflectedDir = Vector3.Reflect(incomingDir, normal);
        reflectedDir.y = 0;
        if (reflectedDir.sqrMagnitude < 0.0001f) return false;
        reflectedDir = reflectedDir.normalized;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = contact.point + normal * 0.1f;
        transform.rotation = Quaternion.LookRotation(reflectedDir);
        if (rb != null) rb.velocity = reflectedDir * speed;
        return true;
    }

    private static bool TryGetDamageable(Collider hit, out IDamageable damageable, out Component damageableComponent)
    {
        damageable = null;
        damageableComponent = null;
        if (hit == null) return false;

        // Unity can sometimes miss interface GetComponent on child colliders; use known types in parents.
        EnemyAI enemy = hit.GetComponentInParent<EnemyAI>();
        if (enemy != null) { damageable = enemy; damageableComponent = enemy; return true; }

        ShooterEnemy shooter = hit.GetComponentInParent<ShooterEnemy>();
        if (shooter != null) { damageable = shooter; damageableComponent = shooter; return true; }

        KamikazeEnemyAI kamikaze = hit.GetComponentInParent<KamikazeEnemyAI>();
        if (kamikaze != null) { damageable = kamikaze; damageableComponent = kamikaze; return true; }

        SlimeBoss slimeBoss = hit.GetComponentInParent<SlimeBoss>();
        if (slimeBoss != null) { damageable = slimeBoss; damageableComponent = slimeBoss; return true; }

        LichBoss lichBoss = hit.GetComponentInParent<LichBoss>();
        if (lichBoss != null) { damageable = lichBoss; damageableComponent = lichBoss; return true; }

        DashBoss dashBoss = hit.GetComponentInParent<DashBoss>();
        if (dashBoss != null) { damageable = dashBoss; damageableComponent = dashBoss; return true; }

        return false;
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