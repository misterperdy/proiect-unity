using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelayedExplosion : MonoBehaviour
{
    public float delay = 2.0f; // how long to wait
    public int damage = 20;

    // visuals for effect
    public GameObject explosionVisual;
    // warning circle
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

        // boom time
        if (warningVisual) warningVisual.SetActive(false);
        if (explosionVisual) explosionVisual.SetActive(true);
        float dynamicRadius = 0.5f; // default radius

        if (warningVisual != null)
        {
            // getting radius from the warning circle scale
            // divided by 2 cause radius is half diameter
            dynamicRadius = warningVisual.transform.lossyScale.x / 2f;
        }
        else
        {
            // fallback
            dynamicRadius = transform.lossyScale.x / 2f;
        }

        // checking who got hit
        Collider[] hits = Physics.OverlapSphere(transform.position, dynamicRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth health = hit.GetComponent<PlayerHealth>();
                if (health != null) health.TakeDamage(damage);
            }
        }

        // wait a bit to show explosion then delete
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }
    void OnDrawGizmos()
    {
        // editor helper
        float r = 0.5f;
        if (warningVisual != null) r = warningVisual.transform.lossyScale.x / 2f;
        else r = transform.lossyScale.x / 2f;

        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, r);
    }
}