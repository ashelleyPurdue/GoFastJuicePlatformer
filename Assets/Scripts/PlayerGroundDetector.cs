using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGroundDetector : MonoBehaviour
{
    public const float GROUND_DETECTOR_THICKNESS = 0.1f;
    public const float GROUND_DETECTOR_RADIUS = 0.5f;

    public Vector3 GroundVelocity {get; private set;}
    public Transform CurrentGround {get; private set;}
    public bool IsGrounded => CurrentGround != null;
    public float LastGroundedTime {get; private set;}

    public bool IsBonkingHead => CheckBonkingHead();

    private Vector3 _lastPositionRelativeToGround;
    
    /// <summary>
    /// Call this at the beginning of FixedUpdate, before you do any
    /// calculations that depend on this object's properties.
    /// </summary>
    public void UpdateGroundState()
    {
        var previousGround = CurrentGround;
        CurrentGround = GetGround();

        // Record the last time we were grounded
        if (IsGrounded)
            LastGroundedTime = Time.time;

        // Calculate how fast the ground is moving (aka: the ground velocity)
        if (IsGrounded && CurrentGround == previousGround)
        {
            // Figure out where our "foot prints" have moved to
            var currentFootprintsPos = CurrentGround.TransformPoint(_lastPositionRelativeToGround);
            var lastFootprintsPos = transform.position;

            // Figure out how much the footprints moved, and move by that much
            var deltaFootprints = currentFootprintsPos - lastFootprintsPos;
            GroundVelocity = deltaFootprints / Time.deltaTime;
        }
        else
        {
            // If we're not on a platform, then the ground velocity is zero.
            // If we're standing on a *different* platform than before, then we
            // have no way of tracking its velocity, so we'll just cheat and set
            // it to zero in that case too.
            GroundVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Call this at the end of FixedUpdate, after you have moved the character.
    /// This ensures GroundVelocity is properly calculated on the next frame.
    /// </summary>
    public void RecordFootprintPos()
    {
        if (IsGrounded)
            _lastPositionRelativeToGround = CurrentGround.InverseTransformPoint(transform.position);
    }

    private bool CheckBonkingHead()
    {
        return CylinderPhysics.CylinderCast(
            transform.position + (Vector3.up * 2),
            GROUND_DETECTOR_RADIUS,
            GROUND_DETECTOR_THICKNESS,
            Vector3.up,
            GROUND_DETECTOR_THICKNESS / 2
        );
    }

    /// <summary>
    /// Returns the Transform of the ground that we're standing on,
    /// or null if we're in the air.
    /// </summary>
    /// <returns></returns>
    private Transform GetGround()
    {
        Vector3 origin = transform.position;
        var hits = CylinderPhysics.CylinderCastAll(
            transform.position,
            GROUND_DETECTOR_RADIUS,
            GROUND_DETECTOR_THICKNESS,
            Vector3.down,
            GROUND_DETECTOR_THICKNESS / 2
        );

        foreach (var h in hits)
        {
            if (h.collider.transform != this.transform)
                return h.collider.transform;
        }
        return null;
    }
}
