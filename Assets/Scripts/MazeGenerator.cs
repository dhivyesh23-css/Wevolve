//
// Filename: MazeGenerator.cs
//
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generates a 2D circular maze with a single mesh and a 2D polygon collider for physics.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(PolygonCollider2D))]
public class MazeGenerator : MonoBehaviour
{
    [Header("Maze Structure")]
    [Tooltip("Number of concentric rings in the maze.")]
    [Range(2, 50)]
    public int numberOfRings = 10;

    [Tooltip("Radius of the innermost ring (the central goal area).")]
    public float nucleusRadius = 1.5f;

    [Tooltip("How much space each subsequent ring adds.")]
    public float ringWidth = 1.0f;

    [Tooltip("The thickness of the generated maze walls.")]
    public float wallThickness = 0.1f;

    [Header("Entrances & Goal")]
    [Tooltip("Number of entrances on the outermost wall.")]
    [Range(1, 10)]
    public int numberOfEntrances = 4;

    [Header("Generation")]
    [Tooltip("Set to 0 for a random seed.")]
    public int seed = 0;

    // --- Private members ---
    private class CircularMazeCell
    {
        public int row, col;
        public bool visited = false;
        public bool wallClockwise = true;
        public bool wallOutward = true;
    }

    private CircularMazeCell[,] _grid;
    private MeshFilter _meshFilter;
    private PolygonCollider2D _polygonCollider;
    private System.Random _rng;

    void Start()
    {
        Generate();
    }

    public void Generate()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _polygonCollider = GetComponent<PolygonCollider2D>();

        _rng = (seed == 0) ? new System.Random() : new System.Random(seed);
        
        // Clear previous collider paths
        _polygonCollider.pathCount = 0;

