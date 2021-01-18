using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [ExecuteInEditMode]
public class SphereGizmo : MonoBehaviour
{
    public float radius = 0.1f;
    public Color color = Color.blue;

    void OnDrawGizmos()
    {
        var originalColor = Gizmos.color;
        Gizmos.color = color;

        Gizmos.DrawSphere(transform.position, radius);

        Gizmos.color = originalColor;
    }
}
