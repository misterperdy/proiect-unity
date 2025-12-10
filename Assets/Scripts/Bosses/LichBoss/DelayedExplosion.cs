using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelayedExplosion : MonoBehaviour
{
    public float delay = 2.0f; // Time before explosion
    public int damage = 20;

    // Assign a red cylinder or particle effect here
    public GameObject explosionVisual;
    // Assign the semi-transparent circle sprite/quad here
    public GameObject warningVisual;

    void Start()
    {
        StartCoroutine(ExplodeSequence());
    }

    IEnumerator ExplodeSequence()
    {
        if (warningVisual) warningVisual.SetActive(true);
        if (explosionVisual) explosionVisual.SetActive(false);

        yield return new WaitForSeconds(delay);

        // Boom
        if (warningVisual) warningVisual.SetActive(false);
        if (explosionVisual) explosionVisual.SetActive(true);
        float dynamicRadius = 0.5f; // Default fallback (1 unit wide)

        if (warningVisual != null)
        {
            // If the warning circle is scaled to 5, the width is 5.
            // Radius is half of width.
            // We use lossyScale to get the global scale (in case parent is scaled)
            dynamicRadius = warningVisual.transform.lossyScale.x / 2f;
        }
        else
        {
            // Fallback to the root object's scale
            dynamicRadius = transform.lossyScale.x / 2f;
        }

        // Deal Damage
        Collider[] hits = Physics.OverlapSphere(transform.position, dynamicRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth health = hit.GetComponent<PlayerHealth>();
                if (health != null) health.TakeDamage(damage);
            }
        }

        // Linger for a moment to show the explosion effect, then die
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }
    void OnDrawGizmos()
    {
        float r = 0.5f;
        if (warningVisual != null) r = warningVisual.transform.lossyScale.x / 2f;
        else r = transform.lossyScale.x / 2f;

        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, r);
    }
}