        InitializeGrid();
        CarveMazePaths();
        CreateOpenings();
        BuildMazeMeshAndCollider();
    }
    
    // ---
    // ### STEP 1: SETUP GRID
    // ---
    private void InitializeGrid()
    {
        var ringCellCounts = new List<int>();
        float currentRadius = nucleusRadius;

        for (int i = 0; i < numberOfRings; i++)
        {
            float circumference = 2 * Mathf.PI * currentRadius;
            int cellsInRing = Mathf.RoundToInt(circumference / ringWidth);
            ringCellCounts.Add(cellsInRing);
            currentRadius += ringWidth;
        }
        
        int maxCols = 0;
        foreach(var count in ringCellCounts) if (count > maxCols) maxCols = count;

        _grid = new CircularMazeCell[numberOfRings, maxCols];

        for (int i = 0; i < numberOfRings; i++)
        {
            for (int j = 0; j < ringCellCounts[i]; j++)
            {
                _grid[i, j] = new CircularMazeCell { row = i, col = j };
            }
        }
    }
    
    // ---
    // ### STEP 2: CARVE MAZE (DFS)
    // ---
    private void CarveMazePaths()
    {
        var stack = new Stack<CircularMazeCell>();
        var startCell = _grid[0, _rng.Next(0, GetCellCountInRing(0))];
        startCell.visited = true;
        stack.Push(startCell);

        while (stack.Count > 0)
        {
            var currentCell = stack.Pop();
            var neighbors = GetUnvisitedNeighbors(currentCell);

            if (neighbors.Count > 0)
            {
                stack.Push(currentCell);
                var randomNeighbor = neighbors[_rng.Next(0, neighbors.Count)];
                RemoveWall(currentCell, randomNeighbor);
                randomNeighbor.visited = true;
                stack.Push(randomNeighbor);
            }
        }
    }

    private List<CircularMazeCell> GetUnvisitedNeighbors(CircularMazeCell cell)
    {
        var neighbors = new List<CircularMazeCell>();
        int r = cell.row;
        int c = cell.col;
        int cellsInCurrentRing = GetCellCountInRing(r);

        // Clockwise
        int clockwiseCol = (c + 1) % cellsInCurrentRing;
        if (!_grid[r, clockwiseCol].visited) neighbors.Add(_grid[r, clockwiseCol]);
        
        // Counter-Clockwise
        int counterClockwiseCol = (c - 1 + cellsInCurrentRing) % cellsInCurrentRing;
        if (!_grid[r, counterClockwiseCol].visited) neighbors.Add(_grid[r, counterClockwiseCol]);

        // Outward
        if (r < numberOfRings - 1)
        {
            int cellsInOuterRing = GetCellCountInRing(r + 1);
            int outerCol = Mathf.RoundToInt((float)c / cellsInCurrentRing * cellsInOuterRing);
            outerCol = Mathf.Clamp(outerCol, 0, cellsInOuterRing - 1);
            if (_grid[r + 1, outerCol] != null && !_grid[r + 1, outerCol].visited)
            {
                neighbors.Add(_grid[r + 1, outerCol]);
            }
        }
        
        // Inward
        if (r > 0)
        {
            int cellsInInnerRing = GetCellCountInRing(r - 1);
            int innerCol = Mathf.RoundToInt((float)c / cellsInCurrentRing * cellsInInnerRing);
            innerCol = Mathf.Clamp(innerCol, 0, cellsInInnerRing - 1);
             if (_grid[r - 1, innerCol] != null && !_grid[r - 1, innerCol].visited)
            {
                neighbors.Add(_grid[r - 1, innerCol]);
            }
        }
        return neighbors;
    }
    
    private void RemoveWall(CircularMazeCell from, CircularMazeCell to)
    {
        if (to.row > from.row) from.wallOutward = false;
        else if (to.row < from.row) to.wallOutward = false;
        else if ((to.col > from.col || (to.col == 0 && from.col == GetCellCountInRing(from.row) - 1))) from.wallClockwise = false;
        else to.wallClockwise = false;
    }

    // ---
    // ### STEP 3: CREATE ENTRANCES
    // ---
    private void CreateOpenings()
    {
        for (int i = 0; i < numberOfEntrances; i++)
        {
            int entranceRing = numberOfRings - 1;
            int entranceCol = _rng.Next(0, GetCellCountInRing(entranceRing));
            if (_grid[entranceRing, entranceCol] != null)
            {
                _grid[entranceRing, entranceCol].wallOutward = false;
            }
        }
    }

    // ---
    // ### STEP 4: BUILD MESH AND 2D COLLIDER
    // ---
    private void BuildMazeMeshAndCollider()
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var colliderPaths = new List<Vector2[]>();

        for (int r = 0; r < numberOfRings; r++)
        {
            int cellsInRing = GetCellCountInRing(r);
            if (cellsInRing == 0) continue;

            float innerRadius = nucleusRadius + r * ringWidth;
            float outerRadius = innerRadius + ringWidth;
            float angleStep = 360.0f / cellsInRing;

            for (int c = 0; c < cellsInRing; c++)
            {
                var cell = _grid[r, c];
                float startAngle = c * angleStep;

                if (cell.wallOutward)
                {
                    int segments = 5;
                    for (int i = 0; i < segments; i++)
                    {
                        float ang1 = startAngle + (i * (angleStep / segments));
                        float ang2 = startAngle + ((i + 1) * (angleStep / segments));
                        var v = CreateArcQuad(outerRadius, wallThickness, ang1, ang2);
                        AddGeometry(vertices, triangles, colliderPaths, v);
                    }
                }

                if (cell.wallClockwise)
                {
                    float angle = (c + 1) * angleStep;
                    var v = CreateRadialQuad(innerRadius, outerRadius, angle, wallThickness);
                    AddGeometry(vertices, triangles, colliderPaths, v);
                }
            }
        }

        var mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        _meshFilter.mesh = mesh;
        
        _polygonCollider.pathCount = colliderPaths.Count;
        for(int i=0; i < colliderPaths.Count; i++)
        {
            _polygonCollider.SetPath(i, colliderPaths[i]);
        }
    }

    // ---
    // ### HELPER METHODS
    // ---
    private int GetCellCountInRing(int ringIndex)
    {
        int count = 0;
        if (ringIndex < 0 || ringIndex >= _grid.GetLength(0)) return 0;
        for (int c = 0; c < _grid.GetLength(1); c++)
        {
            if (_grid[ringIndex, c] != null) count++;
            else break;
        }
        return count;
    }

    private Vector3[] CreateArcQuad(float radius, float thickness, float a1, float a2) {
        return new Vector3[] {
            PointOnCircle(radius - thickness/2, a1), PointOnCircle(radius + thickness/2, a1),
            PointOnCircle(radius - thickness/2, a2), PointOnCircle(radius + thickness/2, a2)
        };
    }

    private Vector3[] CreateRadialQuad(float r1, float r2, float angle, float thickness) {
        Vector3 c1 = PointOnCircle(r1, angle);
        Vector3 c2 = PointOnCircle(r2, angle);
        Vector3 offset = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0, -Mathf.Cos(angle * Mathf.Deg2Rad)) * thickness;
        return new Vector3[] { c1 - offset/2, c1 + offset/2, c2 - offset/2, c2 + offset/2 };
    }

    private Vector3 PointOnCircle(float radius, float angleDeg) {
        // Using X and Y for 2D plane, Z will be 0
        return new Vector3(
            radius * Mathf.Cos(angleDeg * Mathf.Deg2Rad), 
            radius * Mathf.Sin(angleDeg * Mathf.Deg2Rad), 
            0
        );
    }

    private void AddGeometry(List<Vector3> v, List<int> t, List<Vector2[]> p, Vector3[] q) {
        int index = v.Count;
        v.AddRange(q);
        t.AddRange(new int[] { index, index + 2, index + 1, index + 2, index + 3, index + 1 });
        // The Z component is ignored when converting Vector3 to Vector2 for the collider.
        p.Add(new Vector2[] { q[0], q[2], q[3], q[1] });
    }
}