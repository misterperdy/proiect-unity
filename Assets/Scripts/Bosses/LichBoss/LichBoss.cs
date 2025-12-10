using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LichBoss : MonoBehaviour
{
    [Header("General Stats")]
    public int maxHealth = 1500;
    public float currentHealth; 
    public int contactDamage = 15;
    private bool isInvulnerable = false;
    private bool hasTriggeredPhaseTwo = false;
    private bool isPhaseTwo = false;
    private bool isSurvivePhase = false; // Phase 3

    [Header("Detection")]
    public float sightRange = 10f;
    public Transform castPoint;
    [Header("Spiral Attack (Shared)")]
    public GameObject projectilePrefab;

    [Header("Spiral Phase 1")]
    public int p1_spiralCount = 30;
    public float p1_spiralSpeed = 0.11f;
    [Range(1, 10)] public int p1_spiralArms = 4;
    public float p1_spiralRotationStep = 9f;

    [Header("Spiral Phase 2 & 3")]
    public int p2_spiralCount = 50; // Used in P2
    public float p2_spiralSpeed = 0.12f;
    [Range(1, 10)] public int p2_spiralArms = 5;
    public float p2_spiralRotationStep = 7f;
    public int p2_reverseAfterWaves = 15;

    [Header("Zone Attack (Active - Phase 1)")]
    public GameObject zonePrefabP1;
    public int p1_zoneCount = 25;
    public float p1_zoneSpawnRadius = 20f;

    [Header("Zone Attack (Passive - Phase 2)")]
    public GameObject zonePrefabP2;
    public int p2_passiveZoneCount = 5;
    public float p2_passiveZoneInterval = 1.5f;

    [Header("Zone Attack (Targeted - Phase 3)")]
    public float p3_targetedZoneInterval = 0.5f; // How fast zones spawn on player

    [Header("Summon (Transition Phase)")]
    public List<GameObject> minionPrefabs;
    public int trans_minionsPerWave = 3;
    public float trans_waveInterval = 5f;
    public float healPercentPerSecond = 5f;

    [Header("Summon (Phase 2 Active)")]
    public float p2_summonCooldown = 15f;
    public int p2_minionsToSpawn = 3;
    private float p2_summonTimer = 0f;

    [Header("Survive Phase (Phase 3)")]
    public float p3_decayPercentPerSecond = 1.5f; 

    [Header("Visuals")]
    public GameObject invulnShieldPrefabPhase2;
    public GameObject invulnShieldPrefabSurvivePhase;
    private GameObject activeShield;

    [Header("References")]
    public Animator animator;
    public BossBarSlider bossHealthBar;

    private Transform player;
    private PlayerHealth playerHealth;
    private bool isAttacking = false;
    private Rigidbody rb;

    public enum BossState { Idle, Chasing, Attacking, Summoning, Surviving, Recovering }
    public BossState currentState;

    void Start()
    {
        currentHealth = maxHealth;
        p2_summonTimer = p2_summonCooldown;

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.transform;
            playerHealth = playerGO.GetComponent<PlayerHealth>();
        }

        currentState = BossState.Idle;

        if (bossHealthBar != null)
        {
            bossHealthBar.SetMaxHealth(maxHealth);
            bossHealthBar.ToggleBar(true);
        }
    }

    void Update()
    {
        // 1. Check for Phase 2 Transition (50% HP)
        if (!hasTriggeredPhaseTwo && currentHealth <= maxHealth * 0.50f && !isAttacking && !isSurvivePhase)
        {
            StartCoroutine(PerformSummonTransition());
            return;
        }

        // 2. Check for Phase 3 (Survive) Transition (20% HP)
        if (!isSurvivePhase && hasTriggeredPhaseTwo && currentHealth <= maxHealth * 0.20f)
        {
            StopAllCoroutines(); // Kill P2 background stuff
            StartCoroutine(PerformSurvivePhase()); // Start P3
            return;
        }

        // 3. Phase 2 Logic Timers
        if (isPhaseTwo && !isSurvivePhase)
        {
            if (p2_summonTimer > 0) p2_summonTimer -= Time.deltaTime;
        }

        // 4. Rotation Logic
        if (player != null && currentState != BossState.Summoning)
        {
            SmoothLookAt(player.position);
        }

        if (isAttacking || isSurvivePhase) return;

        // 5. State Machine
        switch (currentState)
        {
            case BossState.Idle:
                if (CanSeePlayer()) currentState = BossState.Chasing;
                break;
            case BossState.Chasing:
                ChaseAndDecide();
                break;
            case BossState.Recovering:
                break;
        }
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.position) < sightRange;
    }

    void ChaseAndDecide()
    {
        if (player == null) return;

        if (isPhaseTwo && p2_summonTimer <= 0)
        {
            StartCoroutine(PerformPhase2QuickSummon());
            return;
        }

        if (isPhaseTwo)
        {
            // In Phase 2, we ONLY do Spiral (with the passive zones running in background).
            StartCoroutine(PerformSpiralAttack());
        }
        else
        {
            // Phase 1: Random choice
            float rng = Random.Range(0f, 1f);
            if (rng > 0.5f) StartCoroutine(PerformSpiralAttack());
            else StartCoroutine(PerformZoneAttack());
        }
    }

    // ATTACK 1: SPIRAL BULLET HELL
    IEnumerator PerformSpiralAttack()
    {
        isAttacking = true;
        currentState = BossState.Attacking;

        if (animator) animator.SetTrigger("Attack01");

        yield return new WaitForSeconds(0.5f);

        int count = isPhaseTwo ? p2_spiralCount : p1_spiralCount;
        float speed = isPhaseTwo ? p2_spiralSpeed : p1_spiralSpeed;
        int arms = isPhaseTwo ? p2_spiralArms : p1_spiralArms;
        float rotStep = isPhaseTwo ? p2_spiralRotationStep : p1_spiralRotationStep;

        float currentBaseAngle = 0f;
        float anglePerArm = 360f / arms;
        float rotationDir = 1f;

        Vector3 spawnOrigin = castPoint != null ? castPoint.position : transform.position + Vector3.up;

        for (int i = 0; i < count; i++)
        {
            if (isPhaseTwo && p2_reverseAfterWaves > 0)
            {
                if (i > 0 && i % p2_reverseAfterWaves == 0) rotationDir *= -1f;
            }

            for (int armIndex = 0; armIndex < arms; armIndex++)
            {
                if (projectilePrefab != null)
                {
                    float currentBulletAngle = currentBaseAngle + (armIndex * anglePerArm);
                    Quaternion rotation = Quaternion.Euler(0, currentBulletAngle, 0);
                    Instantiate(projectilePrefab, spawnOrigin, rotation);
                }
            }

            currentBaseAngle += (rotStep * rotationDir);
            yield return new WaitForSeconds(speed);
        }

        yield return new WaitForSeconds(1f);

        isAttacking = false;
        currentState = BossState.Chasing;
    }

    // ATTACK 2: LIGHTNING ZONES (P1 Only) 
    IEnumerator PerformZoneAttack()
    {
        isAttacking = true;
        currentState = BossState.Attacking;

        if (animator) animator.SetTrigger("Attack01");

        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < p1_zoneCount; i++)
        {
            if (zonePrefabP1 != null)
            {
                Vector2 randomCircle = Random.insideUnitCircle * p1_zoneSpawnRadius;
                Vector3 spawnPos = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
                spawnPos.y = 0.212f;
                Instantiate(zonePrefabP1, spawnPos, Quaternion.identity);
            }
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(1.5f);
        isAttacking = false;
        currentState = BossState.Chasing;
    }

    // ATTACK 3: SUMMON (PHASE 2 START) 
    IEnumerator PerformSummonTransition()
    {
        hasTriggeredPhaseTwo = true;
        isAttacking = true;
        isInvulnerable = true;
        ToggleShield(true);
        currentState = BossState.Summoning;

        if (animator) animator.SetBool("IsSummoning", true);

        float waveTimer = 0f;

        while (currentHealth < maxHealth)
        {
            currentHealth += (maxHealth * (healPercentPerSecond / 100f)) * Time.deltaTime;
            if (currentHealth > maxHealth) currentHealth = maxHealth;
            if (bossHealthBar != null) bossHealthBar.SetHealth((int)currentHealth);

            if (waveTimer <= 0f)
            {
                SpawnWave(trans_minionsPerWave);
                waveTimer = trans_waveInterval;
            }
            else
            {
                waveTimer -= Time.deltaTime;
            }

            yield return null;
        }

        if (animator) animator.SetBool("IsSummoning", false);
        isInvulnerable = false;
        ToggleShield(false);
        isPhaseTwo = true;

        // Start P2 Passives
        StartCoroutine(ConstantZoneSpawner());

        yield return new WaitForSeconds(1f);
        isAttacking = false;
        currentState = BossState.Chasing;
    }

    IEnumerator PerformPhase2QuickSummon()
    {
        isAttacking = true;
        currentState = BossState.Summoning;
        if (animator) animator.SetBool("IsSummoning", true);
        yield return new WaitForSeconds(1.0f);
        SpawnWave(p2_minionsToSpawn);
        p2_summonTimer = p2_summonCooldown;
        if (animator) animator.SetBool("IsSummoning", false);
        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
        currentState = BossState.Chasing;
    }

    // PHASE 3: SURVIVE
    IEnumerator PerformSurvivePhase()
    {

        // 1. Setup Phase 3
        isSurvivePhase = true;
        isInvulnerable = true; // Immune to player
        isAttacking = true;    // Blocks other updates
        currentState = BossState.Surviving;
        ToggleShield(true);
        yield return new WaitForSeconds(2f);

        // Start Animation (Looping Attack02)
        if (animator) animator.SetBool("IsSummoning", true);

        // Variables for internal loops
        float currentBaseAngle = 0f;
        float anglePerArm = 360f / p2_spiralArms;
        float rotationDir = 1f;

        float spiralTimer = 0f;
        int waveCount = 0;

        float zoneTimer = 0f;

        Vector3 spawnOrigin = castPoint != null ? castPoint.position : transform.position + Vector3.up;

        // 2. Infinite Loop until Death
        while (currentHealth > 0)
        {
            // HEALTH DECAY 
            currentHealth -= (maxHealth * (p3_decayPercentPerSecond / 100f)) * Time.deltaTime;
            if (bossHealthBar != null) bossHealthBar.SetHealth((int)currentHealth);

            // CONTINUOUS SPIRAL
            spiralTimer -= Time.deltaTime;
            if (spiralTimer <= 0)
            {
                // Reversal Logic
                if (p2_reverseAfterWaves > 0 && waveCount > 0 && waveCount % p2_reverseAfterWaves == 0)
                {
                    rotationDir *= -1f;
                }

                // Fire Pattern
                for (int armIndex = 0; armIndex < p2_spiralArms; armIndex++)
                {
                    if (projectilePrefab != null)
                    {
                        float currentBulletAngle = currentBaseAngle + (armIndex * anglePerArm);
                        Quaternion rotation = Quaternion.Euler(0, currentBulletAngle, 0);
                        Instantiate(projectilePrefab, spawnOrigin, rotation);
                    }
                }

                currentBaseAngle += (p2_spiralRotationStep * rotationDir);
                waveCount++;
                spiralTimer = p2_spiralSpeed; // Reset shot timer
            }

            // TARGETED ZONES 
            zoneTimer -= Time.deltaTime;
            if (zoneTimer <= 0)
            {
                if (player != null && zonePrefabP2 != null)
                {
                    // Spawn directly at player position
                    Vector3 targetPos = player.position;
                    targetPos.y = 0.212f;
                    Instantiate(zonePrefabP2, targetPos, Quaternion.identity);
                }
                zoneTimer = p3_targetedZoneInterval;
            }

            yield return null; // Wait for next frame
        }

        // 3. Death
        Die();
    }

    // PHASE 2 PASSIVE
    IEnumerator ConstantZoneSpawner()
    {
        while (true)
        {
            for (int i = 0; i < p2_passiveZoneCount; i++)
            {
                if (zonePrefabP2 != null)
                {
                    Vector2 randomCircle = Random.insideUnitCircle * 15f;
                    Vector3 spawnPos = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
                    spawnPos.y = 0.212f;
                    Instantiate(zonePrefabP2, spawnPos, Quaternion.identity);
                }
            }
            yield return new WaitForSeconds(p2_passiveZoneInterval);
        }
    }

    // HELPERS 

    void SpawnWave(int count)
    {
        if (minionPrefabs == null || minionPrefabs.Count == 0) return;
        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, minionPrefabs.Count);
            GameObject enemyToSpawn = minionPrefabs[randomIndex];
            if (enemyToSpawn != null)
            {
                Vector2 randomCircle = Random.insideUnitCircle * 6f;
                Vector3 spawnPos = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
                spawnPos.y = 0.5f;
                Instantiate(enemyToSpawn, spawnPos, Quaternion.identity);
            }
        }
    }

    void SmoothLookAt(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
        }
    }

    public void TakeDamage(int damage)
    {
        // IMMUNITY: Phase 2 Transition OR Phase 3 (Survive)
        if (isInvulnerable) return;

        currentHealth -= damage;
        if (bossHealthBar != null) bossHealthBar.SetHealth((int)currentHealth);

        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        // Stop logic
        StopAllCoroutines();
        ToggleShield(false);
        this.enabled = false;

        if (animator) animator.SetTrigger("Die");
        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;

        if (bossHealthBar != null) bossHealthBar.ToggleBar(false);
        Destroy(gameObject, 3f);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (playerHealth != null) playerHealth.TakeDamage(contactDamage);
        }
    }

    void ToggleShield(bool state)
    {
        if (state)
        {
            // Enable Shield: Only spawn if one doesn't already exist
            if (activeShield == null)
            {
                // Spawn at boss position, with boss rotation, as a child of the boss
                if(!isSurvivePhase) activeShield = Instantiate(invulnShieldPrefabPhase2, transform.position + Vector3.up + Vector3.up, transform.rotation, transform);

                else
                {
                    activeShield = Instantiate(invulnShieldPrefabSurvivePhase, transform.position + Vector3.up + Vector3.up, transform.rotation, transform);
                }
            }
        }
        else
        {
            // Disable Shield: Destroy the instance
            if (activeShield != null)
            {
                Destroy(activeShield);
                activeShield = null;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, sightRange);
    }
}