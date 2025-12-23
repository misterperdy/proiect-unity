using UnityEngine;
using UnityEngine.AI;

public class ShooterEnemy : MonoBehaviour, IDamageable
{
    [Header("Shooting Stats")]
    [Tooltip("Seconds between shots")]
    public float fireRateMultiplier = 1f;
    [Tooltip("Number of bullets per shot")]
    public int bulletsPerShot = 1;
    [Tooltip("Total angle of the spread")]
    public float spreadAngle = 15f;
    [Tooltip("Random angle offset error")]
    public int aimError = 0;
    
    [Header("Movement Stats")]
    public float viewRange = 20f;
    public float maintainDistance = 10f;
    public float moveSpeed = 3.5f;

    [Header("References")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("Loot")]
    public float lootMultiplier = 1f;
    public int projectileDamage = 10; // New variable to control bullet damage

    private NavMeshAgent agent;
    private Transform player;
    private float nextFireTime;
    private Animator acp;
    private bool isDead = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        acp = GetComponent<Animator>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        
        // If firePoint is not assigned, use the enemy's transform
        if (firePoint == null)
        {
            firePoint = transform;
        }
    }

    void Update()
    {
        if (player == null || isDead) return;

        // Update speed in case it was changed in Inspector
        if (agent != null)
        {
            agent.speed = moveSpeed;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Check if player is within view range
        if (distanceToPlayer <= viewRange)
        {
            acp.SetBool("isChasing", true);
            EngagePlayer(distanceToPlayer);
        }
        else
        {
            // Stop moving if player is out of range
            if (agent != null && agent.hasPath)
            {
                agent.ResetPath();
            }
        }
    }

    public void SetupEnemy(int hp, int dmg, float fireRateMult, int BPS, float spreadAng,
        int aimErr, Color color, float lootMult)
    {
        maxHealth = hp;
        currentHealth = hp;
        projectileDamage = dmg; // Pass this to the projectile later
        fireRateMultiplier = fireRateMult;
        bulletsPerShot = BPS;
        spreadAngle = spreadAng;
        lootMultiplier = lootMult;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers) r.material.color = color;
    }

    void EngagePlayer(float distance)
    {
        // Movement Logic: Maintain distance
        // If too far, move closer. If too close, back away? 
        // For simplicity and stability, we'll set destination to a point 'maintainDistance' away from player
        // along the line connecting them.
        
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        Vector3 targetPosition = player.position - (directionToPlayer * maintainDistance);

        agent.SetDestination(targetPosition);

        // Rotation: Always face the player when engaged
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(directionToPlayer.x, 0, directionToPlayer.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);

        // Shooting Logic
      
          acp.SetFloat("rangedAnimationSpeed", fireRateMultiplier);
          acp.SetTrigger("isRangedAttacking");


        //acp.ResetTrigger("isRangedAttacking");
    }

    public void Shoot()
    {
        projectilePrefab.GetComponent<EnemyProjectile>().damage = projectileDamage;

        if (projectilePrefab == null)
        {
            Debug.LogWarning("ShooterEnemy: Projectile Prefab is null!");
            return;
        }

        // Debug logs to diagnose shooting issues
        Debug.Log($"ShooterEnemy: Shooting! Rate: {fireRateMultiplier}, Bullets: {bulletsPerShot}, Spread: {spreadAngle}");

        // Calculate base rotation towards player
        Vector3 directionToPlayer = player.position - firePoint.position;
        directionToPlayer.y = 0;
        directionToPlayer.Normalize();
        Quaternion baseRotation = Quaternion.LookRotation(directionToPlayer);

        if (bulletsPerShot <= 1)
        {
            // Add a little aiming error if desired
            float error = Random.Range(-aimError, aimError);
            Quaternion finalRot = baseRotation * Quaternion.Euler(0, error, 0);

            SpawnProjectile(finalRot);
            return;
        }

        float totalArcAngle = spreadAngle * (bulletsPerShot - 1);

        float startAngle = -totalArcAngle / 2f;

        for (int i = 0; i < bulletsPerShot; i++)
        {
            // Calculate the fixed angle for this specific bullet index
            float currentAngleOffset = startAngle + (spreadAngle * i);

            // Add random jitter (aimError)
            float randomJitter = Random.Range(-aimError, aimError);
            float finalAngle = currentAngleOffset + randomJitter;

            // Apply rotation to the base direction
            Quaternion fireRotation = baseRotation * Quaternion.Euler(0, finalAngle, 0);

            SpawnProjectile(fireRotation);
        }
    }

    void SpawnProjectile(Quaternion rotation)
    {
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, rotation);

        // Ensure damage is passed correctly
        EnemyProjectile ep = proj.GetComponent<EnemyProjectile>();
        if (ep != null)
        {
            ep.damage = projectileDamage;
        }
    }

    // Visualization for debugging ranges
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRange);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, maintainDistance);
    }

    [Header("Health")]
    public int maxHealth = 50;
    public int currentHealth;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Enemy has died!");

        MinimapTracker tracker = GetComponent<MinimapTracker>();
        if (tracker != null)
        {
            tracker.TriggerDeathAnimation();
        }
        
        agent.isStopped = true;

        acp.ResetTrigger("isRangedAttacking");
        acp.SetBool("isChasing", false);
        acp.SetTrigger("isDead");
        StopAllCoroutines();

        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;
        isDead = true;
        Destroy(gameObject, 2f);
    }
}
