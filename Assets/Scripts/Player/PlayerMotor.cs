using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerGroundDetector))]
[RequireComponent(typeof(PlayerLedgeDetector))]
[RequireComponent(typeof(PlayerWallDetector))]
public class PlayerMotor : MonoBehaviour
{
    private const float CAPSULE_RADIUS = 0.375f;
    private const float CAPSULE_HEIGHT = 1.4558f;
    private const float CAPSULE_Y_OFFSET = 0.7279f;

    // Required components
    private PlayerGroundDetector _ground;
    private PlayerLedgeDetector _ledge;
    private PlayerWallDetector _wall;

    /// <summary>
    /// Velocity relative to the ground we were standing on last.
    /// </summary>
    /// <value></value>
    public Vector3 RelativeVelocity
    {
        get => _relativeVelocity;
        set => _relativeVelocity = value;
    }
    private Vector3 _relativeVelocity;

    /// <summary>
    /// A convenience proxy for manipulating just the x and z values of
    /// RelativeVelocity.
    /// Setting this property changes the x and z values RelativeVelocity,
    /// leaving the y component untouched.
    /// </summary>
    /// <value></value>
    public Vector3 RelativeFlatVelocity
    {
        get => _relativeVelocity.Flattened();
        set
        {
            _relativeVelocity.x = value.x;
            _relativeVelocity.z = value.z;
        }
    }

    /// <summary>
    /// A convenience proxy for manipulating just the y value of
    /// RelativeVelocity.  Mainly so the y velocity can be modified without
    /// needing to copy RelativeVelocity to a temporary variable.
    /// </summary>
    /// <value></value>
    public float RelativeVSpeed
    {
        get => _relativeVelocity.y;
        set => _relativeVelocity.y = value;
    }

    /// <summary>
    /// Total velocity, after adding in velocity from moving platforms
    /// </summary>
    public Vector3 TotalVelocity => _ground.GroundVelocity + RelativeVelocity;

    // Accessors for collision data
    public Vector3 GroundVelocity => _ground.GroundVelocity;
    public Vector3 LastGroundNormal => _ground.LastGroundNormal;
    public bool IsGrounded => _ground.IsGrounded;
    public bool WasGroundedLastFrame => _ground.WasGroundedLastFrame;
    public bool IsBonkingHead => _ground.CheckBonkingHead(_internalPosition);
    public float HeightAboveGround => _ground.HeightAboveGround;
    public float LastGroundedTime => _ground.LastGroundedTime;
    
    public Vector3 LastWallNormal => _wall.LastWallNormal;
    public bool IsTouchingWall => _wall.IsTouchingWall;

    public bool LedgePresent => _ledge.LedgePresent;
    public float LastLedgeHeight => _ledge.LastLedgeHeight;

    private Vector3 _internalPosition;

    void Awake()
    {
        _ground = GetComponent<PlayerGroundDetector>();
        _ledge = GetComponent<PlayerLedgeDetector>();
        _wall = GetComponent<PlayerWallDetector>();

        _internalPosition = transform.position;
    }

    void Update()
    {
        // Smoothly interpolate towards the internal position
        transform.position = Vector3.MoveTowards(
            transform.position,
            _internalPosition,
            TotalVelocity.magnitude * Time.deltaTime
        );
    }

    /// <summary>
    /// Resets the state to a consistent baseline
    /// </summary>
    public void ResetState()
    {
        transform.position = _internalPosition;

        _ground.RecordFootprintPos(_internalPosition);
        _ground.UpdateGroundState(_internalPosition);
        _ground.RecordFootprintPos(_internalPosition);

        _wall.UpdateWallState(_internalPosition);
        _ledge.UpdateLedgeDetectorState(_internalPosition);
    }

    /// <summary>
    /// Use this to teleport the player, instead of setting transform.position
    /// directly.
    /// </summary>
    /// <param name="position"></param>
    public void SetPosition(Vector3 position)
    {
        transform.position = position;
        _internalPosition = position;
    }

    /// <summary>
    /// Checks for collisions and updates values such as:
    /// * Whether or not we're grounded
    /// * The velocity and normal of the last ground we touched
    /// * Whether or not we're touching a wall
    /// * Etc.
    /// </summary>
    public void UpdateCollisionStatus()
    {
        _ground.UpdateGroundState(_internalPosition);
        _ledge.UpdateLedgeDetectorState(_internalPosition);
        _wall.UpdateWallState(_internalPosition);
    }

