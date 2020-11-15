using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CylinderPhysics
{
    private const float ENDLESS_DISTANCE = Mathf.Infinity;
    
    public static Collider[] OverlapCylinder(
        Vector3 origin,
        float radius,
        float height
    )
    {
        // Unity doesn't have support for cylinder collisions.
        // NO MATTER.
        // We can just do two casts: a box cast and a capsule cast.
        // Anything that passes the box cast is within the cylinder's "height".
        // Anything that passes the capsule cast is within the cylinder's "radius".
        // Anything that passes BOTH casts must be in our cylinder.
        // This essentially "trims off the corners" of the box cast.
        // Note that this only works for convex shapes.  
        // Just don't use concave platforms.
        Quaternion orientation = Quaternion.identity;
        var halfExtents = new Vector3
        (
            radius,
            height / 2,
            radius
        );
        var boxPos = origin + (Vector3.up * height / 2);
        var boxHits = Physics.OverlapBox(boxPos, halfExtents, orientation);
        var capsulePointOffset = Vector3.up * (height * 2);
        var capsuleHits = new HashSet<Collider>(Physics.OverlapCapsule
        (
            origin + capsulePointOffset,
            origin - capsulePointOffset,
            radius
        ));
        
        // If any of the colliders from the boxcast ALSO pass a really tall
        // capsule cast, then it's in the cylinder
        var hits = new List<Collider>();
        foreach (var c in boxHits)
        {
            bool inCapsule = capsuleHits.Contains(c);
            if (inCapsule)
                hits.Add(c);
        }
        return hits.ToArray();
    }

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

    /// <summary>
    /// Like a raycast, except it sweeps a circle instead of a point.
    /// Returns null if nothing was hit.
    /// Currently only works in the downward direction.
    /// </summary>
    /// <returns></returns>
    public static RaycastHit? CircleCast(
        Vector3 origin,
        float radius,
        float maxDistance,
        Vector3 direction
    )
    {
        // Create a coordinate system where "direction" points forward.
        var orientation = Quaternion.LookRotation(direction);
        var right   = orientation * Vector3.right;
        var up      = orientation * Vector3.up;
        var forward = orientation * Vector3.forward;

        // Start with a boxcast, to get a rough approximation.
        // We will shave off the "corners" later.
        Vector3 halfExtents = Vector3.one * radius;
        halfExtents.z = 0.025f;
        RaycastHit boxHit;
        bool boxHitSuccess = Physics.BoxCast(
            origin,
            halfExtents,
            direction,
            out boxHit,
            orientation,
            maxDistance
        );

        if (!boxHitSuccess)
            return null;

        // If the impact point is within the circle's radius, then the "corners"
        // didn't have any influence on the result.  Therefore, we don't need to
        // cut them out.
        Vector3 flatOrigin = origin.ProjectOnPlane(direction);
        Vector3 flatBoxHit = boxHit.point.ProjectOnPlane(direction);

        if (Vector3.Distance(flatBoxHit, flatOrigin) <= radius)
            return boxHit;

        // The impact point was outside the radius of the circle
        // (IE: in one of the corners).  So, let's move that point inward onto
        // the edge of the circle, and do a raycast
        Vector3 raycastStartOffset = radius * (flatBoxHit - flatOrigin).normalized;
        Vector3 raycastStart = origin + raycastStartOffset;

        RaycastHit raycastHit;
        bool rayHitSuccess = Physics.Raycast(
            raycastStart,
            direction,
            out raycastHit,
            maxDistance
        );

        if (!rayHitSuccess)
            return null;

        return raycastHit;
    }
}
