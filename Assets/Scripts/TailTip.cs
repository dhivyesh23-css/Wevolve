using UnityEngine;

public class TailTip : MonoBehaviour
{
    public TailController tailController;

    void OnCollisionEnter2D(Collision2D collision)
    {
        // MODIFIED: Pass the entire collision object to the controller for sliding logic
        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            if (tailController != null)
            {
                tailController.HandleWallCollision(collision);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Nucleus"))
        {
            if (tailController != null)
            {
                tailController.CutTail();
            }
        }
    }
}