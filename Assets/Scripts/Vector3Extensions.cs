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
}
