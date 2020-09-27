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
