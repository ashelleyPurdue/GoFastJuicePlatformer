using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugDisplay : MonoBehaviour
{
    private static string _queuedLines = "";
    private static string _queuedFixedLines = "";

    private string _displayedLines = "";
    private string _displayedFixedLines = "";

    public static void PrintLine(string line)
    {
        _queuedLines += line + "\n";
    }

    public static void PrintLineFixed(string line)
    {
        _queuedFixedLines += line + "\n";
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

            corners[i] = orientation.Value * corners[i];
            corners[i] += center;
        }
        // {
        //     new Vector3(0, 0, 0),
        //     new Vector3(0, 0, 1),
        //     new Vector3(0, 1, 0),
        //     new Vector3(0, 1, 1),
        //     new Vector3(1, 0, 0),
        //     new Vector3(1, 0, 1),
        //     new Vector3(1, 1, 0),
        //     new Vector3(1, 1, 1)
        // };
        // for(int i = 0; i < corners.Length; i++)
        // {
        //     corners[i] *= 2;
        //     corners[i] -= Vector3.one;
            
        //     corners[i].x *= halfExtents.x;
        //     corners[i].y *= halfExtents.y;
        //     corners[i].z *= halfExtents.z;
        //     corners[i] = orientation.Value * corners[i];

        //     corners[i] += center;
        // }

        // Draw a line between each corner and every other corner
        for (int i = 0; i < corners.Length; i++)
        {
            for (int j = i + 1; j < corners.Length; j++)
            {
                Debug.DrawLine(corners[i], corners[j], color);
            }
        }
    }

    public void OnGUI()
    {
        GUILayout.BeginVertical("Box");
        GUILayout.Label(_displayedLines);
        GUILayout.Label(_displayedFixedLines);
        GUILayout.EndVertical();
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
}
