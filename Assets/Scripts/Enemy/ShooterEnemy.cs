using UnityEngine;
using UnityEngine.AI;

public class ShooterEnemy : MonoBehaviour
{
    [Header("Shooting Stats")]
    [Tooltip("Seconds between shots")]
    public float fireRateMultiplier = 3f;
    [Tooltip("Number of bullets per shot")]
    public int bulletsPerShot = 1;
    [Tooltip("Total angle of the spread")]
    public float spreadAngle = 15f;
    [Tooltip("Random angle offset error")]
    public int aimError = 5;
    
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
        Vector3 directionToPlayer = (player.position - firePoint.position).normalized;
        Quaternion baseRotation = Quaternion.LookRotation(directionToPlayer);

        // Calculate starting angle for the spread
        // If 1 bullet, angle is 0.
        // If multiple, we center the spread.
        float startAngle = 0;
        float angleStep = 0;

        if (bulletsPerShot > 1)
        {
            startAngle = -spreadAngle / 2f;
            angleStep = spreadAngle / (bulletsPerShot - 1);
        }

        for (int i = 0; i < bulletsPerShot; i++)
        {
            // Calculate spread offset
            float currentSpread = 0;
            if (bulletsPerShot > 1)
            {
                currentSpread = startAngle + (angleStep * i);
            }

            // Add random error
            float randomError = Random.Range(-aimError, aimError);
            float finalAngle = currentSpread + randomError;

            // Apply rotation
            Quaternion fireRotation = baseRotation * Quaternion.Euler(0, finalAngle, 0);

            // Instantiate projectile
            Instantiate(projectilePrefab, firePoint.position, fireRotation);
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
