using UnityEngine;

// Add this script to your Bacteriophage player object.
public class WallDetector : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        // When a wall enters our trigger area, try to get its MazeWall component.
        MazeWall wall = other.GetComponent<MazeWall>();
        if (wall != null)
        {
            // If it's a valid wall, reveal it.
            wall.Reveal();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        // When a wall leaves our trigger area, hide it again.
        MazeWall wall = other.GetComponent<MazeWall>();
        if (wall != null)
        {
            wall.Hide();
        }
    }
}