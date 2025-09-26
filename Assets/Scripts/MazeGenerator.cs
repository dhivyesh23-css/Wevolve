// Filename: MazeGenerator.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Needed for the curviness logic

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(PolygonCollider2D))]
public class MazeGenerator : MonoBehaviour
{
    [Header("Maze Sizing (World Units)")]
    [Tooltip("The total outer radius of the maze. Should match half your cell's width.")]
    public float totalRadius = 5f;
    [Tooltip("The radius of the central goal area. Should match half your nucleus's width.")]
    public float centerRadius = 0.375f;
    [Tooltip("Creates a gap between the totalRadius and the outermost maze wall.")]
    public float outerPadding = 0.5f;

    [Header("Maze Structure")]
    [Range(2, 25)] public int numberOfRings = 5;
    [Tooltip("Higher values create longer, more circular paths.")]
    [Range(0f, 1f)] public float curviness = 0.75f;
    [Tooltip("How many segments to use for each curved wall. Higher is smoother.")]
    [Range(1, 20)] public int arcResolution = 5;
    public float wallThickness = 0.1f;
    [Range(1, 10)] public int numberOfEntrances = 1;
    [Tooltip("Set to 0 for a random seed.")] public int seed = 0;
    
    [Header("Visuals")]
    [Tooltip("The material to apply to each maze wall.")]
    public Material wallMaterial; // <-- THE SLOT TO ASSIGN YOUR MATERIAL

    // --- Private Variables ---
    private class CircularMazeCell { public bool visited, wallClockwise = true, wallOutward = true; }
    private CircularMazeCell[,] _grid;
    private int[] _ringCellCounts;
    private System.Random _rng;
    private float _nucleusRadius;
    private float _ringWidth;

    [ContextMenu("Generate Maze")]
    public void Generate() {
        _rng = (seed == 0) ? new System.Random() : new System.Random(seed);
        GetComponent<PolygonCollider2D>().pathCount = 0;
        GetComponent<MeshFilter>().mesh = null;

        InitializeGrid();
        CarveMazePaths();
        CreateOpenings();
        BuildMazeObjects();
    }

    private void InitializeGrid() {
        float effectiveRadius = totalRadius - outerPadding;
        _nucleusRadius = centerRadius;
        _ringWidth = (effectiveRadius - centerRadius) / numberOfRings;

        _ringCellCounts = new int[numberOfRings];
        float currentRadius = _nucleusRadius + _ringWidth / 2f;
        int maxCols = 0;

        for (int i = 0; i < numberOfRings; i++) {
            float circumference = 2 * Mathf.PI * currentRadius;
            _ringCellCounts[i] = Mathf.Max(4, Mathf.RoundToInt(circumference / _ringWidth));
            if (_ringCellCounts[i] > maxCols) maxCols = _ringCellCounts[i];
            currentRadius += _ringWidth;
        }

        _grid = new CircularMazeCell[numberOfRings, maxCols];
        for (int i = 0; i < numberOfRings; i++) {
            for (int j = 0; j < _ringCellCounts[i]; j++) {
                _grid[i, j] = new CircularMazeCell();
            }
        }
    }
    
    private void BuildMazeObjects() {
        Transform wallContainer = transform.Find("Walls");
        if (wallContainer != null) DestroyImmediate(wallContainer.gameObject);
        wallContainer = new GameObject("Walls").transform;
        wallContainer.parent = transform;
        wallContainer.localPosition = Vector3.zero;

        int cellsInFirstRing = GetCellCountInRing(0);
        int nucleusOpeningCell = _rng.Next(0, cellsInFirstRing);
        for (int c = 0; c < cellsInFirstRing; c++) {
            if (c == nucleusOpeningCell) continue;
            float thetaStart = (c / (float)cellsInFirstRing) * 2 * Mathf.PI;
            float thetaEnd = ((c + 1) / (float)cellsInFirstRing) * 2 * Mathf.PI;
            CreateWallObject(_nucleusRadius, thetaStart, thetaEnd, true, $"NucleusWall_{c}", wallContainer);
        }

        for (int r = 0; r < numberOfRings; r++) {
            int cellsInRing = GetCellCountInRing(r);
            for (int c = 0; c < cellsInRing; c++) {
                if (_grid[r, c] == null) continue;
                float innerRadius = _nucleusRadius + r * _ringWidth;
                float outerRadius = innerRadius + _ringWidth;
                float thetaStart = (c / (float)cellsInRing) * 2 * Mathf.PI;
                float thetaEnd = ((c + 1) / (float)cellsInRing) * 2 * Mathf.PI;
                if (_grid[r, c].wallClockwise) CreateWallObject(innerRadius, outerRadius, thetaEnd, false, $"Wall_R_{r}_{c}", wallContainer);
                if (_grid[r, c].wallOutward) CreateWallObject(outerRadius, thetaStart, thetaEnd, true, $"Wall_A_{r}_{c}", wallContainer);
            }
        }
    }

