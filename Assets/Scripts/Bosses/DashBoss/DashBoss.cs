using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DashBoss : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public int maxHealth = 200;
    public int currentHealth;
    public int contactDamage = 20; // contact damage to the player

    [Header("Detection")]
    public float sightRange = 30f; // how far to see you

    [Header("Dash Settings")]
    public float dashRange = 15f; // distance the attack starts from
    public float dashChargeTime = 1.5f; // Cue - how much he sits idle and aims at you
    public float dashSpeed = 40f;
    public float dashDuration = 0.5f;
    public float attackCooldown = 3f; // after a dash

    [Header("References")]
    public LineRenderer lineRenderer;
    public Animator animator;

    private NavMeshAgent agent;
    private Transform player;
    private PlayerHealth playerHealth; // reference to plyaerHealth scirpt
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

        //setup line renderer
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;
        lineRenderer.startWidth = 0.2f;
        lineRenderer.endWidth = 0.2f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;

        //save normal speed
        defaultSpeed = agent.speed;
        defaultAcceleration = agent.acceleration;

        //find player
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.transform;
            playerHealth = playerGO.GetComponent<PlayerHealth>();
        }

        currentState = BossState.Idle;

        if(bossHealthBar != null)
        {
            bossHealthBar.SetMaxHealth(maxHealth);
            bossHealthBar.ToggleBar(true);
        }
    }

    void Update()
    {
        if (animator != null)
        {
            // Check if agent has a path and is moving effectively
            bool isMoving = agent.velocity.magnitude > 0.1f;
            animator.SetBool("isMoving", isMoving);
        }

        if (isAttacking) return; // skip frame if he's attaciong

        //State Machine
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

        //just check distance for now, no sight raycast check
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

        //if we are in dash range and no t on cooldown, then dash
        if(distance <= dashRange)
        {
            StartCoroutine(PerformDashAttack());
        }
        else
        {
            //walk to him
            agent.SetDestination(player.position);
            agent.speed = defaultSpeed;
        }
    }

    //main attack logic
    private IEnumerator PerformDashAttack()
    {
        isAttacking = true;
        currentState = BossState.ChargingDash;

        //stop the agent
        agent.isStopped = true;
        agent.ResetPath(); // reset current path so it doesn't interfere

        //charge time + visual cue
        lineRenderer.enabled = true;
        float timer = 0f;

        while (timer < dashChargeTime)
        {
            timer += Time.deltaTime;

            SmoothLookAt(player.position); // smooth rotation

            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, player.position);

            yield return null;
        }

        Vector3 finalAttackPosition = player.position;

        //execute dash
        lineRenderer.enabled = false;
        currentState = BossState.Dashing;

        if (animator != null)
        {
            animator.SetTrigger("attack");
        }

        //set huge speed on dash
        agent.speed = dashSpeed;
        agent.acceleration = 1000f;
        agent.isStopped = false;
        agent.SetDestination(finalAttackPosition); // go to where he remembers the plyaer to be

        yield return null; //wait 1 frame

        while (agent.pathPending)
        {
            yield return null; // wait for path to get set on agent
        }

        //let him arrive
        float dashTimer = 0f;
        while(dashTimer < dashDuration && agent.remainingDistance > 1f)
        {
            dashTimer += Time.deltaTime;

            CheckDamageCollision();

            yield return null;
        }

        //stop and recover
        agent.isStopped = true;
        agent.speed = defaultSpeed;
        agent.acceleration = defaultAcceleration;

        currentState = BossState.Recovering;

        // wait until to folow player again
        yield return new WaitForSeconds(attackCooldown);

        agent.isStopped = false;
        isAttacking = false;
        currentState = BossState.Chasing;
    }

    void CheckDamageCollision()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 2f); // sphere around the boss
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(contactDamage);
                    //INVICIBILITY frames logic would go here, but now there is none.
                }
            }
        }
    }

    void SmoothLookAt(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0;

        if(direction!= Vector3.zero)
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

        //update UI
        if (bossHealthBar != null) {
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

        if(bossHealthBar != null)
        {
            bossHealthBar.ToggleBar(false); // hide boss bar
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

            //knobkback
            Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                Vector3 pushDirection = (collision.transform.position - transform.position).normalized;
                playerRb.AddForce(pushDirection * 5f, ForceMode.Force);
            }
        }
    }

    //for editor to see his range.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, dashRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, sightRange);
    }
}
