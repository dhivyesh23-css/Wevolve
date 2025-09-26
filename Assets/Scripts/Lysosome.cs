using UnityEngine;

/// <summary>
/// A suicide bomber type of antibody. Moves towards the player and explodes when close.
/// </summary>
public class LysosomeAntibody : Antibody
{
    [Header("Lysosome Specifics")]
    public float explosionRadius = 3f;
    public float explosionDamage = 50f;
    public float detonationDistance = 1.5f;
    public GameObject explosionParticlePrefab; // Assign your explosion particle effect

    protected override void Update()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= detectionRadius)
        {
            MoveTowardsPlayer();

            // Check if close enough to detonate
            if (distanceToPlayer <= detonationDistance)
            {
                Explode();
            }
        }
    }

    private void Explode()
    {
        // Instantiate particle effect
        if (explosionParticlePrefab != null)
        {
            Instantiate(explosionParticlePrefab, transform.position, Quaternion.identity);
        }

        // Damage objects within the explosion radius
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D hit in colliders)
        {
            // Assuming your player has a component to handle damage, e.g., PlayerHealth
            if (hit.CompareTag("Player"))
            {
                 // hit.GetComponent<PlayerHealth>().TakeDamage(explosionDamage);
                 Debug.Log("Player hit by explosion!");
            }
        }

        // Destroy the lysosome object
        Destroy(gameObject);
    }

    // Optional: Draw a gizmo in the editor to visualize the explosion radius
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detonationDistance);
    }
}
