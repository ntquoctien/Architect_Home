using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure C# data class that stores room geometry as a list of corner points on the XZ plane.
/// Provides methods to add, insert, remove, and move corners with validation.
/// </summary>
public class RoomData
{
    /// <summary>
    /// Height of the room's walls.
    /// </summary>
    public float RoomHeight { get; private set; }

    /// <summary>
    /// List of corner positions in world space (XZ plane, Y is typically 0).
    /// Points should be ordered in a counter-clockwise direction.
    /// </summary>
    public List<Vector3> Corners { get; private set; }

    /// <summary>
    /// Event fired whenever the room geometry changes (corners added/removed/moved) or height changes.
    /// </summary>
    public event Action OnGeometryChanged;

    /// <summary>
    /// Constructor initializes with a default rectangular room.
    /// </summary>
    /// <param name="width">Width of the initial room</param>
    /// <param name="length">Length of the initial room</param>
    /// <param name="height">Height of the room (wall height)</param>
    /// <param name="centerPosition">Center position of the room</param>
    public RoomData(float width = 5f, float length = 5f, float height = 3f, Vector3 centerPosition = default)
    {
        Corners = new List<Vector3>();
        RoomHeight = height;
        InitializeRectangularRoom(width, length, centerPosition);
    }

    /// <summary>
    /// Initializes a rectangular room with 4 corners.
    /// </summary>
    private void InitializeRectangularRoom(float width, float length, Vector3 center)
    {
        float halfWidth = width * 0.5f;
        float halfLength = length * 0.5f;

        Corners.Clear();
        Corners.Add(new Vector3(center.x - halfWidth, 0, center.z - halfLength)); // Bottom-left
        Corners.Add(new Vector3(center.x + halfWidth, 0, center.z - halfLength)); // Bottom-right
        Corners.Add(new Vector3(center.x + halfWidth, 0, center.z + halfLength)); // Top-right
        Corners.Add(new Vector3(center.x - halfWidth, 0, center.z + halfLength)); // Top-left
    }

    /// <summary>
    /// Adds a new corner at the end of the list.
    /// </summary>
    public void AddCorner(Vector3 position)
    {
        position.y = 0; // Ensure on XZ plane
        Corners.Add(position);
        
        if (ValidatePolygon())
        {
            OnGeometryChanged?.Invoke();
        }
        else
        {
            Corners.RemoveAt(Corners.Count - 1);
            Debug.LogWarning("Cannot add corner: Would create self-intersecting polygon");
        }
    }

    /// <summary>
    /// Inserts a corner at a specific index.
    /// </summary>
    public void InsertCorner(int index, Vector3 position)
    {
        if (index < 0 || index > Corners.Count)
        {
            Debug.LogWarning($"Invalid index {index} for insertion");
            return;
        }

        position.y = 0;
        Corners.Insert(index, position);
        
        if (ValidatePolygon())
        {
            OnGeometryChanged?.Invoke();
        }
        else
        {
            Corners.RemoveAt(index);
            Debug.LogWarning("Cannot insert corner: Would create self-intersecting polygon");
        }
    }

    /// <summary>
    /// Removes a corner at the specified index.
    /// Minimum 3 corners required to maintain a valid polygon.
    /// </summary>
    public bool RemoveCorner(int index)
    {
        if (Corners.Count <= 3)
        {
            Debug.LogWarning("Cannot remove corner: Minimum 3 corners required");
            return false;
        }

        if (index < 0 || index >= Corners.Count)
        {
            Debug.LogWarning($"Invalid index {index} for removal");
            return false;
        }

        Vector3 removedCorner = Corners[index];
        Corners.RemoveAt(index);
        
        if (ValidatePolygon())
        {
            OnGeometryChanged?.Invoke();
            return true;
        }
        else
        {
            Corners.Insert(index, removedCorner);
            Debug.LogWarning("Cannot remove corner: Would create invalid polygon");
            return false;
        }
    }

