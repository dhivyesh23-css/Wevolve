// Filename: CellSpawner.cs
using UnityEngine;
using System.Collections.Generic;

public class CellSpawner : MonoBehaviour
{
    public GameObject cellPrefab;
    public Transform playerTransform;
    public int numberOfCells = 20;
    public float spawnRadius = 50f;
    public float minDistanceBetween = 8f;

    [System.Serializable]
    public class CellType { public string name; public float scale; public int weight; }
    public CellType[] cellTypes = new CellType[] {
        new CellType{ name="Small", scale=1.0f, weight=60 },
        new CellType{ name="Medium", scale=1.5f, weight=40 }
    };

    private List<Vector3> spawnedPositions = new List<Vector3>();
    private List<float> spawnedRadii = new List<float>();

    void Start() {
        if (!playerTransform || !cellPrefab) { Debug.LogError("Assign Player and Cell Prefab!"); return; }
        SpawnCells();
    }

    void SpawnCells() {
        for (int i = 0; i < numberOfCells; i++) {
            int attempts = 0;
            while (attempts < 30) {
                Vector2 rand = Random.insideUnitCircle * spawnRadius;
                Vector3 pos = playerTransform.position + new Vector3(rand.x, rand.y, 0);
                CellType type = GetRandomCellType();
                float radius = (type.scale / 2f) * minDistanceBetween;
                if (!IsOverlapping(pos, radius)) {
                    GameObject cell = Instantiate(cellPrefab, pos, Quaternion.identity);
                    cell.transform.localScale = Vector3.one * type.scale;
                    spawnedPositions.Add(pos);
                    spawnedRadii.Add(radius);
                    break;
                }
                attempts++;
            }
        }
    }

    bool IsOverlapping(Vector3 pos, float radius) {
        for (int i=0; i < spawnedPositions.Count; i++) {
            if (Vector3.Distance(pos, spawnedPositions[i]) < radius + spawnedRadii[i]) return true;
        }
        return false;
    }

    CellType GetRandomCellType() {
        int totalWeight = 0;
        foreach (var type in cellTypes) totalWeight += type.weight;
        int rand = Random.Range(0, totalWeight);
        foreach (var type in cellTypes) {
            if (rand < type.weight) return type;
            rand -= type.weight;
        }
        return cellTypes[0];
    }
}