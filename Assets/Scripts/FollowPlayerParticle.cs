using UnityEngine;

public class FollowPlayerParticle : MonoBehaviour
{
    // The player's transform, set by the spawner
    public Transform playerTarget;
    public float followSpeed = 5f;
    public float despawnTime;
    
    private bool isFollowing = false;
    
    // Call this method from the spawner
    public void Initialize(Transform target, bool shouldFollow, float lifetime)
    {
        playerTarget = target;
        isFollowing = shouldFollow;
        despawnTime = lifetime;
        
        if (isFollowing)
        {
            // Change color for following particles (Antibodies) to a pale, light yellow (half-white yellow)
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                // R=1.0, G=1.0, B=0.7 results in a pale, highly visible light yellow
                renderer.material.color = new Color(1f, 1f, 0.7f);
            }
        }
        
        // Destroy the particle after its lifetime
        Destroy(gameObject, despawnTime);
    }
    
    void Update()
    {
        if (isFollowing && playerTarget != null)
        {
            // Move the particle towards the player's position
            Vector3 direction = (playerTarget.position - transform.position).normalized;
            transform.position += direction * followSpeed * Time.deltaTime;
        }
    }
}
