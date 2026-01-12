using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class EnemyAI : MonoBehaviour, IDamageable
{
    public int maxHealth = 50;
    public int currentHealth;
    public int damage = 10;
    public float attackRange = 1.5f;
    public float sightRange = 20f;
    public float patrolRadius = 10f;
    public float attackSpeed = 1f;
    public float moveSpeed = 3.5f;
    public float medkitDropChance = 10f; // drop chance percent

    private float lastDamageSfxTime = -999f;
    private const float damageSfxMinInterval = 0.08f;


    private NavMeshAgent agent;
    private Transform player;
    private PlayerHealth playerHealth;
    private float lastAttackTime = 0f;
    private Vector3 lastKnownPlayerPosition;
    private bool isDead = false;
    private int randomNumber;

    public GameObject xpOrbPrefab;

    [Header("Loot")]
    public float lootMultiplier = 1f;

    [Header("Hit Effect")]
    public GameObject hitParticles;
    public GameObject skeletonMat;
    public GameObject skeletonRibMat;
    public float fadeTime = 0.01f; // speed of flashing white
    public Color32 hitColor = new Color32(255, 0, 0, 255);
    public string rarity;

    private enum AIState { Patrolling, Chasing, Attacking, Searching }
    private AIState currentState;
    private Animator acp;
    private Material hitMat;
    private Material ribHitMat;
    private Color32 originalColor = new Color32(255, 255, 255, 0);

    void Start()
    {
        // coloring based on rarity
        if (rarity != null)
        {
            if (rarity == "Magic")
            {
                originalColor = new Color32(0, 85, 255, 0);
            }
            else if (rarity == "Rare")
            {
                originalColor = new Color32(215, 224, 39, 0);
            }
            else
            {
                originalColor = new Color32(255, 255, 255, 0);
            }
        }

        currentHealth = maxHealth;
        randomNumber = Random.Range(1, 3); // random attack animation
        agent = GetComponent<NavMeshAgent>();
        acp = GetComponent<Animator>();
        agent.speed = moveSpeed;

        hitMat = skeletonMat.GetComponent<SkinnedMeshRenderer>().material;
        ribHitMat = skeletonRibMat.GetComponent<MeshRenderer>().material;

        // finding player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null && acp == null)
        {
            Debug.LogError("No GameObject with tag 'Player' found in the scene. EnemyAI requires a Player to function.");
            Destroy(gameObject);
            return;
        }
        player = playerObj.transform;
        playerHealth = playerObj.GetComponent<PlayerHealth>();

        // warp to navmesh if spawned off-grid
        if (!agent.isOnNavMesh)
        {
            NavMeshHit closestHit;
            if (NavMesh.SamplePosition(transform.position, out closestHit, 100f, NavMesh.AllAreas))
            {
                agent.Warp(closestHit.position);
            }
            else
            {
                Debug.LogError("Could not find a valid position on the NavMesh for the enemy.");
                Destroy(gameObject);
                return;
            }
        }

        StartCoroutine(InitializeAI());
    }

    public void SetupEnemy(int hp, int dmg, float attSpeed, float spd, Color color, float lootMult, string rrty)
    {
        // applying stats from spawner
        maxHealth = hp;
        currentHealth = hp;
        damage = dmg;
        attackSpeed = attSpeed;
        lootMultiplier = lootMult;
        moveSpeed = spd;
        rarity = rrty;

        // applying visual color
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            r.material.color = color;
        }
    }

    System.Collections.IEnumerator InitializeAI()
    {
        // wait frame just in case
        yield return new WaitForEndOfFrame();

        currentState = AIState.Patrolling;
        SetRandomPatrolDestination();
    }

    void Update()
    {
        if (isDead) return;

        // animate running
        bool isMoving = agent.velocity.magnitude > 0.1f;
        acp.SetBool("isChasing", isMoving);

        // state machine loop
        switch (currentState)
        {
            case AIState.Patrolling:
                Patrol();
                break;
            case AIState.Chasing:
                Chase();
                break;
            case AIState.Attacking:
                Attack();
                break;
            case AIState.Searching:
                Search();
                break;
        }
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > sightRange) return false;

        Vector3 origin = transform.position + Vector3.up;

        Vector3 target = player.position + Vector3.up;

        Vector3 directionToPlayer = (target - origin).normalized;

        // raycast to check walls
        RaycastHit hit;
        if (Physics.Raycast(origin, directionToPlayer, out hit, sightRange))
        {
            if (hit.transform == player)
            {
                return true;
            }
        }
        return false;
    }

    void Patrol()
    {
        if (CanSeePlayer())
        {
            currentState = AIState.Chasing;
            return;
        }

        // if arrived at destination
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            SetRandomPatrolDestination();
        }
    }

    void Chase()
    {
        if (!CanSeePlayer())
        {
            currentState = AIState.Searching;
            lastKnownPlayerPosition = player.position;
            agent.SetDestination(lastKnownPlayerPosition);
            return;
        }

        acp.SetBool("isChasing", true);
        agent.SetDestination(player.position);

        if (Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            currentState = AIState.Attacking;
        }
    }

    void Attack()
    {
        agent.isStopped = true;

        if (Vector3.Distance(transform.position, player.position) > attackRange)
        {
            acp.ResetTrigger("isMeleeAttacking" + randomNumber);
            currentState = AIState.Chasing;
            agent.isStopped = false;
            return;
        }

        // setting animation speed based on attack speed
        acp.SetFloat("meleeAnimationSpeed", attackSpeed);
        acp.SetTrigger("isMeleeAttacking" + randomNumber);
        acp.SetBool("isChasing", false);

        Vector3 targetPosition = player.position;

        // keep looking at player but flat
        targetPosition.y = transform.position.y;

        transform.LookAt(targetPosition);

    }

    public void PlayerTakeDamage()
    {
        // called via animation event usually
        if (Time.time > lastAttackTime)
        {
            if (playerHealth != null)
            {
                if (Vector3.Distance(transform.position, player.position) <= attackRange)
                {
                    if (MusicManager.Instance != null)
                    {
                        MusicManager.Instance.PlaySpatialSfx(MusicManager.Instance.enemySwordSwingSfx, transform.position, 1f, 2f, 25f);
                    }
                    playerHealth.TakeDamage(damage);
                }
            }
            lastAttackTime = Time.time;
        }
    }

    void Search()
    {
        if (CanSeePlayer())
        {
            currentState = AIState.Chasing;
            return;
        }

        // gave up searching
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentState = AIState.Patrolling;
            SetRandomPatrolDestination();
        }
    }

    void SetRandomPatrolDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += transform.position;
        NavMeshHit hit;
        // finding random valid point on navmesh
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, 1))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            Debug.LogWarning("EnemyAI: Failed to find a valid NavMesh position for patrol destination.");
        }
    }

    public void TakeDamage(int amount)
    {
        StartCoroutine(SetHitEffect());
        StartCoroutine(SetHitParticles());

        currentHealth -= amount;

        // play hurt sound
        if (MusicManager.Instance != null && Time.time - lastDamageSfxTime >= damageSfxMinInterval)
        {
            string n = gameObject.name.ToLower();
            AudioClip clip = n.Contains("slime") ? MusicManager.Instance.slimeEnemyTookDamageSfx : MusicManager.Instance.skeletonTookDamageSfx;
            MusicManager.Instance.PlaySpatialSfx(clip, transform.position, 1f, 2f, 25f);
            lastDamageSfxTime = Time.time;
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator SetHitEffect()
    {
        // flashing red/white
        skeletonMat.GetComponent<SkinnedMeshRenderer>().material = hitMat;
        skeletonRibMat.GetComponent<MeshRenderer>().material = ribHitMat;
        hitMat.color = hitColor;
        ribHitMat.color = hitColor;

        while (hitMat.color != originalColor)
        {

            hitMat.color = Color.Lerp(hitMat.color, originalColor, fadeTime);
            ribHitMat.color = Color.Lerp(ribHitMat.color, originalColor, fadeTime);

            yield return null;
        }
    }

    private IEnumerator SetHitParticles()
    {
        hitParticles.SetActive(true);

        yield return new WaitForSeconds(0.1f);

        hitParticles.SetActive(false);

    }

    void Die()
    {
        Debug.Log("Enemy has died!");

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlaySpatialSfx(MusicManager.Instance.enemyDiesSfx, transform.position, 1f, 2f, 25f);
        }

        MinimapTracker tracker = GetComponent<MinimapTracker>();
        if (tracker != null)
        {
            tracker.TriggerDeathAnimation();
        }

        // dropping medkit maybe
        if (DungeonGenerator.instance != null)
            if (DungeonGenerator.instance.medkitPrefab != null)
            {
                float randomValue = Random.Range(0f, 100f);
                if (randomValue <= medkitDropChance)
                {
                    Vector3 pos = transform.position + new Vector3(0f, 0.3f, 0f);
                    GameObject medkit = Instantiate(DungeonGenerator.instance.medkitPrefab, pos + new Vector3(0, 0.28f, 2.5f), Quaternion.Euler(0, 90, 0));
                }
            }

        // dropping exp
        if (xpOrbPrefab != null)
        {
            int xpToDrop = Mathf.RoundToInt(1 * lootMultiplier);

            GameObject orb = Instantiate(xpOrbPrefab, transform.position + Vector3.up, Quaternion.identity);
            orb.GetComponent<XPOrb>().Initialize(xpToDrop);
        }

        agent.isStopped = true;

        acp.ResetTrigger("isMeleeAttacking" + randomNumber);
        acp.SetBool("isChasing", false);
        acp.SetTrigger("isDead");

        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;
        isDead = true;
        // waiting for animation to finish before destroying
        Destroy(gameObject, 2f);
    }
}