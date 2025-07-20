using UnityEngine;
using System.Collections.Generic;

public class Pathfinding : MonoBehaviour
{
    public static Pathfinding Instance;

    public LayerMask obstacleMask;
    public float nodeSpacing = 1.0f; // Distance between nodes

    void Awake()
    {
        Instance = this;
    }

    // Simple grid-based A* pathfinding
    public List<Vector3> FindPath(Vector3 start, Vector3 end)
    {
        List<Vector3> path = new List<Vector3>();
        Vector3 current = start;
        int maxIterations = 1000; // Prevent infinite loops

        for (int i = 0; i < maxIterations; i++)
        {
            if (Vector3.Distance(current, end) < nodeSpacing)
            {
                path.Add(end);
                break;
            }

            Vector3 direction = (end - current).normalized;
            Vector3 next = current + direction * nodeSpacing;

            // Check for obstacles
            if (Physics2D.OverlapCircle(next, nodeSpacing * 0.4f, obstacleMask))
            {
                // Try to step sideways if blocked
                Vector3 perp = Vector3.Cross(direction, Vector3.forward).normalized;
                Vector3 alt1 = current + (direction + perp).normalized * nodeSpacing;
                Vector3 alt2 = current + (direction - perp).normalized * nodeSpacing;

                if (!Physics2D.OverlapCircle(alt1, nodeSpacing * 0.4f, obstacleMask))
                    next = alt1;
                else if (!Physics2D.OverlapCircle(alt2, nodeSpacing * 0.4f, obstacleMask))
                    next = alt2;
                else
                    break; // No path found
            }

            path.Add(next);
            current = next;
        }

        return path;
    }
}
