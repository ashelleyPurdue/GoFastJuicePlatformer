using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Vector3Extensions
{
    public static Vector3 Flattened(this Vector3 v)
    {
        v.y = 0;
        return v;
    }

    public static Vector3 ProjectOnPlane(this Vector3 v, Vector3 planeNormal)
    {
        return Vector3.ProjectOnPlane(v, planeNormal);
    }

    public static Vector3 ProjectOnVector(this Vector3 v, Vector3 target)
    {
        return Vector3.Project(v, target);
    }

    public static Vector3 ReflectOffOfSurface(this Vector3 v, Vector3 surfaceNormal)
    {
        var vectorAlongSurface = v.ProjectOnPlane(surfaceNormal);
        var vectorIntoSurface = v - vectorAlongSurface;

        return -vectorIntoSurface + vectorAlongSurface;
    }

    /// <summary>
    /// Returns the component of this vector along the target vector.
    /// </summary>
    /// <param name="v"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static float ComponentAlong(this Vector3 v, Vector3 target)
    {
        float dot = Vector3.Dot(v, target);
        float mag = target.magnitude;
        return dot / mag;
    }
}
