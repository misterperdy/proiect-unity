// ExplosionHandler.cs

using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ExplosionHandler : MonoBehaviour
{
    public int damage = 40; // dmg done to enemies
    public float radius = 4f; // how big the explosion is
    public float delay = 1.0f; // time before boom

    public PlayerStats ownerStats; // reference to track stats

    public int explosionDamage { get { return damage; } }
    public float explosionRadius { get { return radius; } }
    public float explosionDelay { get { return delay; } }

    public void StartExplosion()
    {
        // start the timer for explosion
        StartCoroutine(ExplodeSequence());
    }

    IEnumerator ExplodeSequence()
    {
        float timer = 0f;
        // wait for the delay time
        while (timer < explosionDelay)
        {
            timer += Time.deltaTime;
            // making the sphere bigger over time to show it charging up
            transform.localScale += Vector3.one * Time.deltaTime * 0.5f;
            yield return null;
        }

        // boom time
        Explode();
    }

    void Explode()
    {
        // find all objects inside the explosion radius
        Collider[] hitObjects = Physics.OverlapSphere(transform.position, radius);

        foreach (Collider hit in hitObjects)
        {
            // dont hurt the player with his own bomb
            if (hit.CompareTag("Player"))
            {
                continue;
            }


            // try to find enemy scripts on the hit objects
            EnemyAI enemy = hit.GetComponent<EnemyAI>();
            DashBoss boss = hit.GetComponent<DashBoss>();

            // apply damage if it is a normal enemy
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                if (ownerStats != null) ownerStats.ReportDamageDealt(damage);
            }
            // apply damage if it is a boss
            else if (boss != null)
            {
                boss.TakeDamage(damage);
                if (ownerStats != null) ownerStats.ReportDamageDealt(damage);
            }

            // adding physics force to objects so they fly away
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(1000f, transform.position, radius);

                // disabling navmesh agent so the physics can work (otherwise enemy stays stuck)
                NavMeshAgent agent = hit.GetComponent<NavMeshAgent>();
                if (agent != null && agent.enabled)
                {
                    agent.isStopped = true;
                }

            }
        }

        // destroy the explosion object after finish
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        // drawing a red circle in editor to see the range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

}