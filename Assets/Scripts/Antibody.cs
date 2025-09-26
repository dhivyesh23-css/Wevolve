// Filename: Antibody.cs
using UnityEngine;
using System.Collections;

public class Antibody : MonoBehaviour
{
    public enum EnemyType { Melee, Shooter, Bomber }
    [Header("Core Settings")]
    public EnemyType enemyType = EnemyType.Melee;
    public Transform player;
    public float health = 100f;
    public float moveSpeed = 3f;
    public float detectionRange = 15f;
    public float attackRange = 2f;

    [Header("Shooter Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 1f;
    private float nextFireTime = 0f;

    [Header("Bomber Settings")]
    public GameObject explosionParticlePrefab;
    public float explosionRadius = 3f;
    public float explosionDamage = 50f;
    public float timeToExplode = 1.5f;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (player == null)
        {
            // Find player by tag. Make sure your player object has the "Player" tag.
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
        }
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange)
        {
            // --- Movement & Rotation ---
            Vector2 direction = player.position - transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            rb.rotation = angle;
            rb.linearVelocity = direction.normalized * moveSpeed;

            // --- Behavior by Type ---
            switch (enemyType)
            {
                case EnemyType.Shooter:
                    if (distanceToPlayer <= attackRange) {
                        rb.linearVelocity = Vector2.zero; // Stop to shoot
                        if (Time.time >= nextFireTime) {
                            Shoot();
                            nextFireTime = Time.time + 1f / fireRate;
                        }
                    }
                    break;
                case EnemyType.Bomber:
                    if (distanceToPlayer <= attackRange) {
                        StartCoroutine(ExplodeSequence());
                    }
                    break;
            }
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    void Shoot()
    {
        if(bulletPrefab && firePoint)
            Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
    }

    IEnumerator ExplodeSequence()
    {
        // Disable this script to prevent re-triggering
        this.enabled = false; 
        
        // You could add a flashing visual effect here
        yield return new WaitForSeconds(timeToExplode);

        if(explosionParticlePrefab)
            Instantiate(explosionParticlePrefab, transform.position, Quaternion.identity);

        // Damage logic
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach(var hit in hits) {
            if(hit.CompareTag("Player")) {
                Debug.Log("Player hit by explosion!");
                // Example: hit.GetComponent<PlayerHealth>().TakeDamage(explosionDamage);
            }
        }
        Destroy(gameObject);
    }
}