using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(EdgeCollider2D))]
public class LineRendererCollisionAdapter : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private EdgeCollider2D edgeCollider;

    void Awake()
    {
        // Get references to the LineRenderer and EdgeCollider2D on this GameObject.
        lineRenderer = GetComponent<LineRenderer>();
        edgeCollider = GetComponent<EdgeCollider2D>();
        
        // Ensure the collider is a trigger so it doesn't block movement,
        // and its position is reset to (0,0) so it's relative to the LineRenderer's positions.
        edgeCollider.isTrigger = true;
        edgeCollider.offset = Vector2.zero;
    }

    /// <summary>
    /// Updates the EdgeCollider2D's points to match the current positions of the LineRenderer.
    /// This method must be called whenever the LineRenderer's points change.
    /// </summary>
    public void UpdateColliderFromLine()
    {
        // Get the positions from the LineRenderer.
        Vector3[] linePositions = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(linePositions);

        // Convert the Vector3 array to a Vector2 array for the EdgeCollider2D.
        Vector2[] colliderPoints = new Vector2[linePositions.Length];
        for (int i = 0; i < linePositions.Length; i++)
        {
            // The positions must be in local space for the EdgeCollider2D.
            // Converting from world space to local space relative to the LineRenderer's transform.
            colliderPoints[i] = transform.InverseTransformPoint(linePositions[i]);
        }

        // Set the points on the EdgeCollider2D.
        edgeCollider.points = colliderPoints;
    }
}