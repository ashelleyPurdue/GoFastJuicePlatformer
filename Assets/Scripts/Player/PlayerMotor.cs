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

    private Vector3 _internalPosition => _characterController.transform.position;
    private CharacterController _characterController;

    void Awake()
    {
        _ground = GetComponent<PlayerGroundDetector>();
        _ledge = GetComponent<PlayerLedgeDetector>();
        _wall = GetComponent<PlayerWallDetector>();

        // HACK: Create a secret character controller that we use for the
        // internal, non-interpolated position
        var dummy = new GameObject();
        dummy.transform.position = transform.position;
        dummy.layer = LayerMask.NameToLayer("Ignore Raycast");
        _characterController = dummy.AddComponent<CharacterController>();

        _characterController.radius = CAPSULE_RADIUS;
        _characterController.height = CAPSULE_HEIGHT;
        _characterController.center = Vector3.up * CAPSULE_Y_OFFSET;
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

        // CharacterController maintains its own private "position" field,
        // which happens to trump "transform.position".  This means you can't
        // teleport the player by changing "transform.position", because the
        // CharacterController will just roll you back to its internal position.
        //
        // The "correct" way to avoid this would be to call CharacterController's
        // "SetPosition()" method, like you would for a rigidbody.  Unfortunately,
        // CharacterController doesn't HAVE a "SetPosition()" method.
        //
        // Thanks, Unity >_<
        //
        // To get around this, we disable the CharacterController, and then 
        // immediately re-enable it.  This forces CharacterController to sync
        // its internal position with "transform.position", avoiding that stupid
        // rollback.
        _characterController.transform.position = position;
        _characterController.enabled = false;
        _characterController.enabled = true;
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
        _characterController.Move(TotalVelocity * Time.deltaTime);
        SendTriggerEnterEvents();

        // Remember moving-platform stuff for next frame
        _ground.RecordFootprintPos(_internalPosition);
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
