using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;            // assign the Bacteriophage
    public float yThreshold = 0f;       // camera starts following after player crosses this Y
    public float smoothSpeed = 5f;      // camera follow smoothing
    public Vector3 offset = new Vector3(0f, 0f, -10f); // camera behind player

    [Header("Optional Background Parallax")]
    public Transform background;        // assign background object if any
    public float parallaxFactor = 0.5f; // 0 = static, 1 = follows exactly

    private Vector3 initialOffset;      // stored initial offset from player
    private Vector3 bgInitialPos;       // background start position
    private bool yFollowActive = false; // flag to check if threshold was crossed

    void Start()
    {
        if (player == null)
        {
            Debug.LogWarning("CameraFollow: No player assigned!");
            enabled = false;
            return;
        }

        initialOffset = offset;

        if (background != null)
            bgInitialPos = background.position;
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Activate Y-follow once threshold crossed
        if (!yFollowActive && player.position.y > yThreshold)
            yFollowActive = true;

        // Target position based on player
        Vector3 targetPos = player.position + initialOffset;

        // Only follow Y if active
        if (!yFollowActive)
            targetPos.y = transform.position.y; // keep current camera Y

        // Smoothly move camera
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);

        // Optional: move background with parallax
        if (background != null)
        {
            Vector3 bgTarget = new Vector3(transform.position.x * parallaxFactor,
                                           transform.position.y * parallaxFactor,
                                           background.position.z);
            background.position = Vector3.Lerp(background.position, bgTarget, smoothSpeed * Time.deltaTime);
        }
    }
}
