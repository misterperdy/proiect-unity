using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class KamikazeEnemyAI : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public int maxHealth = 30;
    public int currentHealth;
    public int explosionDamage = 40;
    public float explosionRadius = 4f;
    public float explosionDelay = 1.0f;

    [Header("AI Settings")]
    public float sightRange = 20f;
    public float patrolRadius = 10f;
    public float triggerDistance = 2.5f; //distance to trigger explosion sqeunecene

    [Header("Explosion")]
    public GameObject explosion;

    [Header("Body")]
    public GameObject body;

    [Header("Loot")]
    public float lootMultiplier = 1f;

    public GameObject xpOrbPrefab;

    private Rigidbody rb;
    private NavMeshAgent agent;
    private Transform player;
    private PlayerHealth playerHealth;
    private bool isExploding = false;


    private enum AIState { Patrolling, Chasing, Exploding, Searching }
    private AIState currentState;

    void Start()
    {
        currentHealth = maxHealth;

        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        explosion.SetActive(false);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
        }

        // navmesh setup same as enemyt
        if (!agent.isOnNavMesh)
        {
            NavMeshHit closestHit;
            if (NavMesh.SamplePosition(transform.position, out closestHit, 100f, NavMesh.AllAreas))
                agent.Warp(closestHit.position);
        }

        currentState = AIState.Patrolling;
        SetRandomPatrolDestination();
    }

    void Update()
    {
        // skip if exploding
        if (isExploding) return;

        switch (currentState)
        {
            case AIState.Patrolling:
                Patrol();
                break;
            case AIState.Chasing:
                Chase();
                break;
            case AIState.Searching:
                Search();
                break;
                // explosion is managed by coroutine , not in update
        }
    }

    public void SetupEnemy(int hp, int dmg, Color color, float lootMult)
    {
        maxHealth = hp;
        currentHealth = hp;
        explosionDamage = dmg;
        lootMultiplier = lootMult;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers) r.material.color = color;
    }

    // CanSeePlayer same as enemy
    bool CanSeePlayer()
    {
        if (player == null) return false;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > sightRange) return false;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        RaycastHit hit;
        if (Physics.Raycast(transform.position, directionToPlayer, out hit, sightRange) && hit.transform == player)
            return true;

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
            SetRandomPatrolDestination();
    }

    void Chase()
    {
        if (!CanSeePlayer())
        {
            currentState = AIState.Searching;
            agent.SetDestination(player.position);
            return;
        }

        agent.SetDestination(player.position);

        // triggers explison when close
        if (Vector3.Distance(transform.position, player.position) <= triggerDistance)
        {
            StartCoroutine(ExplodeSequence());
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

    // explosion logic
    IEnumerator ExplodeSequence()
    {
        isExploding = true;
        currentState = AIState.Exploding;

        // stop enemy movement
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        if (rb != null)
        {
            rb.isKinematic = true; // set rigidbody to kinematic so it doesn't take knockback
            rb.velocity = Vector3.zero;
        }

        //explosion timer+animation
        float timer = 0f;

        while (timer < explosionDelay)
        {
            timer += Time.deltaTime;

            //gets bigger
            transform.localScale += Vector3.one * Time.deltaTime * 0.5f;
            yield return null;
        }

        // explode
        Explode();
    }

    void Explode()
    {
        //damage done in a sphere
        Collider[] hitObjects = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in hitObjects)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth ph = hit.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    ph.TakeDamage(explosionDamage);
                }

                Rigidbody rb = hit.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(1000f, transform.position, explosionRadius);
                }
            }
        }

        

        explosion.SetActive(true);
        body.SetActive(false);  

        //destroy as it's served its purpose
        Destroy(gameObject,1f);
    }

    void SetRandomPatrolDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, 1))
            agent.SetDestination(hit.position);
    }

    //to be able to kill it
    public void TakeDamage(int amount)
    {
        if (isExploding) return;

        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        MinimapTracker tracker = GetComponent<MinimapTracker>();
        if (tracker != null)
        {
            tracker.TriggerDeathAnimation(); 
        }

        if (xpOrbPrefab != null)
        {
            int xpToDrop = Mathf.RoundToInt(1 * lootMultiplier);
            GameObject orb = Instantiate(xpOrbPrefab, transform.position + Vector3.up, Quaternion.identity);
            orb.GetComponent<XPOrb>().Initialize(xpToDrop);
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
    }
}
