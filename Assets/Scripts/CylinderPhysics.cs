using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CylinderPhysics
{
    private const float ENDLESS_DISTANCE = Mathf.Infinity;
    
    public static bool CylinderCast(
        Vector3 origin,
        float radius,
        float height,
        Vector3 direction,
        float maxDistance = ENDLESS_DISTANCE
    )
    {
        return CylinderCastAll(origin, radius, height, direction, maxDistance).Length > 0;
    }

    public static RaycastHit[] CylinderCastAll(
        Vector3 origin,
        float radius,
        float height,
        Vector3 direction,
        float maxDistance = ENDLESS_DISTANCE
    )
    {
        Debug.DrawLine(origin, origin + (direction * maxDistance));
        Debug.DrawLine(origin, origin + Vector3.left * radius);
        Debug.DrawLine(origin, origin + Vector3.right * radius);
        Debug.DrawLine(origin, origin + Vector3.forward * radius);
        Debug.DrawLine(origin, origin + Vector3.back * radius);

        // Unity doesn't have support for cylinder collisions.
        // NO MATTER.
        // We can just do two casts: a box cast and a capsule cast.
        // Anything that passes the box cast is within the cylinder's "height".
        // Anything that passes the capsule cast is within the cylinder's "radius".
        // Anything that passes BOTH casts must be in our cylinder.
        // This essentially "trims off the corners" of the box cast.
        // Note that this only works for convex shapes.  
        // Just don't use concave platforms.
        Quaternion orientation = Quaternion.identity;   // TODO: Derive this from direction
        var halfExtents = new Vector3
        (
            radius,
            height / 2,
            radius
        );
        var boxPos = origin - new Vector3(0, height / 2, 0);
        var boxHits = Physics.BoxCastAll(boxPos, halfExtents, direction, orientation, maxDistance);
        
        var capsulePointOffset = direction.normalized * (height * 2);
        var capsuleHits = new HashSet<Collider>(Physics.OverlapCapsule
        (
            origin + capsulePointOffset,
            origin - capsulePointOffset,
            radius
        ));

        // If any of the colliders from the boxcast ALSO pass a really tall
        // capsule cast, then it's in the cylinder
        var hits = new List<RaycastHit>();
        foreach (var h in boxHits)
        {
            bool inCapsule = capsuleHits.Contains(h.collider);
            if (inCapsule)
                hits.Add(h);
        }
        return hits.ToArray();
    }
}
