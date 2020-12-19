using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerWallDetector : MonoBehaviour
{
    public const float WALL_CHECK_DIST = 0.13f;

    public Vector3 LastWallNormal {get; private set;}
    public bool IsTouchingWall {get; private set;}

    public void UpdateWallState()
    {
        // Do a capsule overlap to see if we're touching a wall
        var wallHits = CylinderPhysics.OverlapCylinder(
            transform.position,
            PlayerMovement.BODY_RADIUS + WALL_CHECK_DIST,
            PlayerMovement.BODY_HEIGHT
        );
        IsTouchingWall = wallHits.Any(c => c.transform != this.transform);

        if (!IsTouchingWall)
            return;

        // Do some cheaty box-casting to find the normal of the wall we're touching.
        var lastNormal = FindWallNormal();
        if (!lastNormal.HasValue)
        {
            IsTouchingWall = false;
            return;
        }
        LastWallNormal = lastNormal.Value;
    }

    private Vector3? FindWallNormal()
    {
        // Do a box cast in 8 directions and find the hit that was closest.
        RaycastHit? closestHit = null;
        float shortestDist = float.MaxValue;
        for (float angle = 0; angle < 360; angle += 45)
        {
            var hit = BoxcastInDir(angle);

            if (hit == null)
                continue;

            if (hit.Value.distance < shortestDist)
            {
                shortestDist = hit.Value.distance;
                closestHit = hit;
            }
        }

        return closestHit?.normal;
    }

    private RaycastHit? BoxcastInDir(float angleDeg)
    {
        // Do a box cast in this direction
        float angleRad = Mathf.Deg2Rad * angleDeg;

        var forward = new Vector3(
            Mathf.Cos(angleRad),
            0,
            Mathf.Sin(angleRad)
        );

        var orientation = Quaternion.LookRotation(forward, Vector3.up);

        var halfExtents = new Vector3(
            PlayerMovement.BODY_RADIUS,
            PlayerMovement.BODY_HEIGHT / 2,
            PlayerMovement.BODY_RADIUS
        );

        var boxCenter = transform.position + (Vector3.up * PlayerMovement.BODY_HEIGHT / 2);

        RaycastHit hit;
        bool hitAnything = Physics.BoxCast(
            boxCenter,
            halfExtents,
            forward,
            out hit,
            orientation,
            WALL_CHECK_DIST
        );

        // TODO: Verify that it's within our cylinder?

        if (!hitAnything)
            return null;
        return hit;
    }
}
