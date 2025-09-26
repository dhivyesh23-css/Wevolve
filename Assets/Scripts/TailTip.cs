// Filename: TailTip.cs
using UnityEngine;

public class TailTip : MonoBehaviour
{
    public TailController tailController;

    // We now use OnTriggerEnter2D because the collider is set to "Is Trigger"
    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if we hit the nucleus
        if (other.CompareTag("Nucleus"))
        {
            // If so, cut the tail and finish
            tailController.CutTail();
        }
        // Check if we hit a maze wall (which is on the "Ground" layer)
        else if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            // If so, just stop the tail from extending further
            tailController.StopExtension();
        }
    }
}