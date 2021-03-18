using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGroundDetector : MonoBehaviour
{
    public const float GROUND_DETECTOR_THICKNESS = 0.1f;
    public const float RAYCAST_DISTANCE = 1000;
    public const float RAYCAST_OFFSET = 1f;

    public Vector3 GroundVelocity {get; private set;}
    public Vector3 LastGroundNormal {get; private set;}
    public Transform CurrentGround {get; private set;}
    public bool IsGrounded {get; private set;}
    public bool WasGroundedLastFrame {get; private set;}
    public float HeightAboveGround {get; private set;}
    public float LastGroundedTime {get; private set;}

    public bool IsBonkingHead => CheckBonkingHead();

    private Vector3 _lastPositionRelativeToGround;
    
    /// <summary>
    /// Call this at the beginning of FixedUpdate, before you do any
    /// calculations that depend on this object's properties.
    /// </summary>
    public void UpdateGroundState()
    {
        RaycastHit? hit = GetGround();

        // Update the height above the ground
        HeightAboveGround = hit.HasValue
            ? hit.Value.distance - RAYCAST_OFFSET
            : RAYCAST_DISTANCE;   // If we're over a void, then we're Really Fuckin' High(tm)

        // We're grounded if our height is below a threshold
        WasGroundedLastFrame = IsGrounded;
        IsGrounded = HeightAboveGround < GROUND_DETECTOR_THICKNESS;

        // Update the current ground.
        var previousGround = CurrentGround;
        CurrentGround = IsGrounded
            ? hit.Value.transform
            : null;

        // Record the last time we were grounded
        if (IsGrounded)
            LastGroundedTime = Time.time;

        // Update the ground normal
        if (IsGrounded)
            LastGroundNormal = hit.Value.normal;

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
            transform.position + (Vector3.up * PlayerConstants.BODY_HEIGHT),
            PlayerConstants.BODY_RADIUS,
            GROUND_DETECTOR_THICKNESS,
            Vector3.up,
            GROUND_DETECTOR_THICKNESS / 2
        );
    }

    /// <summary>
    /// Returns a raycast hit for the ground we're above,
    /// or null if we're in the air.
    /// </summary>
    /// <returns></returns>
    private RaycastHit? GetGround()
    {
        return CylinderPhysics.CircleCast(
            transform.position + (Vector3.up * RAYCAST_OFFSET),
            PlayerConstants.BODY_RADIUS,
            RAYCAST_DISTANCE,
            Vector3.down,
            QueryTriggerInteraction.Ignore
        );
    }
}
