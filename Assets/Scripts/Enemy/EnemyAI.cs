using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public int maxHealth = 50;
    public int currentHealth;
    public int damage = 10;
    public float attackRange = 1.5f;
    public float sightRange = 20f;
    public float patrolRadius = 10f;
    public float attackSpeed = 1f;
    public float moveSpeed = 3.5f;


    private NavMeshAgent agent;
    private Transform player;
    private PlayerHealth playerHealth;
    private float lastAttackTime = 0f;
    private Vector3 lastKnownPlayerPosition;
    private bool isDead = false;
    private int randomNumber;

    [Header("Loot")]
    public float lootMultiplier = 1f;

    private enum AIState { Patrolling, Chasing, Attacking, Searching }
    private AIState currentState;
    private Animator acp;

    void Start()
    {
        currentHealth = maxHealth;
        randomNumber = Random.Range(1, 3);
        agent = GetComponent<NavMeshAgent>();
        acp = GetComponent<Animator>();
        agent.speed = moveSpeed;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null && acp == null)
        {
            Debug.LogError("No GameObject with tag 'Player' found in the scene. EnemyAI requires a Player to function.");
            Destroy(gameObject);
            return;
        }
        player = playerObj.transform;
        playerHealth = playerObj.GetComponent<PlayerHealth>();

        // Ensure the agent is on the NavMesh
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

    public void SetupEnemy(int hp, int dmg, float attSpeed, float spd, Color color, float lootMult)
    {
        maxHealth = hp;
        currentHealth = hp;
        damage = dmg;
        attackSpeed = attSpeed;
        lootMultiplier = lootMult;
        moveSpeed = spd;

        // Change color of the mesh
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            r.material.color = color;
        }
    }

    System.Collections.IEnumerator InitializeAI()
    {
        // Wait for the end of the frame to ensure the agent is fully initialized
        yield return new WaitForEndOfFrame();

        currentState = AIState.Patrolling;
        SetRandomPatrolDestination();
    }

    void Update()
    {
        if (isDead) return;
        switch (currentState)
        {
            case AIState.Patrolling:
                Patrol();
                break;
            case AIState.Chasing:
                acp.SetBool("isChasing", true);
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

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        RaycastHit hit;
        if (Physics.Raycast(transform.position, directionToPlayer, out hit, sightRange) && hit.transform == player)
        {
            return true;
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
            currentState = AIState.Chasing;
            agent.isStopped = false;
            return;
        }

        acp.SetFloat("meleeAnimationSpeed", attackSpeed);
        acp.SetTrigger("isMeleeAttacking" + randomNumber);
        acp.SetBool("isChasing", false);

        Vector3 targetPosition = player.position;

        // Force the target's Y position to match the Enemy's Y position
        targetPosition.y = transform.position.y;

        // Now look at the flattened position
        transform.LookAt(targetPosition);

    }

    public void PlayerTakeDamage()
    {
            if (Time.time > lastAttackTime)
                {
                    if (playerHealth != null)
                    {
                        if(Vector3.Distance(transform.position, player.position) <= attackRange) playerHealth.TakeDamage(damage);
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

        acp.ResetTrigger("isMeleeAttacking" + randomNumber);
        acp.SetBool("isChasing", false);
        acp.SetTrigger("isDead");

        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;
        isDead = true;
        Destroy(gameObject, 2f);
    }
}