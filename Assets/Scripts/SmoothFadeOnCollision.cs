using UnityEngine;

// This script makes an object transparent as long as a trigger is inside it.
public class TransparencyOnTrigger : MonoBehaviour
{
    // How transparent this object becomes (0.0 is invisible, 1.0 is opaque).
    [Range(0, 1)]
    public float transparentAlpha = 0.3f;

    private SpriteRenderer sr;

    void Start()
    {
        // Get the SpriteRenderer component on this object.
        sr = GetComponent<SpriteRenderer>();
    }

    // This is called when the Phage's trigger ENTERS the Cell's solid collider.
    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the entering object has the tag "Player".
        if (other.gameObject.CompareTag("Player"))
        {
            // Instantly make this object transparent.
            Color newColor = sr.color;
            newColor.a = transparentAlpha;
            sr.color = newColor;
        }
    }

    // This is called when the Phage's trigger EXITS the Cell's solid collider.
    void OnTriggerExit2D(Collider2D other)
    {
        // Check if the exiting object has the tag "Player".
        if (other.gameObject.CompareTag("Player"))
        {
            // Instantly make this object fully opaque again.
            Color newColor = sr.color;
            newColor.a = 1f;
            sr.color = newColor;
        }
    }
}