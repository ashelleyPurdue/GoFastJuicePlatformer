using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CylinderVisualizer
{
    private static List<GameObject> _voxelPool = new List<GameObject>();
    public static void VisualizeCylinderCast(
        Vector3 origin,
        float radius,
        float height,
        Vector3 direction,
        float maxDistance = Mathf.Infinity
    )
    {
        Vector3 visualizationSize = Vector3.one * 1;
        Vector3 start = origin - (visualizationSize / 2);
        Vector3 end = origin + (visualizationSize / 2);

        // Create a bunch of voxels.  They'll act as "probes".
        Vector3 voxelSize = Vector3.one * 0.1f;
        var voxels = new HashSet<GameObject>();

        for (float x = start.x; x < end.x; x += voxelSize.x)
        {
            for (float y = start.y; y < end.y; y += voxelSize.y)
            {
                for (float z = start.z; z < end.z; z += voxelSize.z)
                {
                    var pos = new Vector3(x, y, z);

                    // Skip the positions that are obviously deep inside
                    float distance = Vector3.Cross(direction, pos - origin).magnitude;
                    if (distance < radius)
                        continue;

                    // Either create the voxel, or get one from the pool.
                    GameObject voxel;
                    int i = voxels.Count;
                    if (i < _voxelPool.Count)
                    {
                        voxel = _voxelPool[i];
                    }
                    else
                    {
                        voxel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        GameObject.Destroy(voxel.GetComponent<Renderer>());
                        _voxelPool.Add(voxel);
                    }
                    
                    // Move it to the correct place
                    voxel.transform.position = pos;
                    voxel.transform.localScale = voxelSize;

                    var collider = voxel.GetComponent<BoxCollider>();
                    collider.isTrigger = true;

                    voxels.Add(voxel);
                }
            }
        }

        // Destroy all voxels in the pool that aren't being used.
        while(_voxelPool.Count > voxels.Count)
        {
            int i = _voxelPool.Count - 1;
            GameObject.Destroy(_voxelPool[i]);
            _voxelPool.RemoveAt(i);
        }

        // Do the cylinder cast
        var hits = CylinderPhysics.CylinderCastAll(origin, radius, height, direction, maxDistance);

        // Draw a gizmo for every voxel that's in the cylinder cast
        foreach (var h in hits)
        {
            if (!voxels.Contains(h.collider.gameObject))
                continue;

            DebugDrawCube(Color.blue, h.collider.gameObject.transform.position, voxelSize);
        }
    }

    private static void DebugDrawCube(Color color, Vector3 center, Vector3 size)
    {
        // Generate all the corners
        Vector3 startCorner = center - (size / 2);
        Vector3[] corners = new[]
        {
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 1),
            new Vector3(0, 1, 0),
            new Vector3(0, 1, 1),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 1),
            new Vector3(1, 1, 0),
            new Vector3(1, 1, 1)
        };
        for(int i = 0; i < corners.Length; i++)
        {
            corners[i].x *= size.x;
            corners[i].y *= size.y;
            corners[i].z *= size.z;

            corners[i] += center;//startCorner;
        }

        // Draw a line between each corner and every other corner
        for (int i = 0; i < corners.Length; i++)
        {
            for (int j = i + 1; j < corners.Length; j++)
            {
                Debug.DrawLine(corners[i], corners[j], color);
            }
        }
    }
}