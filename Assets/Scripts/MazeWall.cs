using UnityEngine;

// This script will be automatically added to each wall piece by the generator.
[RequireComponent(typeof(MeshRenderer))]
public class MazeWall : MonoBehaviour
{
    private MeshRenderer meshRenderer;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        // Start with the wall invisible.
        meshRenderer.enabled = false;
    }

    public void Reveal()
    {
        meshRenderer.enabled = true;
    }

    public void Hide()
    {
        meshRenderer.enabled = false;
    }
}