using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Dash : MonoBehaviour
{
    private Rigidbody _rigidBody;
    public TrailRenderer _trailRenderer;
    private PlayerHealth _playerHealth;
    private Animator animator;
    private PlayerStats _playerStats;
    private PlayerAttack _playerAttack;

    [Header("Dash Damage (Perk)")]
    [SerializeField] private float _dashDamageFallback = 10f;
    private readonly HashSet<int> _damagedThisDash = new HashSet<int>();

    [Header("Dash Hit VFX")]
    [SerializeField] private bool _dashHitVfxEnabled = true;
    [SerializeField] private float _dashHitVfxYOffset = 0.6f;
    [SerializeField] private bool _dashHitVfxDebugLog = false;

    [Header("Dashing")]
    // increased velocity bc it was too slow before
    [SerializeField] private float _dashVelocity = 15f;
    [SerializeField] private float _dashingTime = 0.2f;
    [SerializeField] private float _dashCooldown = 1f;
    private Vector3 _dashingDir;
    private bool _isDashing = false;
    private bool _canDash = true;


    void Start()
    {
        _rigidBody = GetComponent<Rigidbody>();

        // get components if they are null
        if (_trailRenderer == null)
            _trailRenderer = GetComponentInChildren<TrailRenderer>();
        _playerHealth = GetComponent<PlayerHealth>();
        _playerStats = GetComponent<PlayerStats>();
        _playerAttack = GetComponent<PlayerAttack>();
        // get animator from children bc its on the model
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (PauseManager.IsPaused) return;

        var dashInput = Input.GetButtonDown("Dash");

        // 1. check input for dash
        if (dashInput && _canDash)
        {
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");

            _dashingDir = new Vector3(x, 0, z).normalized;

            // if player is not moving dash forward
            if (_dashingDir == Vector3.zero)
            {
                // default to forward direction
                _dashingDir = transform.forward;

            }

            // set animator params for blend tree
            animator.SetFloat("InputX", x);
            animator.SetFloat("InputZ", z);

            StartCoroutine(PerformDash());
        }
    }

    // physics update for rigid body movement
    void FixedUpdate()
    {
        if (_isDashing)
        {
            // move the player position manually
            _rigidBody.MovePosition(_rigidBody.position + _dashingDir * _dashVelocity * Time.fixedDeltaTime);

            TryDealDashDamage();
        }
    }

    private void TryDealDashDamage()
    {
        if (_playerStats == null) return;
        if (_playerStats.dashDamageBonusPercent <= 0f) return;

        float radius = Mathf.Max(0.01f, _playerStats.dashDamageRadius);
        Vector3 origin = _rigidBody != null ? _rigidBody.position : transform.position;
        // check sphere overlap to find enemies
        Collider[] hits = Physics.OverlapSphere(origin, radius, ~0, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0) return;

        float baseDamage = _playerAttack != null
            ? _playerAttack.GetCurrentItemBaseDamage(_dashDamageFallback)
            : _dashDamageFallback;

        float dmg = _playerStats.GetModifiedDamage(baseDamage);
        dmg *= (1f + _playerStats.dashDamageBonusPercent);

        int damage = Mathf.Max(1, Mathf.RoundToInt(dmg));

        foreach (Collider hit in hits)
        {
            if (hit == null) continue;

            // dont hit self
            if (hit.attachedRigidbody != null && hit.attachedRigidbody == _rigidBody) continue;
            if (hit.transform != null && hit.transform.root == transform.root) continue;

            if (!TryGetDamageable(hit, out IDamageable damageable, out Component damageableComponent))
                continue;

            int id = damageableComponent.GetInstanceID();
            // dont dmg same enemy twice in one dash
            if (_damagedThisDash.Contains(id)) continue;
            _damagedThisDash.Add(id);

            damageable.TakeDamage(damage);
            _playerStats.ReportDamageDealt(damage);

            if (_dashHitVfxEnabled)
            {
                Vector3 vfxPos = hit.bounds.center + Vector3.up * _dashHitVfxYOffset;
                SpawnDashHitVfx(vfxPos);

                if (_dashHitVfxDebugLog)
                {
                    Debug.Log($"Dash hit VFX spawned at {vfxPos}");
                }
            }
        }
    }

    private static void SpawnDashHitVfx(Vector3 position)
    {
        GameObject go = new GameObject("DashHitVFX");
        go.transform.position = position;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();

        if (psr != null)
        {
            // try find shader for particles
            Shader shader =
                Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                Shader.Find("Particles/Standard Unlit") ??
                Shader.Find("Particles/Additive") ??
                Shader.Find("Sprites/Default");

            if (shader != null) psr.sharedMaterial = new Material(shader);

            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.sortingOrder = 50;
            psr.trailMaterial = psr.sharedMaterial;
        }

        var main = ps.main;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.duration = 0.35f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.13f);
        main.maxParticles = 128;

        // color gradient form white to purple
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(0.8f, 0.35f, 1f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.85f, 0f),
                new GradientAlphaKey(0.0f, 1f)
            }
        );
        main.startColor = new ParticleSystem.MinMaxGradient(gradient);

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18, 28) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.18f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(gradient);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var trails = ps.trails;
        trails.enabled = true;
        trails.mode = ParticleSystemTrailMode.PerParticle;
        trails.ratio = 1f;
        trails.lifetime = 0.28f;
        trails.dieWithParticles = true;
        trails.inheritParticleColor = true;
        trails.sizeAffectsWidth = true;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(0.045f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
        trails.colorOverLifetime = new ParticleSystem.MinMaxGradient(gradient);

        ps.Play();
        // destroy after 1.5 secs
        Object.Destroy(go, 1.5f);
    }

    private static bool TryGetDamageable(Collider hit, out IDamageable damageable, out Component damageableComponent)
    {
        damageable = null;
        damageableComponent = null;
        if (hit == null) return false;

        // check parent for enemy scripts
        EnemyAI enemy = hit.GetComponentInParent<EnemyAI>();
        if (enemy != null) { damageable = enemy; damageableComponent = enemy; return true; }

        ShooterEnemy shooter = hit.GetComponentInParent<ShooterEnemy>();
        if (shooter != null) { damageable = shooter; damageableComponent = shooter; return true; }

        KamikazeEnemyAI kamikaze = hit.GetComponentInParent<KamikazeEnemyAI>();
        if (kamikaze != null) { damageable = kamikaze; damageableComponent = kamikaze; return true; }

        SlimeBoss slimeBoss = hit.GetComponentInParent<SlimeBoss>();
        if (slimeBoss != null) { damageable = slimeBoss; damageableComponent = slimeBoss; return true; }

        LichBoss lichBoss = hit.GetComponentInParent<LichBoss>();
        if (lichBoss != null) { damageable = lichBoss; damageableComponent = lichBoss; return true; }

        DashBoss dashBoss = hit.GetComponentInParent<DashBoss>();
        if (dashBoss != null) { damageable = dashBoss; damageableComponent = dashBoss; return true; }

        return false;
    }

    private IEnumerator PerformDash()
    {
        _canDash = false;
        _isDashing = true;
        _damagedThisDash.Clear();

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlaySfx(MusicManager.Instance.playerDashSfx);
        }

        // trigger animation only once
        animator.SetTrigger("t_dash");

        _trailRenderer.emitting = true;

        // ignore collision with enemies so we go through them
        Physics.IgnoreLayerCollision(7, 8, true);
        if (_playerHealth != null) _playerHealth.canTakeDamage = false;

        // wait for dash time
        yield return new WaitForSeconds(_dashingTime);

        // stop dash
        _isDashing = false;
        _trailRenderer.emitting = false;
        _rigidBody.velocity = Vector3.zero; // stop inertia

        // reset collision and damage
        Physics.IgnoreLayerCollision(7, 8, false);
        if (_playerHealth != null) _playerHealth.canTakeDamage = true;

        // wait cooldown
        float finalCooldown = (_playerStats != null)
            ? _playerStats.GetModifiedDashCooldown(_dashCooldown)
            : _dashCooldown;
        yield return new WaitForSeconds(finalCooldown);
        _canDash = true;
    }
}