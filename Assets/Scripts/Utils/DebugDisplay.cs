using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugDisplay : MonoBehaviour
{
    private static string _queuedLines = "";
    private static string _queuedFixedLines = "";

    private static DebugDisplay _instance = null;

    private string _displayedLines = "";
    private string _displayedFixedLines = "";

    public static void PrintLine(string line)
    {
        // Don't print anything if there's no instance of DebugDisplay
        // in the scene.  This prevents lines from queueing up indefinitely
        // and causing a memory leak.
        if (_instance == null)
            return;

        if (Time.inFixedTimeStep)
            _queuedFixedLines += line + "\n";
        else
            _queuedLines += line + "\n";
    }

    public void Awake()
    {
        _instance = this;
    }

    public void Update()
    {
        _displayedLines = _queuedLines;
        _queuedLines = "";
    }

    public void FixedUpdate()
    {
        _displayedFixedLines = _queuedFixedLines;
        _queuedFixedLines = "";
    }


    // --- Other utility functions ---

    public static void DrawRay(Color color, Vector3 origin, Vector3 dir, float length)
    {
        var end = origin + (dir * length);
        Debug.DrawLine(origin, end, color);
    }

    public static void DrawCube(
        Color color,
        Vector3 center,
        Vector3 halfExtents,
        Quaternion? orientation = null
    )
    {
        if (orientation == null)
            orientation = Quaternion.identity;

        // Generate all the corners
        Vector3[] corners = CubeCorners(center, halfExtents, orientation.Value);

        // Draw a line between each corner and every other corner
        ConnectCorners(corners, color);
    }

    public static void DrawPoint(Color color, Vector3 point, float radius = 0.1f)
    {
        DrawCube(color, point, radius * Vector3.one);
    }

    public static void DrawBoxCast(
        Color color,
        Vector3 origin,
        Vector3 halfExtents,
        Vector3 direction,
        float maxDistance,
        Quaternion? boxOrientation = null
    )
    {
        if (!boxOrientation.HasValue)
            boxOrientation = Quaternion.identity;

        var startCorners = CubeCorners(
            origin,
            halfExtents,
            boxOrientation.Value
        );
        var endCorners = CubeCorners(
            origin + (direction * maxDistance),
            halfExtents,
            boxOrientation.Value
        );

        // Draw the start and end boxes
        ConnectCorners(startCorners, color);
        ConnectCorners(endCorners, color);

        // Draw lines connecting the two boxes
        ConnectParallel(startCorners, endCorners, color);
    }

    public static void DrawCircleCast(
        Color color,
        Vector3 origin,
        float radius,
        float maxDistance,
        Vector3 direction
    )
    {
        var startCorners = CircleCorners(
            origin,
            radius,
            12,
            direction
        );
        var endCorners = CircleCorners(
            origin + (direction * maxDistance),
            radius,
            12,
            direction
        );

        ConnectCorners(startCorners, color);
        ConnectCorners(endCorners, color);
        ConnectParallel(startCorners, endCorners, color);
    }

    private static Vector3[] CubeCorners(
        Vector3 center,
        Vector3 halfExtents,
        Quaternion orientation
    )
    {
        // Generate all the corners
        Vector3 startCorner = center - halfExtents;
        Vector3[] corners = new[]
        {
            new Vector3(-1, 1, 1),
            new Vector3(1, 1, 1),
            new Vector3(-1, 1, -1),
            new Vector3(1, 1, -1),
            new Vector3(-1, -1, 1),
            new Vector3(1, -1, 1),
            new Vector3(-1, -1, -1),
            new Vector3(1, -1, -1),
        };
        for (int i = 0; i < corners.Length; i++)
        {
            corners[i].x *= halfExtents.x;
            corners[i].y *= halfExtents.y;
            corners[i].z *= halfExtents.z;

            corners[i] = orientation * corners[i];
            corners[i] += center;
        }

        return corners;
    }

    private static Vector3[] CircleCorners(
        Vector3 center,
        float radius,
        int numCorners,
        Vector3 normal
    )
    {
        var rot = Quaternion.LookRotation(normal);
        var up    = rot * Vector3.up;
        var right = rot * Vector3.right;
        
        var corners = new Vector3[numCorners];
        for (int i = 0; i < numCorners; i++)
        {
            float angleDeg = (360 * i) / numCorners;
            float angleRad = Mathf.Deg2Rad * angleDeg;

            var c = new Vector3(
                radius * Mathf.Cos(angleDeg * Mathf.Deg2Rad),
                0,
                radius * Mathf.Sin(angleDeg * Mathf.Deg2Rad)
            );

            corners[i] =
                (radius * Mathf.Cos(angleRad) * right) +
                (radius * Mathf.Sin(angleRad) * up) +
                center;
        }

        return corners;
    }

    private static void ConnectCorners(Vector3[] corners, Color color)
    {
        for (int i = 0; i < corners.Length; i++)
        {
            for (int j = i + 1; j < corners.Length; j++)
                Debug.DrawLine(corners[i], corners[j], color);
        }
    }

    private static void ConnectParallel(Vector3[] startCorners, Vector3[] endCorners, Color color)
    {
        for (int i = 0; i < startCorners.Length; i++)
            Debug.DrawLine(startCorners[i], endCorners[i], color);
    }

    public void OnGUI()
    {
        GUILayout.BeginVertical("Box");
        GUILayout.Label(_displayedLines);
        GUILayout.Label(_displayedFixedLines);
        GUILayout.EndVertical();
    }
}
