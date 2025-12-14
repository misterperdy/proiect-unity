// ExplosionHandler.cs

using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ExplosionHandler : MonoBehaviour
{
    public int damage = 40;
    public float radius = 4f;
    public float delay = 1.0f;

    public int explosionDamage { get { return damage; } }
    public float explosionRadius { get { return radius; } }
    public float explosionDelay { get { return delay; } }

    public void StartExplosion()
    {
        StartCoroutine(ExplodeSequence());
    }

    IEnumerator ExplodeSequence()
    {
        float timer = 0f;
        while (timer < explosionDelay)
        {
            timer += Time.deltaTime;
            transform.localScale += Vector3.one * Time.deltaTime * 0.5f;
            yield return null;
        }

        Explode();
    }

    void Explode()
    {
        Collider[] hitObjects = Physics.OverlapSphere(transform.position, radius);

        foreach (Collider hit in hitObjects)
        {
            if (hit.CompareTag("Player"))
            {
                continue;
            }

            
                EnemyAI enemy = hit.GetComponent<EnemyAI>();
                DashBoss boss = hit.GetComponent<DashBoss>();

                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                }
                else if (boss != null)
                {
                    boss.TakeDamage(damage);
                }

                Rigidbody rb = hit.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(1000f, transform.position, radius);

                    NavMeshAgent agent = hit.GetComponent<NavMeshAgent>();
                    if (agent != null && agent.enabled)
                    {
                        agent.isStopped = true;
                    }
                
            }
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

}

