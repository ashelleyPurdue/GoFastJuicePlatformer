using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DropShadowBehavior : MonoBehaviour
{
    private const float _radius = 0.5f;
    private const float _thickness = 1;
    private const float _maxDist = 1000;

    void FixedUpdate()
    {
        transform.localPosition = Vector3.zero;
        // We'll begin our box cast just a little bit higher than the origin.
        // That way, the shadow go "through" the floor when we're standing on
        // the ground.
        Vector3 origin = transform.parent.TransformPoint(0, _thickness, 0);
        Vector3? contactPoint = FindContactPoint(origin);

        // If we're not above ground(EG: above a bottomless pit), just disappear
        if (!contactPoint.HasValue)
        {
            transform.localScale = Vector3.zero;
            return;
        }

        // We're over ground, so jump to the spot where the shadow should be.
        transform.localScale = Vector3.one;
        transform.position = contactPoint.Value;
    }

    /// <summary>
    /// Returns the point where the shadow should be.
    /// Returns null if we're not over ground.
    /// </summary>
    /// <param name="origin"></param>
    /// <returns></returns>
    private Vector3? FindContactPoint(Vector3 origin)
    {
        // Use the same hit-detection logic as the player, so that the shadow is
        // accurate.
        var hits = CylinderPhysics.CylinderCastAll(origin, _radius, _thickness, Vector3.down);
        if (hits.Length == 0)
            return null;

        Vector3 pos = origin;
        pos.y = hits[0].point.y;
        return pos;
    }

    
}
