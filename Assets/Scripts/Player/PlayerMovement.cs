using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(IPlayerInput))]
[RequireComponent(typeof(PlayerGroundDetector))]
[RequireComponent(typeof(PlayerLedgeDetector))]
[RequireComponent(typeof(PlayerWallDetector))]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // Required components
    private IPlayerInput _input;
    private PlayerGroundDetector _ground;
    private PlayerLedgeDetector _ledge;
    private PlayerWallDetector _wall;
    private CharacterController _controller;

    // Constants
    public const float BODY_HEIGHT = 1.58775f;
    public const float BODY_RADIUS = 0.375f;
    
    public const float SHORT_JUMP_DECAY_RATE = 0.7f;

    public const float FULL_JUMP_HEIGHT = 5;
    public const float FULL_JUMP_RISE_TIME = 0.404f;
    public const float FULL_JUMP_FALL_TIME = 0.328f;

    public const float HSPEED_MIN = 2;
    public const float HSPEED_MAX_GROUND = 8;
    public const float HSPEED_MAX_AIR = 20;

    public const float TERMINAL_VELOCITY_AIR = -100;
    public const float TERMINAL_VELOCITY_WALL_SLIDE = -10;

    public const float HACCEL_GROUND = 15;
    public const float HACCEL_AIR = 9;
    public const float HACCEL_AIR_BACKWARDS = 15;


    public const float BONK_SPEED = -3;
    public const float LEDGE_GRAB_VSPEED = 11;
    public const float LEDGE_GRAB_HSPEED = 4;
    public const float LEDGE_GRAB_DURATION = 0.15f;

    public const float ROT_SPEED_DEG = 360 * 2;
    public const float FRICTION_GROUND = 15;
    public const float FRICTION_WALL_SLIDE = 10;

    public const float COYOTE_TIME = 0.1f;      // Allows you to press the jump button a little "late" and still jump
    public const float EARLY_JUMP_TIME = 0.1f;  // Allows you to press the jump button a little "early" and still jump
    
    // If you jump again shortly after you land, you'll do a "chained jump."
    // This is like the "double jump" from 3D Mario games.
    public const float CHAINED_JUMP_HSPEED_MULT = 1.3f;
    public const float CHAINED_JUMP_TIME_WINDOW = 0.1f;

    public const float WALL_JUMP_HSPEED_MULT = 1.1f;

    public const float MAX_PIVOT_SPEED = 0.25f; // If you're below this speed, you can pivot on a dime.

    // Events
    public UnityEvent StartedJumping;

    // Computed jump/gravity values
    private float _jumpSpeed;
    private float _fallGravity;
    private float _riseGravity;
    private float _shortJumpRiseGravity;
    private float _wallSlideGravity => _riseGravity;


    // Accessors
    public Vector3 Forward => AngleForward(HAngleDeg);

    public Vector3 TotalVelocity => 
        _ground.GroundVelocity +
        _walkVelocity +
        (Vector3.up * VSpeed);

    public bool IsGrabbingLedge {get; private set;}
    public bool IsWallSliding {get; private set;}

    // State
    public float HAngleDeg {get; private set;}
    public float HSpeed {get; private set;}
    public float VSpeed {get; private set;}

    private float _lastJumpButtonPressTime = float.NegativeInfinity;
    private bool _jumpReleased;

    private float _chainedJumpTimer = 0;
    private int _chainedJumpCount = 0;

    private float _ledgeGrabTimer = 0;
    private Vector3 _walkVelocity;

    public void Awake()
    {
        _input = GetComponent<IPlayerInput>();
        _ground = GetComponent<PlayerGroundDetector>();
        _ledge = GetComponent<PlayerLedgeDetector>();
        _wall = GetComponent<PlayerWallDetector>();
        _controller = GetComponent<CharacterController>();

        // Compute jump parameters
        var jumpValues = GravityMath.ComputeGravity(
            FULL_JUMP_HEIGHT,
            FULL_JUMP_RISE_TIME,
            FULL_JUMP_FALL_TIME
        );

        _jumpSpeed   = jumpValues.JumpVelocity;
        _fallGravity = jumpValues.FallGravity;
        _riseGravity = jumpValues.RiseGravity;
        Debug.Log("Jump speed: " + _jumpSpeed);
    }

    /// <summary>
    /// Resets the state to a consistent baseline
    /// </summary>
    public void ResetState()
    {
        HAngleDeg = 0;
        HSpeed = 0;
        VSpeed = 0;
        _walkVelocity = Vector3.zero;

        _lastJumpButtonPressTime = float.NegativeInfinity;
        _jumpReleased = false;
        _ledgeGrabTimer = 0;
        _chainedJumpTimer = 0;
        _chainedJumpCount = 0;

        IsGrabbingLedge = false;
        IsWallSliding = false;

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

    public void Update()
    {
        if (_input.JumpPressed)
            _lastJumpButtonPressTime = Time.time;
    }

    public void FixedUpdate()
    {
        // Detect various states
        _ground.UpdateGroundState();
        _ledge.UpdateLedgeDetectorState();
        _wall.UpdateWallState();

        // Start wall-sliding if we're falling while touching a wall
        // (and somewhat facing toward that wall)
        IsWallSliding = 
            !_ground.IsGrounded &&
            _wall.IsTouchingWall &&
            Forward.ComponentAlong(-_wall.LastWallNormal) > 0 &&
            VSpeed < 0;

        // Adjust the velocity based on the current state
        if (IsWallSliding)
            WhileWallSliding();
        else if (_ground.IsGrounded)
            WhileGrounded();
        else
            WhileAirborne();

        ApplyLedgeGrabbing();

        DebugDisplay.PrintLine("HSpeed: " + HSpeed);
        DebugDisplay.PrintLine("VSpeed: " + VSpeed);
        DebugDisplay.PrintLine("Chained jump count: " + _chainedJumpCount);
        DebugDisplay.PrintLine("Chained jump timer: " + _chainedJumpTimer);
        
        // Move with the current velocity
        _controller.Move(TotalVelocity * Time.deltaTime);

        // Remember moving-platform stuff for next frame
        _ground.RecordFootprintPos();
    }

    private void WhileGrounded()
    {
        // Stop falling when we hit the ground.
        VSpeed = 0;

        // HACK: Snap to the ground if we're hovering over it a little bit.
        if (_ground.HeightAboveGround > 0)
            VSpeed = -_ground.HeightAboveGround / Time.deltaTime;
        
        // If we obtained negative hspeed while in the air(EG: from air braking),
        // bring it back to zero so the player doesn't go flying backwards.
        if (HSpeed < 0)
            HSpeed = 0;

        // Start the chained jump timer once we land
        if (!_ground.WasGroundedLastFrame)
            _chainedJumpTimer = CHAINED_JUMP_TIME_WINDOW;

        // Reset the chained jump count if you wait too long after landing
        _chainedJumpTimer -= Time.deltaTime;
        if (_chainedJumpTimer < 0)
        {
            _chainedJumpTimer = 0;
            _chainedJumpCount = 0;
        }

        GroundedControls();
    }
    private void GroundedControls()
    {
        // On the ground, we let the player turn without sliding around or losing
        // speed.
        // We do this by keeping track of their speed and angle separately.
        // The target speed is controlled by the magnitude of the left stick.
        // The target angle is controlled by the direction of the left stick.

        // Speed up/slow down with the left stick
        var inputVector = GetWalkInput();
        float hSpeedIntended = inputVector.magnitude * HSPEED_MAX_GROUND;

        if (hSpeedIntended < HSPEED_MIN)
            hSpeedIntended = 0;

        float accel = HSpeed < hSpeedIntended
            ? HACCEL_GROUND
            : FRICTION_GROUND;

        HSpeed = Mathf.MoveTowards(HSpeed, hSpeedIntended, accel * Time.deltaTime);

        // HACK: Immediately accelerate to the minimum speed.
        // This makes the controls feel snappy and responsive, while still
        // having a feeling of acceleration.
        if (hSpeedIntended > 0 && HSpeed < HSPEED_MIN)
            HSpeed = HSPEED_MIN;

        // Rotate with the left stick
        if (inputVector.magnitude > 0.001f)
        {
            // Gradually rotate until we're facing the direction the stick
            // is pointing
            float targetAngleDeg = Mathf.Atan2(inputVector.z, inputVector.x) * Mathf.Rad2Deg;

            HAngleDeg = Mathf.MoveTowardsAngle(
                HAngleDeg,
                targetAngleDeg,
                ROT_SPEED_DEG * Time.deltaTime
            );

            // ...unless we're going really slow, then just pivot instantly.
            if (HSpeed < MAX_PIVOT_SPEED)
                HAngleDeg = targetAngleDeg;
        }

        // Jump when the button is pressed and we're on the ground.
        // Well, OK, that's a little too strict.  
        // We should let the player press the jump button a little bit before 
        // hitting the ground.
        if (JumpPressedRecently())
        {
            StartGroundJump();
        }

        // Update the velocity based on HSpeed
        _walkVelocity = HSpeed * AngleForward(HAngleDeg);
    }

    private void WhileAirborne()
    {
        // Apply gravity
        // Use more gravity when we're falling so the jump arc feels "squishier"
        float gravity = VSpeed > 0
            ? _riseGravity
            : _fallGravity;

        VSpeed -= gravity * Time.deltaTime;

        // Cap the VSpeed at the terminal velocity
        if (VSpeed < TERMINAL_VELOCITY_AIR)
            VSpeed = TERMINAL_VELOCITY_AIR;

        // Start going downwards if you bonk your head on the ceiling.
        // Don't bonk your head!
        if (VSpeed > 0 && _ground.IsBonkingHead)
            VSpeed = BONK_SPEED;

        AirborneControls();
    }
    private void AirborneControls()
    {
        if (!_input.JumpHeld)
            _jumpReleased = true;

        // Cut the jump short if the button was released on the way up.
        // Immediately setting the VSpeed to 0 looks jarring, so instead we'll
        // exponentially decay it every frame.
        // Once it's decayed below a certain threshold, we'll let gravity do the
        // rest of the work so it still looks natural.
        if (VSpeed > (_jumpSpeed / 2) && _jumpReleased)
            VSpeed *= SHORT_JUMP_DECAY_RATE;

        // Let the player jump for a short period after walking off a ledge,
        // because everyone is human.  
        // Well, except maybe Wile E. Coyote, but he makes mistakes too. 
        bool wasGroundedRecently = (Time.time - COYOTE_TIME < _ground.LastGroundedTime);
        if (VSpeed < 0 && wasGroundedRecently && JumpPressedRecently())
        {
            StartGroundJump();
            DebugDisplay.PrintLine("Coyote-time jump!");
        }

        // In the air, we let the player "nudge" their velocity by applying a
        // force in the direction the stick is being pushed.
        // Unlike on the ground, you *will* lose speed and slide around if you
        // try to change your direction.
        var inputVector = GetWalkInput();

        Vector3 forward = AngleForward(HAngleDeg);
        bool pushingBackwards = inputVector.ComponentAlong(forward) < -0.5f;
        bool pushingForwards = inputVector.ComponentAlong(forward) > 0.75f;
        bool movingForwards = _walkVelocity.normalized.ComponentAlong(forward) > 0;

        float accel = HACCEL_AIR;
        float maxSpeed = HSPEED_MAX_AIR;

        // Reduce the speed limit when moving backwards.
        // If you're wanna go fast, you gotta go forward.
        if (!movingForwards)
            maxSpeed = HSPEED_MAX_GROUND;

        // Give them a little bit of help if they're pushing backwards
        // on the stick, so it's easier to "abort" a poorly-timed jump.
        if (pushingBackwards)
            accel = HACCEL_AIR_BACKWARDS;

        // Apply a force to get our new velocity.
        var oldVelocity = _walkVelocity;
        var newVelocity = _walkVelocity + (inputVector * accel * Time.deltaTime);
        
        // Only let the player accellerate up to the normal ground speed.
        // We won't slow them down if they're already going faster than that,
        // though (eg: due to the speed boost from chained jumping)
        float oldSpeed = oldVelocity.magnitude;
        float newSpeed = newVelocity.magnitude;

        bool wasAboveGroundSpeedLimit = oldSpeed > HSPEED_MAX_GROUND;
        bool nowAboveGroundSpeedLimit = newSpeed > HSPEED_MAX_GROUND;

        if (newSpeed > oldSpeed)
        {
            if (wasAboveGroundSpeedLimit)
                newSpeed = oldSpeed;
            else if (nowAboveGroundSpeedLimit)
                newSpeed = HSPEED_MAX_GROUND;
        }

        // We WILL, however, slow them down if they're going past the max air
        // speed.  That's a hard maximum.
        if (newSpeed > maxSpeed)
            _walkVelocity = _walkVelocity.normalized * maxSpeed;

        _walkVelocity = newVelocity.normalized * newSpeed;

        // Keep HSpeed up-to-date, so it'll be correct when we land.
        HSpeed = _walkVelocity.ComponentAlong(Forward);
    }

    private void WhileWallSliding()
    {
        // Apply gravity
        float gravity = _wallSlideGravity;
        VSpeed -= gravity * Time.deltaTime;

        if (VSpeed < TERMINAL_VELOCITY_WALL_SLIDE)
            VSpeed = TERMINAL_VELOCITY_WALL_SLIDE;

        // Cancel all walking velocity pointing "inside" the wall.
        _walkVelocity = _walkVelocity.ProjectOnPlane(_wall.LastWallNormal);
        HSpeed = _walkVelocity.magnitude;

        // Apply horizontal friction, since sliding on a wall naturally slows
        // you down.
        HSpeed -= FRICTION_WALL_SLIDE * Time.deltaTime;
        if (HSpeed < 0)
            HSpeed = 0;

        _walkVelocity = HSpeed * _walkVelocity.normalized;

        WallSlidingControls();
    }
    private void WallSlidingControls()
    {
        // Wall kick when we press the jump button
        if (JumpPressedRecently())
        {
            // Kick away from the wall
            var kickDir = ReflectOffOfSurface(Forward, _wall.LastWallNormal);
            float kickSpeed = Mathf.Max(
                HSPEED_MAX_GROUND,
                HSpeed
            );
            kickSpeed *= WALL_JUMP_HSPEED_MULT;

            _walkVelocity = kickDir * kickSpeed;
            HAngleDeg = Mathf.Rad2Deg * Mathf.Atan2(kickDir.z, kickDir.x);

            // Jump up
            _jumpReleased = false;
            VSpeed = _jumpSpeed;
            StartedJumping.Invoke();
        }
    }

    private void ApplyLedgeGrabbing()
    {
        // Grab the ledge if we can
        if (!IsGrabbingLedge && CanGrabLedge() && VSpeed < 0)
        {
            IsGrabbingLedge = true;
            _ledgeGrabTimer = LEDGE_GRAB_DURATION;
        }

        // HACK: override the VSpeed and HSpeed while the ledge is being grabbed
        if (IsGrabbingLedge)
        {
            VSpeed = LEDGE_GRAB_VSPEED;
            HSpeed = LEDGE_GRAB_HSPEED;
            _walkVelocity = HSpeed * AngleForward(HAngleDeg);

            _ledgeGrabTimer -= Time.deltaTime;
            if (_ledgeGrabTimer <= 0)
            {
                IsGrabbingLedge = false;
            }
        }
    }

    private void StartGroundJump()
    {
        _chainedJumpCount++;
        _jumpReleased = false;
        VSpeed = _jumpSpeed;
        StartedJumping.Invoke();

        // Give the player a speed boost every time they do 2 chained jumps
        if (_chainedJumpCount % 2 == 0)
            HSpeed *= CHAINED_JUMP_HSPEED_MULT;
    }

    /// <summary>
    /// Returns a vector representing the left control stick, relative to camera
    /// space.
    /// </summary>
    /// <returns></returns>
    private Vector3 GetWalkInput()
    {
        var rawInput = new Vector3
        (
            _input.LeftStick.x,
            0,
            _input.LeftStick.y
        );
        
        // Rotate it into camera space
        var adjustedInput = 
            Camera.main.transform.forward * rawInput.z +
            Camera.main.transform.right * rawInput.x;
        
        // "flatten" it so that the y-coordinate is zero, while preserving the
        // magnitude.
        adjustedInput.y = 0;
        adjustedInput.Normalize();
        adjustedInput *= rawInput.magnitude;

        // Cap the magnitude at 1.0, because some people like to play with
        // keyboards.
        if (adjustedInput.magnitude > 1)
            adjustedInput.Normalize();

        return adjustedInput;
    }

    private bool JumpPressedRecently()
    {
        return (Time.time - EARLY_JUMP_TIME < _lastJumpButtonPressTime);
    }

    private bool CanGrabLedge()
    {
        DebugDisplay.DrawPoint(Color.red, transform.position + (Vector3.up * _ledge.LastLedgeHeight));
        if (!_ledge.LedgePresent)
            return false;

        // Only grab the ledge if it's in the sweet spot
        if (_ledge.LastLedgeHeight < BODY_HEIGHT / 2)
            return false;
        
        if (_ledge.LastLedgeHeight > BODY_HEIGHT)
            return false;

        // Only grab the ledge if we're wall-sliding
        if (!IsWallSliding)
            return false;

        return true;
    }

    private Vector3 AngleForward(float angleDeg)
    {
        return new Vector3(
            Mathf.Cos(Mathf.Deg2Rad * angleDeg),
            0,
            Mathf.Sin(Mathf.Deg2Rad * angleDeg)
        );
    }

    private Vector3 ReflectOffOfSurface(Vector3 v, Vector3 surfaceNormal)
    {
        var vectorAlongSurface = v.ProjectOnPlane(surfaceNormal);
        var vectorIntoSurface = v - vectorAlongSurface;

        return -vectorIntoSurface + vectorAlongSurface;
    }
}
