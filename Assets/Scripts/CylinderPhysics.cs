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
        float maxDistance = ENDLESS_DISTANCE,
        QueryTriggerInteraction hitTrigger = QueryTriggerInteraction.UseGlobal
    )
    {
        var allHits = CylinderCastAll(
            origin,
            radius,
            height,
            direction,
            maxDistance,
            hitTrigger
        );
        return allHits.Length > 0;
    }

    public static RaycastHit[] CylinderCastAll(
        Vector3 origin,
        float radius,
        float height,
        Vector3 direction,
        float maxDistance = ENDLESS_DISTANCE,
        QueryTriggerInteraction hitTrigger = QueryTriggerInteraction.UseGlobal
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
        var boxHits = Physics.BoxCastAll(
            boxPos,
            halfExtents,
            direction,
            orientation,
            maxDistance,
            Physics.DefaultRaycastLayers,
            hitTrigger
        );
        
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
        Vector3 direction,
        QueryTriggerInteraction hitTriggers = QueryTriggerInteraction.UseGlobal
    )
    {
        // Create a coordinate system where "direction" points forward.
        var orientation = Quaternion.LookRotation(direction);
        var right   = orientation * Vector3.right;
        var up      = orientation * Vector3.up;
        var forward = orientation * Vector3.forward;

        // Do a sphere cast, but then fudge the distance to make it look like
        // a circle.
        RaycastHit hit;
        bool hitAnything = Physics.SphereCast(
            origin,
            radius,
            direction,
            out hit,
            maxDistance,
            Physics.DefaultRaycastLayers,
            hitTriggers
        );

        if (!hitAnything)
            return null;

        // Fudge the hit's distance
        float originY = origin.ComponentAlong(direction);
        float hitPointY = hit.point.ComponentAlong(direction);
        hit.distance = Mathf.Abs(hitPointY - originY);

        // Drop the hit if it's too far away
        if (hit.distance > maxDistance)
            return null;

        // To complete the illusion of this being a cylinder and not a sphere,
        // we need to adjust the normal.  Spheres will produce a "diagonal"
        // normal if they pass through the "edge" of a platform, and that's
        // not what we want.  So, we need to do an ordinary raycast to get an
        // ordinary normal.
        var rayStartPoint = hit.point - (hit.distance * direction);
        RaycastHit rayHit;
        bool lineHitAnything = Physics.Raycast(
            rayStartPoint,
            direction,
            out rayHit,
            maxDistance
        );
        if (lineHitAnything)
            hit.normal = rayHit.normal;

        return hit;
    }
}
