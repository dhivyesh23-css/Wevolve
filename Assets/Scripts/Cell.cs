// Filename: Cell.cs
using UnityEngine;

/// <summary>
/// Main component for the Cell prefab.
/// It holds a reference to the MazeGenerator to create the internal maze.
/// </summary>
[RequireComponent(typeof(MazeGenerator))]
[RequireComponent(typeof(VisualController))]
public class Cell : MonoBehaviour
{
    // --- Reference to the maze script ---
    private MazeGenerator mazeGenerator;

    void Awake()
    {
        // Get the MazeGenerator component
        mazeGenerator = GetComponent<MazeGenerator>();
        if (mazeGenerator == null)
        {
            Debug.LogError("MazeGenerator script could not be found on this Cell prefab!", this.gameObject);
        }
    }

    void Start()
    {
        // --- FIXED: Tell the maze generator to build the maze ---
        // The Generate() method is self-contained and doesn't need any parameters.
        if (mazeGenerator != null)
        {
            mazeGenerator.Generate();
        }
    }
}