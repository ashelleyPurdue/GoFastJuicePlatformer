using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerGroundDetector))]
[RequireComponent(typeof(PlayerLedgeDetector))]
[RequireComponent(typeof(PlayerWallDetector))]
[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    // Required components
    private PlayerGroundDetector _ground;
    private PlayerLedgeDetector _ledge;
    private PlayerWallDetector _wall;
    private CharacterController _controller;

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
    public bool IsBonkingHead => _ground.IsBonkingHead;
    public float HeightAboveGround => _ground.HeightAboveGround;
    public float LastGroundedTime => _ground.LastGroundedTime;
    
    public Vector3 LastWallNormal => _wall.LastWallNormal;
    public bool IsTouchingWall => _wall.IsTouchingWall;

    public bool LedgePresent => _ledge.LedgePresent;
    public float LastLedgeHeight => _ledge.LastLedgeHeight;

    void Awake()
    {
        _ground = GetComponent<PlayerGroundDetector>();
        _ledge = GetComponent<PlayerLedgeDetector>();
        _wall = GetComponent<PlayerWallDetector>();
        _controller = GetComponent<CharacterController>();
    }

    /// <summary>
    /// Resets the state to a consistent baseline
    /// </summary>
    public void ResetState()
    {
        _ground.RecordFootprintPos();
        _ground.UpdateGroundState();
        _ground.RecordFootprintPos();

        _wall.UpdateWallState();
        _ledge.UpdateLedgeDetectorState();
    }

    /// <summary>
    /// Use this to teleport the player, instead of setting transform.position
    /// directly.
    /// </summary>
    /// <param name="position"></param>
    public void SetPosition(Vector3 position)
    {
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
        transform.position = position;
        _controller.enabled = false;
        _controller.enabled = true;
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
        _ground.UpdateGroundState();
        _ledge.UpdateLedgeDetectorState();
        _wall.UpdateWallState();
    }

    /// <summary>
    /// Moves the player in a straight line at the current velocity,
    /// automatically taking moving/rotating platforms into account.
    /// </summary>
    public void Move()
    {
        // Move with the current velocity
        _controller.Move(TotalVelocity * Time.deltaTime);

        // Remember moving-platform stuff for next frame
        _ground.RecordFootprintPos();
    }
}