    /// <summary>
    /// Moves an existing corner to a new position.
    /// </summary>
    public bool MoveCorner(int index, Vector3 newPosition)
    {
        if (index < 0 || index >= Corners.Count)
        {
            Debug.LogWarning($"Invalid index {index} for moving");
            return false;
        }

        newPosition.y = 0;
        Vector3 oldPosition = Corners[index];
        Corners[index] = newPosition;
        
        if (ValidatePolygon())
        {
            OnGeometryChanged?.Invoke();
            return true;
        }
        else
        {
            Corners[index] = oldPosition;
            return false;
        }
    }

    /// <summary>
    /// Updates the room height and triggers regeneration.
    /// </summary>
    public void SetRoomHeight(float newHeight)
    {
        if (Mathf.Approximately(RoomHeight, newHeight)) return;

        RoomHeight = newHeight;
        OnGeometryChanged?.Invoke();
    }

    /// <summary>
    /// Gets the edge segment (start, end) for a given edge index.
    /// </summary>
    public (Vector3 start, Vector3 end) GetEdge(int edgeIndex)
    {
        int nextIndex = (edgeIndex + 1) % Corners.Count;
        return (Corners[edgeIndex], Corners[nextIndex]);
    }

    /// <summary>
    /// Calculates the length of an edge.
    /// </summary>
    public float GetEdgeLength(int edgeIndex)
    {
        var (start, end) = GetEdge(edgeIndex);
        return Vector3.Distance(start, end);
    }

    /// <summary>
    /// Validates that the polygon does not self-intersect.
    /// Uses a simple line segment intersection check.
    /// </summary>
    private bool ValidatePolygon()
    {
        if (Corners.Count < 3) return false;

        // Check for self-intersection
        for (int i = 0; i < Corners.Count; i++)
        {
            Vector2 a1 = new Vector2(Corners[i].x, Corners[i].z);
            Vector2 a2 = new Vector2(Corners[(i + 1) % Corners.Count].x, Corners[(i + 1) % Corners.Count].z);

            for (int j = i + 2; j < Corners.Count; j++)
            {
                // Don't check adjacent edges
                if (j == (i + Corners.Count - 1) % Corners.Count) continue;

                Vector2 b1 = new Vector2(Corners[j].x, Corners[j].z);
                Vector2 b2 = new Vector2(Corners[(j + 1) % Corners.Count].x, Corners[(j + 1) % Corners.Count].z);

                if (LineSegmentsIntersect(a1, a2, b1, b2))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if two line segments intersect (excluding endpoints).
    /// </summary>
    private bool LineSegmentsIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
    {
        float d = (a2.x - a1.x) * (b2.y - b1.y) - (a2.y - a1.y) * (b2.x - b1.x);
        if (Mathf.Abs(d) < 0.0001f) return false; // Parallel

        float t = ((b1.x - a1.x) * (b2.y - b1.y) - (b1.y - a1.y) * (b2.x - b1.x)) / d;
        float u = ((b1.x - a1.x) * (a2.y - a1.y) - (b1.y - a1.y) * (a2.x - a1.x)) / d;

        // Check if intersection occurs within both segments (excluding exact endpoints)
        return t > 0.001f && t < 0.999f && u > 0.001f && u < 0.999f;
    }

    /// <summary>
    /// Calculates the total perimeter of the room.
    /// </summary>
    public float GetPerimeter()
    {
        float perimeter = 0f;
        for (int i = 0; i < Corners.Count; i++)
        {
            perimeter += GetEdgeLength(i);
        }
        return perimeter;
    }

    /// <summary>
    /// Calculates the approximate area using the shoelace formula.
    /// </summary>
    public float GetArea()
    {
        float area = 0f;
        for (int i = 0; i < Corners.Count; i++)
        {
            int j = (i + 1) % Corners.Count;
            area += Corners[i].x * Corners[j].z;
            area -= Corners[j].x * Corners[i].z;
        }
        return Mathf.Abs(area) * 0.5f;
    }
}
