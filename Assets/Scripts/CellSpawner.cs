// Filename: CellSpawner.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Procedurally spawns cells in a specified radius around a target (the player), ensuring they do not overlap.
/// </summary>
public class CellSpawner : MonoBehaviour
{
    [Header("Spawning Configuration")]
    public GameObject cellPrefab;
    public Transform playerTransform;
    public int numberOfCellsToSpawn = 20;
    public float spawnRadius = 50f;
    public float minDistanceBetweenCells = 5f;

    [Header("Cell Size Variation")]
    public Vector2 scaleRange = new Vector2(0.8f, 1.5f);

    private List<Vector3> spawnedCellPositions = new List<Vector3>();

    void Start()
    {
        if (playerTransform == null || cellPrefab == null)
        {
            Debug.LogError("Player Transform or Cell Prefab is not assigned in the CellSpawner!");
            return;
        }
        SpawnCells();
    }

    void SpawnCells()
    {
        for (int i = 0; i < numberOfCellsToSpawn; i++)
        {
            int attempts = 0;
            bool positionFound = false;
            Vector3 potentialPosition = Vector3.zero;

            while (!positionFound && attempts < 30)
            {
                Vector2 randomPointInCircle = Random.insideUnitCircle * spawnRadius;
                potentialPosition = playerTransform.position + new Vector3(randomPointInCircle.x, randomPointInCircle.y, 0);

                if (!IsOverlapping(potentialPosition))
                {
                    positionFound = true;
                }
                attempts++;
            }

            if (positionFound)
            {
                GameObject newCell = Instantiate(cellPrefab, potentialPosition, Quaternion.identity);

                float randomScaleX = Random.Range(scaleRange.x, scaleRange.y);
                float randomScaleY = (Random.value > 0.3f) ? Random.Range(scaleRange.x, scaleRange.y) : randomScaleX; 
                newCell.transform.localScale = new Vector3(randomScaleX, randomScaleY, 1f);
                
                // --- FIXED: Removed the unnecessary call to SetClearanceRadius ---
                
                spawnedCellPositions.Add(potentialPosition);
            }
        }
    }

    bool IsOverlapping(Vector3 position)
    {
        foreach (Vector3 spawnedPos in spawnedCellPositions)
        {
            if (Vector3.Distance(position, spawnedPos) < minDistanceBetweenCells)
            {
                return true;
            }
        }
        return false;
    }
}