using UnityEngine;

public class AuraRingVisual : MonoBehaviour
{
    [Header("Ring")]
    [Min(0.01f)] public float radius = 4f;
    public float yOffset = 0.05f;

    [Header("Particles")]
    [Min(0f)] public float emissionRate = 24f;
    [Min(0.01f)] public float particleSize = 0.04f;
    [Min(0.01f)] public float minLifetime = 1.2f;
    [Min(0.01f)] public float maxLifetime = 2.4f;

    [Header("Trails")]
    public bool enableTrails = true;
    [Min(0.01f)] public float trailLifetime = 0.18f;
    [Min(0.001f)] public float trailWidth = 0.05f;

    [Header("Orbit (Clockwise)")]
    // negaitve y makes it spin clockwise
    public float minOrbitSpeed = -0f;
    public float maxOrbitSpeed = -3f;

    [Header("Colors")]
    [Min(0.01f)] public float colorCycleSpeed = 0.6f;
    [Range(0f, 1f)] public float colorAlpha = 0.85f;

    private ParticleSystem ps;
    private ParticleSystemRenderer psr;
    private float lastRadius = -1f;

    private static readonly Color Red = new Color(1f, 0.15f, 0.15f, 1f);
    private static readonly Color Blue = new Color(0.1f, 0.6f, 1f, 1f);

    void Awake()
    {
        EnsureParticleSystem();
        ApplyRadius(force: true);
        ApplyGradientColor(force: true);
    }

    void OnEnable()
    {
        EnsureParticleSystem();
        ApplyRadius(force: true);
        ApplyGradientColor(force: true);
    }

    void Update()
    {
        ApplyRadius(force: false);

        if (ps == null) return;

        ApplyGradientColor(force: false);
    }

    public void SetRadius(float newRadius)
    {
        radius = Mathf.Max(0.01f, newRadius);
        ApplyRadius(force: true);
    }

    private void EnsureParticleSystem()
    {
        if (ps == null) ps = GetComponent<ParticleSystem>();
        if (ps == null) ps = gameObject.AddComponent<ParticleSystem>();

        psr = GetComponent<ParticleSystemRenderer>();

        // setup material
        if (psr != null && psr.sharedMaterial == null)
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                Shader.Find("Particles/Standard Unlit") ??
                Shader.Find("Particles/Additive") ??
                Shader.Find("Sprites/Default");

            if (shader != null)
            {
                psr.sharedMaterial = new Material(shader);
            }
        }

        var main = ps.main;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(particleSize * 0.6f, particleSize * 1.2f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(minLifetime, maxLifetime);
        main.playOnAwake = true;
        main.maxParticles = 200;

        // spawn on ring shape
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.radiusThickness = 0.15f;
        shape.arc = 360f;
        // rotate to lay on ground
        shape.rotation = new Vector3(90f, 0f, 0f);
        shape.position = new Vector3(0f, yOffset, 0f);

        // emission always on
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = emissionRate;

        // orbit speed
        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        // use twoconstants to fix unity warnings
        vol.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
        vol.orbitalY = new ParticleSystem.MinMaxCurve(minOrbitSpeed, maxOrbitSpeed);
        vol.orbitalZ = new ParticleSystem.MinMaxCurve(0f, 0f);
        vol.radial = 0f;
        vol.speedModifier = 1f;

        // some noise for randomness
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.12f;
        noise.frequency = 0.35f;
        noise.scrollSpeed = 0.15f;
        noise.damping = true;

        var trails = ps.trails;
        trails.enabled = enableTrails;
        if (enableTrails)
        {
            trails.mode = ParticleSystemTrailMode.PerParticle;
            trails.ratio = 1f;
            trails.lifetime = trailLifetime;
            trails.dieWithParticles = true;
            trails.sizeAffectsWidth = true;
            trails.inheritParticleColor = true;
            trails.minVertexDistance = 0.01f;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
        }

        if (psr != null)
        {
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.minParticleSize = 0f;
            psr.maxParticleSize = 0.5f;
            psr.trailMaterial = psr.sharedMaterial;
        }

        if (!ps.isPlaying) ps.Play();
    }

    private void ApplyRadius(bool force)
    {
        float r = Mathf.Max(0.01f, radius);
        if (!force && Mathf.Approximately(r, lastRadius)) return;
        lastRadius = r;

        if (ps == null) return;

        var shape = ps.shape;
        shape.radius = r;
    }

    private void ApplyGradientColor(bool force)
    {
        if (ps == null) return;

        float t = (Mathf.Sin(Time.time * Mathf.Max(0.01f, colorCycleSpeed)) + 1f) * 0.5f;
        Color c = Color.Lerp(Red, Blue, t);
        c.a = Mathf.Clamp01(colorAlpha);

        var main = ps.main;
        main.startColor = c;

        // tint renderer for URP
        if (psr != null)
        {
            psr.sharedMaterial.color = c;
        }

        // tune trails
        var trails = ps.trails;
        if (trails.enabled)
        {
            trails.lifetime = trailLifetime;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(trailWidth, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
        }
    }
}