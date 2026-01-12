using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DashBoss : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public int maxHealth = 200;
    public int currentHealth;
    public int contactDamage = 20; // dmg when touching player

    [Header("Detection")]
    public float sightRange = 30f; // range to see the player

    [Header("Dash Settings")]
    public float dashRange = 15f; // distance to start dashing
    public float dashChargeTime = 1.5f; // how long he waits before launching
    public float dashSpeed = 40f;
    public float dashDuration = 0.5f;
    public float attackCooldown = 3f; // time between attacks

    [Header("References")]
    public LineRenderer lineRenderer;
    public Animator animator;

    [Header("Dash Visuals")]
    public Material dashIndicatorMaterial; // material for the red line
    public float dashLineScrollSpeed = -5.0f; // animation speed for texture
    public float dashLineTiling = 5.0f; // texture tiling count

    private NavMeshAgent agent;
    private Transform player;
    private PlayerHealth playerHealth; // ref to player health script
    private float defaultSpeed;
    private float defaultAcceleration;
    private bool isAttacking = false;

    [Header("Hit Effect")]
    public GameObject hitParticles;

    public enum BossState { Idle, Chasing, ChargingDash, Dashing, Recovering }
    public BossState currentState;

    [Header("UI")]
    public BossBarSlider bossHealthBar;

    private float lastDamageSfxTime = -999f;
    private const float damageSfxMinInterval = 0.08f;

    void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;

        // 1. making the line look like a cone (wide at boss, thin at target)
        lineRenderer.widthCurve = new AnimationCurve(
            new Keyframe(0, 0.6f),  // Start 
            new Keyframe(1, 0.1f)   // End
        );

        // 2. assigning material
        if (dashIndicatorMaterial != null)
        {
            lineRenderer.material = dashIndicatorMaterial;
            lineRenderer.textureMode = LineTextureMode.Tile; // needed for scrolling animation
        }
        else
        {
            // fallback if i forgot to assign material
            lineRenderer.material = new Material(Shader.Find("Mobile/Particles/Additive"));
        }

        // 3. color gradient from red to orange fade
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.red, 0.0f), new GradientColorKey(new Color(1f, 0.5f, 0f), 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        lineRenderer.colorGradient = gradient;

        // -------------------------------------------------------------

        // remembering default speeds to reset later
        defaultSpeed = agent.speed;
        defaultAcceleration = agent.acceleration;

        // finding player in scene
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
        if (animator != null)
        {
            // updating animation bool based on movement
            bool isMoving = agent.velocity.magnitude > 0.1f;
            animator.SetBool("isMoving", isMoving);
        }

        if (isAttacking) return; // dont do anything if already attacking

        // simple state machine
        switch (currentState)
        {
            case BossState.Idle:
                if (CanSeePlayer())
                {
                    if (MusicManager.Instance != null) MusicManager.Instance.PlayBossMusic();
                    currentState = BossState.Chasing;
                }
                break;
            case BossState.Chasing:
                ChaseAndDecide();
                break;
            case BossState.Recovering:
                ChaseAndDecide();
                break;
        }


    }

    bool CanSeePlayer()
    {
        if (player == null)
        {
            return false;
        }

        // checking distance only, no raycast needed for now
        float distance = Vector3.Distance(transform.position, player.position);
        return distance < sightRange;
    }

    void ChaseAndDecide()
    {
        if (player == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        // logic to decide if we dash or walk
        if (distance <= dashRange)
        {
            StartCoroutine(PerformDashAttack());
        }
        else
        {
            // just walk towards player
            agent.SetDestination(player.position);
            agent.speed = defaultSpeed;
        }
    }

    // the main attack logic
    private IEnumerator PerformDashAttack()
    {
        isAttacking = true;
        currentState = BossState.ChargingDash;

        // stop moving to charge up
        agent.isStopped = true;
        agent.ResetPath(); // clearing path

        // show the red line
        lineRenderer.enabled = true;
        float timer = 0f;

        while (timer < dashChargeTime)
        {
            timer += Time.deltaTime;

            SmoothLookAt(player.position); // face the player

            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, player.position);

            // animating the texture on the line
            if (lineRenderer.material != null)
            {
                // scrolling texture effect
                float offset = Time.time * dashLineScrollSpeed;
                lineRenderer.material.mainTextureOffset = new Vector2(offset, 0);
                lineRenderer.material.mainTextureScale = new Vector2(dashLineTiling, 1);
            }

            yield return null;
        }

        Vector3 finalAttackPosition = player.position;

        // hide line and go
        lineRenderer.enabled = false;
        currentState = BossState.Dashing;

        if (animator != null)
        {
            animator.SetTrigger("attack");
        }

        // making him super fast for the dash
        agent.speed = dashSpeed;
        agent.acceleration = 1000f;
        agent.isStopped = false;
        agent.SetDestination(finalAttackPosition); // dash to last known pos

        yield return null; // wait one frame for unity pathfinding

        while (agent.pathPending)
        {
            yield return null; // waiting for path calc
        }

        // wait until he arrives or time runs out
        float dashTimer = 0f;
        while (dashTimer < dashDuration && agent.remainingDistance > 1f)
        {
            dashTimer += Time.deltaTime;

            CheckDamageCollision(); // manual collision check

            yield return null;
        }

        // stop the dash
        agent.isStopped = true;
        agent.speed = defaultSpeed;
        agent.acceleration = defaultAcceleration;

        currentState = BossState.Recovering;

        // cooldown period
        yield return new WaitForSeconds(attackCooldown);

        agent.isStopped = false;
        isAttacking = false;
        currentState = BossState.Chasing;
    }

    void CheckDamageCollision()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 2f); // checking sphere around boss
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(contactDamage);
                    // maybe add invincibility frames later
                }
            }
        }
    }

    void SmoothLookAt(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0; // keep rotation flat

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        StartCoroutine(SetHitParticles());
        if (MusicManager.Instance != null && Time.time - lastDamageSfxTime >= damageSfxMinInterval)
        {
            MusicManager.Instance.PlaySpatialSfx(MusicManager.Instance.golemBossTookDamageSfx, transform.position, 1f, 3f, 35f);
            lastDamageSfxTime = Time.time;
        }

        // updating ui bar
        if (bossHealthBar != null)
        {
            bossHealthBar.SetHealth(currentHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
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
            MusicManager.Instance.PlaySpatialSfx(MusicManager.Instance.bossDiesSfx, transform.position, 1f, 3f, 45f);
        }

        if (MusicManager.Instance != null) MusicManager.Instance.PlayGameplayMusic();

        Debug.Log("Boss Defeated!");

        if (bossHealthBar != null)
        {
            bossHealthBar.ToggleBar(false); // remove boss bar
        }

        Destroy(gameObject);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(contactDamage);
            }

            // pushing player back
            Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                Vector3 pushDirection = (collision.transform.position - transform.position).normalized;
                playerRb.AddForce(pushDirection * 5f, ForceMode.Force);
            }
        }
    }

    // debug drawing for editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, dashRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, sightRange);
    }
}