    /// <summary>
    /// Moves the player in a straight line at the current velocity,
    /// automatically taking moving/rotating platforms into account.
    /// It also climbs up/down slopes and
    /// </summary>
    public void Move()
    {
        // Move with the current velocity
        MoveWithSlopes(TotalVelocity * Time.deltaTime);
        SendTriggerEnterEvents();

        // Remember moving-platform stuff for next frame
        _ground.RecordFootprintPos(_internalPosition);
    }

    /// <summary>
    /// Moves with the given delta, sliding along sloped surfaces and
    /// stopping when obstructed by walls.
    /// </summary>
    /// <param name="deltaPos"></param>
    private void MoveWithSlopes(Vector3 deltaPos)
    {
        var moveResults = TryMove(_internalPosition, deltaPos);
        while (moveResults.remainingDeltaPos.magnitude > 0)
        {
            moveResults = TryMove(
                moveResults.stopPoint,
                moveResults.remainingDeltaPos.ProjectOnPlane(moveResults.contactSurfaceNormal)
            );
        }

        _internalPosition = moveResults.stopPoint;
    }
    
    private TryMoveResults TryMove(Vector3 startPoint, Vector3 deltaPos)
    {
        // If there's no distance to move, then don't do anything.
        if (deltaPos.magnitude == 0)
        {
            return new TryMoveResults
            {
                stopPoint = startPoint,
                remainingDeltaPos = Vector3.zero,
                contactSurfaceNormal = Vector3.zero,
                madeContact = false
            };
        }

        // Do a capsule cast along the delta pos
        Vector3 capsuleBottom = startPoint;
        Vector3 capsuleTop = capsuleBottom + (Vector3.up * CAPSULE_HEIGHT);
        bool hitAnything = Physics.CapsuleCast(
            point1: capsuleBottom + (Vector3.up * CAPSULE_RADIUS),
            point2: capsuleTop - (Vector3.up * CAPSULE_RADIUS),
            radius: CAPSULE_RADIUS,
            direction: deltaPos.normalized,
            maxDistance: deltaPos.magnitude,
            queryTriggerInteraction: QueryTriggerInteraction.Ignore,
            layerMask: Physics.DefaultRaycastLayers,
            hitInfo: out RaycastHit hit
        );

        // If we didn't hit anything, then just go straight to the destination
        if (!hitAnything)
        {
            return new TryMoveResults
            {
                stopPoint = startPoint + deltaPos,
                remainingDeltaPos = Vector3.zero,
                contactSurfaceNormal = Vector3.zero,
                madeContact = false
            };
        }

        // Move as far as we can, up to the collision point.
        Vector3 stopPoint = startPoint + (hit.distance * deltaPos.normalized);

        // Move a little bit out of the collision so the next raycast starts
        // outside the collider
        stopPoint += hit.normal * 0.01f;

        // Calculate how much remaining delta pos we have
        Vector3 distanceTraveled = stopPoint - startPoint;
        Vector3 remainingDeltaPos = deltaPos - distanceTraveled;

        return new TryMoveResults
        {
            stopPoint = stopPoint,
            remainingDeltaPos = remainingDeltaPos,
            contactSurfaceNormal = hit.normal,
            madeContact = true
        };
    }
    private struct TryMoveResults
    {
        /// <summary>
        /// The position of our feet when we had to stop, either due to a
        /// collision or due to successfully moving all the way.
        /// </summary>
        public Vector3 stopPoint;

        /// <summary>
        /// The delta pos that should be fed into the next attempt.
        /// Will be zero if we made it all the way without making contact
        /// </summary>
        public Vector3 remainingDeltaPos;

        /// <summary>
        /// The normal of the surface that we made contact with, if we made
        /// contact.
        /// Will be zero if we didn't make contact.
        /// </summary>
        public Vector3 contactSurfaceNormal;

        /// <summary>
        /// Will be false if we traveled the entire distance without making
        /// contact.  Will be true otherwise.
        /// </summary>
        public bool madeContact;
    }

    private void SendTriggerEnterEvents()
    {
        Vector3 capsuleBottom = _internalPosition;
        Vector3 capsuleTop = capsuleBottom + (Vector3.up * CAPSULE_HEIGHT);
        var colliders = Physics.OverlapCapsule(
            point0: capsuleBottom + (Vector3.up * CAPSULE_RADIUS),
            point1: capsuleTop - (Vector3.up * CAPSULE_RADIUS),
            radius: CAPSULE_RADIUS,
            queryTriggerInteraction: QueryTriggerInteraction.Collide,
            layerMask: Physics.DefaultRaycastLayers
        );

        foreach (var c in colliders)
        {
            c.SendMessage(
                "OnPlayerMotorCollisionStay",
                this,
                SendMessageOptions.DontRequireReceiver
            );
        }
    }
}
