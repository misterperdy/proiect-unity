using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public int maxHealth = 50;
    public int currentHealth;
    public int damage = 10;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    public float sightRange = 20f;
    public float patrolRadius = 10f;
  

    private NavMeshAgent agent;
    private Transform player;
    private PlayerHealth playerHealth;
    private float lastAttackTime = 0f;
    private Vector3 lastKnownPlayerPosition;

    private enum AIState { Patrolling, Chasing, Attacking, Searching }
    private AIState currentState;

    void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
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

    System.Collections.IEnumerator InitializeAI()
    {
        // Wait for the end of the frame to ensure the agent is fully initialized
        yield return new WaitForEndOfFrame();

        currentState = AIState.Patrolling;
        SetRandomPatrolDestination();
    }

    void Update()
    {
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

        transform.LookAt(player);

        if (Time.time > lastAttackTime + attackCooldown)
        {
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
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
        Destroy(gameObject);
    }
}