    private void CreateWallObject(float val1, float val2, float val3, bool isArc, string name, Transform parent) {
        GameObject wallGO = new GameObject(name);
        wallGO.transform.parent = parent;
        wallGO.layer = LayerMask.NameToLayer("Ground");
        
        var mf = wallGO.AddComponent<MeshFilter>();
        var mr = wallGO.AddComponent<MeshRenderer>();
        var pc = wallGO.AddComponent<PolygonCollider2D>();
        wallGO.AddComponent<MazeWall>();

        mr.material = wallMaterial;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        Vector2[] colliderPath;

        if (isArc) {
            float radius = val1, thetaStart = val2, thetaEnd = val3;
            float r_inner = radius - wallThickness / 2f;
            float r_outer = radius + wallThickness / 2f;

            for (int i = 0; i < arcResolution; i++) {
                float t1 = (float)i / arcResolution;
                float t2 = (float)(i + 1) / arcResolution;
                float angle1 = Mathf.Lerp(thetaStart, thetaEnd, t1);
                float angle2 = Mathf.Lerp(thetaStart, thetaEnd, t2);
                int vertIndex = i * 4;
                vertices.Add(PolarToCartesian(r_inner, angle1));
                vertices.Add(PolarToCartesian(r_outer, angle1));
                vertices.Add(PolarToCartesian(r_outer, angle2));
                vertices.Add(PolarToCartesian(r_inner, angle2));
                triangles.Add(vertIndex); triangles.Add(vertIndex + 1); triangles.Add(vertIndex + 2);
                triangles.Add(vertIndex); triangles.Add(vertIndex + 2); triangles.Add(vertIndex + 3);
            }
            
            colliderPath = new Vector2[arcResolution + 1];
            for (int i = 0; i <= arcResolution; i++) {
                colliderPath[i] = PolarToCartesian(radius, Mathf.Lerp(thetaStart, thetaEnd, (float)i/arcResolution));
            }
        } else {
            float innerRadius = val1, outerRadius = val2, theta = val3;
            float halfThick_inner = Mathf.Asin((wallThickness / 2f) / innerRadius);
            float halfThick_outer = Mathf.Asin((wallThickness / 2f) / outerRadius);
            vertices.Add(PolarToCartesian(innerRadius, theta - halfThick_inner));
            vertices.Add(PolarToCartesian(innerRadius, theta + halfThick_inner));
            vertices.Add(PolarToCartesian(outerRadius, theta + halfThick_outer));
            vertices.Add(PolarToCartesian(outerRadius, theta - halfThick_outer));
            triangles.AddRange(new int[] { 0, 1, 2, 0, 2, 3 });
            colliderPath = new Vector2[] { PolarToCartesian(innerRadius, theta), PolarToCartesian(outerRadius, theta) };
        }

        Mesh mesh = new Mesh { vertices = vertices.ToArray(), triangles = triangles.ToArray() };
        mesh.RecalculateNormals();
        mf.mesh = mesh;
        pc.SetPath(0, colliderPath);
    }

    #region Pathfinding Logic
    private List<(int nR, int nC, char dir)> GetUnvisitedNeighbors(int r, int c) {
        var circularNeighbors = new List<(int, int, char)>();
        var radialNeighbors = new List<(int, int, char)>();
        int cellsInRing = GetCellCountInRing(r);
        int cwC = (c + 1) % cellsInRing;
        if (!_grid[r, cwC].visited) circularNeighbors.Add((r, cwC, 'C'));
        int ccwC = (c - 1 + cellsInRing) % cellsInRing;
        if (!_grid[r, ccwC].visited) circularNeighbors.Add((r, ccwC, 'A'));
        if (r < numberOfRings - 1) {
            int cellsInOuterRing = GetCellCountInRing(r + 1);
            float proportion = (c + 0.5f) / cellsInRing;
            int outwardC = Mathf.FloorToInt(proportion * cellsInOuterRing);
            if (!_grid[r + 1, outwardC].visited) radialNeighbors.Add((r + 1, outwardC, 'O'));
        }
        if (r > 0) {
            int cellsInInnerRing = GetCellCountInRing(r - 1);
            float proportion = (c + 0.5f) / cellsInRing;
            int inwardC = Mathf.FloorToInt(proportion * cellsInInnerRing);
            if (!_grid[r - 1, inwardC].visited) radialNeighbors.Add((r - 1, inwardC, 'I'));
        }

        if (_rng.NextDouble() < curviness && circularNeighbors.Any()) {
            return circularNeighbors;
        }

        return circularNeighbors.Concat(radialNeighbors).ToList();
    }

    private void CarveMazePaths() {
        var stack = new Stack<(int r, int c)>();
        int startR = 0;
        int startC = _rng.Next(0, GetCellCountInRing(startR));
        _grid[startR, startC].visited = true;
        stack.Push((startR, startC));
        while (stack.Count > 0) {
            (int r, int c) = stack.Peek();
            var neighbors = GetUnvisitedNeighbors(r, c);
            if (neighbors.Count > 0) {
                var (nR, nC, dir) = neighbors[_rng.Next(0, neighbors.Count)];
                if (dir == 'O') _grid[r, c].wallOutward = false;
                else if (dir == 'I') _grid[nR, nC].wallOutward = false;
                else if (dir == 'C') _grid[r, c].wallClockwise = false;
                else _grid[nR, nC].wallClockwise = false;
                _grid[nR, nC].visited = true;
                stack.Push((nR, nC));
            } else { stack.Pop(); }
        }
    }
    
    private void CreateOpenings() {
        int outerRing = numberOfRings - 1;
        int cellsInOuterRing = GetCellCountInRing(outerRing);
        for (int i = 0; i < numberOfEntrances; i++) {
            int c = _rng.Next(0, cellsInOuterRing);
            if (_grid[outerRing, c] != null) _grid[outerRing, c].wallOutward = false;
        }
    }
    #endregion
    
    #region Helpers
    private int GetCellCountInRing(int r) => _ringCellCounts[r];
    private Vector2 PolarToCartesian(float radius, float theta) => new Vector2(radius * Mathf.Cos(theta), radius * Mathf.Sin(theta));
    #endregion
}