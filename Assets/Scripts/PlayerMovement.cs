using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(IPlayerInput))]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // Required components
    private IPlayerInput _input;
    private CharacterController _controller;

    // Constants
    public const float GROUND_DETECTOR_THICKNESS = 0.1f;
    public const float GROUND_DETECTOR_RADIUS = 0.5f;

    public const float RISING_GRAVITY = 40;
    public const float FALLING_GRAVITY = 43;

    public const float HSPEED_MAX_GROUND = 8;
    public const float HSPEED_MAX_AIR = 10;

    public const float HACCEL_GROUND = 15;
    public const float HACCEL_AIR = 9;
    public const float HACCEL_AIR_EXTRA = 2;
    public const float HACCEL_AIR_BACKWARDS = 15;

    public const float BONK_SPEED = -3;
    public const float LEDGE_GRAB_SPEED = 10;

    public const float ROT_SPEED_DEG = 360 * 2;
    public const float FRICTION_GROUND = 20;

    public const float COYOTE_TIME = 0.1f;      // Allows you to press the jump button a little "late" and still jump
    public const float EARLY_JUMP_TIME = 0.1f;  // Allows you to press the jump button a little "early" and still jump
    
    public const float MAX_PIVOT_SPEED = 0.25f; // If you're below this speed, you can pivot on a dime.

    // Events
    public UnityEvent StartedJumping;

    // Accessors
    public Vector3 TotalVelocity => _groundVelocity + _walkVelocity + (Vector3.up * VSpeed);

    // State
    public float HAngle {get; private set;}
    public float HSpeed {get; private set;}
    public float VSpeed {get; private set;}

    private float _lastGroundedTime = 0;
    private float _lastJumpButtonPressTime = 0;

    private Transform _currentGround;
    private Vector3 _lastPositionRelativeToGround;
    private Vector3 _groundVelocity;
    private Vector3 _walkVelocity;


    public void Awake()
    {
        _input = GetComponent<IPlayerInput>();
        _controller = GetComponent<CharacterController>();
    }

    public void Update()
    {
        if (_input.JumpPressed)
            _lastJumpButtonPressTime = Time.time;
    }

    public void FixedUpdate()
    {
        UpdateGroundState();
        ApplyGravityAndJumping();
        ApplyHorizontalMovement();
        TryLedgeGrab();

        _controller.Move(TotalVelocity * Time.deltaTime);

        // Remember moving-platform stuff for next frame
        if (IsGrounded())
            _lastPositionRelativeToGround = _currentGround.InverseTransformPoint(transform.position);
    }

    private void UpdateGroundState()
    {
        var previousGround = _currentGround;
        _currentGround = GetGround();

        // Record the last time we were grounded
        if (IsGrounded())
            _lastGroundedTime = Time.time;

        // Calculate how fast the ground is moving (aka: the ground velocity)
        if (IsGrounded() && _currentGround == previousGround)
        {
            // Figure out where our "foot prints" have moved to
            var currentFootprintsPos = _currentGround.TransformPoint(_lastPositionRelativeToGround);
            var lastFootprintsPos = transform.position;

            // Figure out how much the footprints moved, and move by that much
            var deltaFootprints = currentFootprintsPos - lastFootprintsPos;
            _groundVelocity = deltaFootprints / Time.deltaTime;
        }
        else
        {
            // If we're not on a platform, then the ground velocity is zero.
            // If we're standing on a *different* platform than before, then we
            // have no way of tracking its velocity, so we'll just cheat and set
            // it to zero in that case too.
            _groundVelocity = Vector3.zero;
        }
    }

    private void ApplyGravityAndJumping()
    {
        // Apply gravity to the VSpeed.
        // Use more gravity when we're falling so the jump arc feels "squishier"
        float gravity = VSpeed > 0
            ? RISING_GRAVITY
            : FALLING_GRAVITY;

        VSpeed -= gravity * Time.deltaTime;

        if (IsGrounded())
        {
            // Stop falling when we hit the ground.
            VSpeed = 0;

            // If we obtained negative hspeed while in the air(EG: from air braking),
            // bring it back to zero so the player doesn't go flying backwards.
            if (HSpeed < 0)
                HSpeed = 0;
        }

        // Jump when the button is pressed and we're on the ground.
        // Well, OK, that's a little too strict.  
        // We should let the player press the jump button a little bit before hitting the ground.
        // And we should also let them do it a little bit after leaving the ground.
        bool jumpPressedRecently = (Time.time - EARLY_JUMP_TIME < _lastJumpButtonPressTime);
        bool wasGroundedRecently = (Time.time - COYOTE_TIME < _lastGroundedTime);

        if (wasGroundedRecently && jumpPressedRecently)
        {
            VSpeed = 15;
            StartedJumping.Invoke();
        }

        // Start going downwards if you bonk your head on the ceiling.
        // Don't bonk your head!
        if (VSpeed > 0 && IsBonkingHead())
            VSpeed = BONK_SPEED;
    }

    private void ApplyHorizontalMovement()
    {
        var inputVector = GetWalkInput();
        
        // On the ground, we let the player turn without sliding around or losing
        // speed.
        // We do this by keeping track of their speed and angle separately.
        // The target speed is controlled by the magnitude of the left stick.
        // The target angle is controlled by the direction of the left stick.
        if (IsGrounded())
        {
            // Speed up/slow down with the left stick
            float hSpeedIntended = inputVector.magnitude * HSPEED_MAX_GROUND;
            float accel = HSpeed < hSpeedIntended
                ? HACCEL_GROUND
                : FRICTION_GROUND;

            HSpeed = Mathf.MoveTowards(HSpeed, hSpeedIntended, accel * Time.deltaTime);


            // Rotate with the left stick
            if (inputVector.magnitude > 0.001f)
            {
                // Gradually rotate until we're facing the direction the stick
                // is pointing
                float targetAngleDeg = Mathf.Atan2(inputVector.z, inputVector.x) * Mathf.Rad2Deg;
                float hAngleDeg = HAngle * Mathf.Rad2Deg;
                hAngleDeg = Mathf.MoveTowardsAngle(hAngleDeg, targetAngleDeg, ROT_SPEED_DEG * Time.deltaTime);

                // ...unless we're going really slow, then just pivot instantly.
                if (HSpeed < MAX_PIVOT_SPEED)
                    hAngleDeg = targetAngleDeg;

                HAngle = hAngleDeg * Mathf.Deg2Rad;
            }

            // Convert the HSpeed and HAngle to _walkVelocity
            _walkVelocity = new Vector3(
                HSpeed * Mathf.Cos(HAngle),
                0,
                HSpeed * Mathf.Sin(HAngle)
            );
        }

        // In the air, we let the player "nudge" their velocity by applying a
        // force in the direction the stick is being pushed.
        // Unlike on the ground, you *will* lose speed and slide around if you
        // try to change your direction.
        if (!IsGrounded())
        {
            Vector3 forward = new Vector3(
                Mathf.Cos(HAngle),
                0,
                Mathf.Sin(HAngle)
            );
            
            bool pushingBackwards = ComponentAlong(inputVector, forward) < -0.5f;
            bool pushingForwards = ComponentAlong(inputVector, forward) > 0.75f;
            bool movingForwards = ComponentAlong(_walkVelocity.normalized, forward) > 0;

            float accel = HACCEL_AIR;
            float maxSpeed = HSPEED_MAX_GROUND;

            // Give them a little bit of help if they're pushing backwards
            // on the stick, so it's easier to "abort" a poorly-timed jump.
            if (pushingBackwards)
                accel = HACCEL_AIR_BACKWARDS;

            // Let the player exceed their usual max speed if they're moving
            // forward.
            // This makes bunny hopping slightly faster than walking, which I
            // hear makes your game more popular.
            if (movingForwards)
                maxSpeed = HSPEED_MAX_AIR;
            
            // If the player is already going faster than their usual max speed,
            // make it a little harder to accelerate past it.
            if (pushingForwards && _walkVelocity.magnitude >= HSPEED_MAX_GROUND)
                accel = HACCEL_AIR_EXTRA;

            // WHEW.  Finially we can apply a force to the player.
            // Think there were enough special cases, earlier?
            _walkVelocity += inputVector * accel * Time.deltaTime;
            if (_walkVelocity.magnitude > maxSpeed)
            {
                _walkVelocity.Normalize();
                _walkVelocity *= maxSpeed;
            }

            // Keep HSpeed up-to-date, so it'll be correct when we land.
            HSpeed = _walkVelocity.magnitude;
        }

        DebugDisplay.PrintLineFixed($"HSpeed: {HSpeed}");
    }

    private void TryLedgeGrab()
    {
        // HACK: Just keep moving the player up if they can grab a ledge.
        // Their forward momentum will propel them forward, making it look like
        // they did a ledge vault.
        if (CanGrabLedge())
            VSpeed = LEDGE_GRAB_SPEED;
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

        return adjustedInput;
    }

    /// <summary>
    /// Use this instead of CharacterController's built-in isGrounded property.
    /// It uses a cylinder-shaped hitbox for the feet, instead of capsule-shaped.
    /// Capsules suck for platformers; players demand that their feet be CYLINDERS!
    /// </summary>
    /// <returns></returns>
    private bool IsGrounded()
    {
        return _currentGround != null;
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

    private float ComponentAlong(Vector3 a, Vector3 b)
    {
        float dot = Vector3.Dot(a, b);
        float mag = b.magnitude;
        return dot / mag;
    }

    private bool IsBonkingHead()
    {
        return CylinderPhysics.CylinderCast(
            transform.position + (Vector3.up * 2),
            GROUND_DETECTOR_RADIUS,
            GROUND_DETECTOR_THICKNESS,
            Vector3.up,
            GROUND_DETECTOR_THICKNESS / 2
        );
    }

    private bool CanGrabLedge()
    {
        // Only grab the ledge if in the air
        if (IsGrounded())
            return false;
        
        // Only grab the ledge if we're actually moving in the direction we're
        // facing.
        var forward = new Vector3(
            Mathf.Cos(HAngle),
            0,
            Mathf.Sin(HAngle)
        );

        float forwardVelocity = ComponentAlong(_walkVelocity, forward);

        if (forwardVelocity < 0.01f)
            return false;

        // Do 2 box casts in front of us: one for our upper body, and one for
        // our lower body.
        // The lower body should detect a wall, while the upper body should not.

        const float bodyRadius = 0.5f;
        const float bodyHeight = 2;
        float distance = forwardVelocity * Time.deltaTime;

        var lowerBodyStart = transform.position 
            + (forward * bodyRadius)
            + (forward * distance / 2)
            + (Vector3.up * bodyHeight / 4);
        var upperBodyStart = lowerBodyStart + (Vector3.up * bodyHeight / 2);

        var halfExtents = new Vector3(
            bodyRadius,
            bodyHeight / 4,
            distance / 2
        );

        var orientation = Quaternion.LookRotation(forward, Vector3.up);

        bool lowerBody = Physics.CheckBox(
            lowerBodyStart,
            halfExtents,
            orientation
        );
        bool upperBody = Physics.CheckBox(
            upperBodyStart,
            halfExtents,
            orientation
        );
        
        return lowerBody && !upperBody;
    }
}
