using UnityEngine;
using System.Collections;

public class ParticleSpawner : MonoBehaviour
{
    [Header("Target & Prefab")]
    [Tooltip("The Transform to spawn particles around. Drag your player here.")]
    public Transform targetTransform;

    [Tooltip("The prefab (any GameObject) to spawn.")]
    public GameObject particlePrefab;

    [Header("Spawn Settings")]
    public float spawnInterval = 0.1f;
    [Tooltip("The radius around the spawner where particles can appear.")]
    public float spawnRadius = 20f;
    [Tooltip("Set the minimum size multiplier. e.g. 1.0 = 100%")]
    public float minSizeMultiplier = 1.0f;
    [Tooltip("Set the maximum size multiplier. e.g. 3.0 = 300%.")]
    public float maxSizeMultiplier = 3.0f;
    
    // Note: particleLifetime is no longer used for despawning
    // but can be kept for other purposes if needed.

    private float timer;
    private Vector3 originalParticleScale;
    private Camera mainCamera;

    void Start()
    {
        if (targetTransform == null)
        {
            Debug.LogError("Target Transform is not assigned to the ParticleSpawner!", this);
            enabled = false;
            return;
        }

        if (particlePrefab == null)
        {
            Debug.LogError("Particle Prefab is not assigned to the ParticleSpawner!", this);
            enabled = false;
            return;
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found! Please tag your camera as 'MainCamera'.", this);
            enabled = false;
            return;
        }
        
        // Get the original scale of the prefab
        originalParticleScale = particlePrefab.transform.localScale;
        timer = spawnInterval;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            SpawnParticles();
            timer = spawnInterval;
        }
    }

    void SpawnParticles()
    {
        Vector3 spawnPosition;
        int attempts = 0;
        const int maxAttempts = 50;
        
        // Find a spawn position that is not on screen
        do
        {
            Vector2 randomCirclePoint = Random.insideUnitCircle * spawnRadius;
            spawnPosition = targetTransform.position + new Vector3(randomCirclePoint.x, randomCirclePoint.y, 0);
            attempts++;
            if (attempts > maxAttempts)
            {
                return; // Safety break
            }
        } while (IsVisible(spawnPosition, mainCamera));

        // Instantiate the prefab at the calculated position
        GameObject spawnedObject = Instantiate(particlePrefab, spawnPosition, Quaternion.identity);

        // Randomize the size of the spawned object
        float randomMultiplier = Random.Range(minSizeMultiplier, maxSizeMultiplier);
        spawnedObject.transform.localScale = originalParticleScale * randomMultiplier;
    }
    
    // Helper method to check if a position is visible to the camera
    bool IsVisible(Vector3 position, Camera camera)
    {
        Vector3 viewportPoint = camera.WorldToViewportPoint(position);
        return viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
               viewportPoint.y >= 0 && viewportPoint.y <= 1 &&
               viewportPoint.z > 0;
    }

    void OnDrawGizmosSelected()
    {
        // Draw a wire sphere to visualize the spawn radius in the editor
        if (targetTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetTransform.position, spawnRadius);
        }
    }
}