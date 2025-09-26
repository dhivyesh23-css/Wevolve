using UnityEngine;

/// <summary>
/// Base class for all antibody enemies. Handles common properties like health and targeting the player.
/// </summary>
public class Antibody : MonoBehaviour
{
    [Header("Base Antibody Stats")]
    public float health = 100f;
    public float moveSpeed = 3f;
    public float detectionRadius = 15f;
    
    protected Transform playerTransform;

    protected virtual void Start()
    {
        // Find the player in the scene. For efficiency, consider using a singleton GameManager.
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    protected virtual void Update()
    {
        if (playerTransform == null) return;

        // Basic behavior: move towards the player if they are within detection radius
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer <= detectionRadius)
        {
            MoveTowardsPlayer();
        }
    }

    protected virtual void MoveTowardsPlayer()
    {
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        // Can add explosion effects or other death behaviors here
        Destroy(gameObject);
    }
}
