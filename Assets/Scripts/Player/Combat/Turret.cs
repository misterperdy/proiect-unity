using System.Collections;
using UnityEngine;

public class TurretHandler : MonoBehaviour
{
    [Header("Turret Stats")]
    public int damage = 40;
    public float fireRate = 3f;           // shots per second
    public int projectiles = 1;
    public float detectionRadius = 15f;

    [Header("References")]
    public GameObject arrowPrefab;        // projectile prefab
    public Transform firePoint;           // where bullets spawn

    [Header("Targeting")]
    public LayerMask enemyLayer;          // set to Enemy layer (recommended)
    public string enemyTag = "Enemy";     // fallback if you use tags

    public PlayerStats ownerStats;

    private bool isActive;

    [Header("Rotation")]
    public Transform rotatingPart;
    public float rotationSpeed = 360f; // degrees per second




    [SerializeField] private float lifetime = 15f;


    void Awake()
    {
        // Auto-create firePoint if missing
        if (firePoint == null)
        {
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(transform);
            fp.transform.localPosition = Vector3.up * 1.0f; // adjust later
            firePoint = fp.transform;
        }
    }

    public void StartTurret()
    {
        if (arrowPrefab == null)
        {
            Debug.LogError("TurretHandler: arrowPrefab not assigned!");
            return;
        }

        isActive = true;

        Destroy(gameObject, lifetime);

        StartCoroutine(TurretSequence());
    }

    void RotateTowards(Transform target)
    {
        if (rotatingPart == null) return;

        Vector3 direction = target.position - rotatingPart.position;
        direction.y = 0f; // keep rotation flat (top-down)

        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        rotatingPart.rotation = Quaternion.RotateTowards(
            rotatingPart.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }



    private float nextShotTime;

    IEnumerator TurretSequence()
    {
        nextShotTime = Time.time;

        while (isActive)
        {
            Transform target = FindClosestEnemy();

            if (target != null)
            {
                RotateTowards(target); // every frame

                if (Time.time >= nextShotTime)
                {
                    Shoot(target);
                    nextShotTime = Time.time + (1f / Mathf.Max(0.01f, fireRate));
                }

                yield return null; // IMPORTANT: smooth rotation
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
    }


    void Shoot(Transform target)
    {
        if (firePoint == null) return;

        if (MusicManager.Instance != null)
        {
            // Spatial so it attenuates with distance.
            AudioClip clip = MusicManager.Instance.turretShootSfx;
            if (clip == null) clip = MusicManager.FindClipByName("sfx_turret_is_shooting_arrow");
            if (clip != null)
            {
                MusicManager.Instance.PlaySpatialSfx(clip, firePoint.position, 1f, 2f, 25f);
            }
        }

        Vector3 direction = (target.position - firePoint.position).normalized;

        for (int i = 0; i < Mathf.Max(1, projectiles); i++)
        {
            GameObject bulletGO = Instantiate(
                arrowPrefab,
                firePoint.position,
                Quaternion.LookRotation(direction)
            );

            bulletGO.transform.forward = direction;

            Arrow arrow = bulletGO.GetComponent<Arrow>();
            if (arrow != null)
            {
                arrow.Fire(20f, damage, 0, ownerStats);
            }
            else
            {
                Debug.LogError("Turret bullet prefab is missing Arrow component!");
            }
        }
    }


    Transform FindClosestEnemy()
    {
        Collider[] hits;

        // If enemyLayer is set, use it. Otherwise scan everything.
        if (enemyLayer.value != 0)
            hits = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);
        else
            hits = Physics.OverlapSphere(transform.position, detectionRadius);

        float minDistance = Mathf.Infinity;
        Transform closest = null;

        foreach (var hit in hits)
        {
            bool isEnemyByLayer = (enemyLayer.value != 0) && ((enemyLayer.value & (1 << hit.gameObject.layer)) != 0);
            bool isEnemyByTag = hit.CompareTag(enemyTag);

            if (!isEnemyByLayer && !isEnemyByTag) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = hit.transform;
            }
        }


        return closest;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
