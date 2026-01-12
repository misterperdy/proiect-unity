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
    public float triggerDistance = 2.5f; // if player gets this close we go boom

    [Header("Explosion")]
    public GameObject explosion;

    [Header("Body")]
    public GameObject body;

    [Header("Loot")]
    public float lootMultiplier = 1f;
    public GameObject xpOrbPrefab;

    [Header("Hit Effect")]
    public GameObject hitParticles;
    public GameObject kamikazeMat;
    public float fadeTime = 0.01f; // visual feedback speed
    public Color32 hitColor = new Color32(255, 0, 0, 255);
    public string rarity;

    private Rigidbody rb;
    private NavMeshAgent agent;
    private Transform player;
    private PlayerHealth playerHealth;
    private bool isExploding = false;
    private Material hitMat;
    private Color32 originalColor = new Color32(255, 255, 255, 0);

    private float lastDamageSfxTime = -999f;
    private const float damageSfxMinInterval = 0.08f;


    private enum AIState { Patrolling, Chasing, Exploding, Searching }
    private AIState currentState;

    void Start()
    {
        // color setup
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

        hitMat = kamikazeMat.GetComponent<SkinnedMeshRenderer>().material;

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

        // navmesh safety check
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
        // once started, cant stop explosion
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
        }
    }

    public void SetupEnemy(int hp, int dmg, Color color, float lootMult, string rrty)
    {
        maxHealth = hp;
        currentHealth = hp;
        explosionDamage = dmg;
        lootMultiplier = lootMult;
        rarity = rrty;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers) r.material.color = color;
    }

    // checking vision
    bool CanSeePlayer()
    {
        if (player == null) return false;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > sightRange) return false;

        Vector3 origin = transform.position + Vector3.up;
        Vector3 target = player.position + Vector3.up;

        Vector3 directionToPlayer = (target - origin).normalized;

        RaycastHit hit;
        if (Physics.Raycast(origin, directionToPlayer, out hit, sightRange))
        {
            if (hit.transform == player) return true;
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

        // reached dest
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

        // checks if close enough to blow up
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

    // the big boom logic
    IEnumerator ExplodeSequence()
    {
        isExploding = true;
        currentState = AIState.Exploding;

        // freezing movement
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
        }

        // expanding animation
        float timer = 0f;

        while (timer < explosionDelay)
        {
            timer += Time.deltaTime;

            // visual swelling
            transform.localScale += Vector3.one * Time.deltaTime * 0.5f;
            yield return null;
        }

        // actually dealing damage
        Explode();
    }

    void Explode()
    {
        // finding what we hit
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

                // knockback effect
                Rigidbody rb = hit.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(1000f, transform.position, explosionRadius);
                }
            }
        }


        // showing explosion gfx
        explosion.SetActive(true);
        body.SetActive(false);

        // cleaning up
        Destroy(gameObject, 1f);
    }

    void SetRandomPatrolDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, 1))
            agent.SetDestination(hit.position);
    }

    public void TakeDamage(int amount)
    {
        StartCoroutine(SetHitEffect());
        StartCoroutine(SetHitParticles());

        if (isExploding) return; // cant kill if already exploding

        currentHealth -= amount;

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

        kamikazeMat.GetComponent<SkinnedMeshRenderer>().material = hitMat;
        hitMat.color = hitColor;

        while (hitMat.color != originalColor)
        {
            hitMat.color = Color.Lerp(hitMat.color, originalColor, fadeTime);

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
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlaySpatialSfx(MusicManager.Instance.enemyDiesSfx, transform.position, 1f, 2f, 25f);
        }

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