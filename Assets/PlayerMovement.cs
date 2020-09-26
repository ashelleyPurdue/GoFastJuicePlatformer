using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private readonly float _groundDetectorThickness = 0.1f;
    private readonly float _groundDetectorRadius = 0.5f;
    private readonly float _gravity = 40;
    private readonly float _hSpeedMax = 5;
    private readonly float _hAccelMax = 5;
    private readonly float _rotSpeedDeg = 360 * 2;
    private readonly float _friction = 10;

    private readonly float _coyoteTime = 0.1f;      // Allows you to press the jump button a little "late" and still jump
    private readonly float _earlyJumpTime = 0.1f;   // Allows you to press the jump button a little "early" and still jump

    private float _hAngle = 0;
    private float _hSpeed = 0;
    private float _vSpeed = 0;

    private float _lastGroundedTime = 0;
    private float _lastJumpButtonPressTime = 0;

    private Transform _currentGround;
    private Vector3 _lastPositionRelativeToGround;
    private Vector3 _groundVelocity;

    private CharacterController _controller;

    public void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    public void Update()
    {
        if (Input.GetButtonDown("Jump"))
            _lastJumpButtonPressTime = Time.time;
    }

    public void FixedUpdate()
    {
        UpdateGroundState();
        ApplyGravityAndJumping();
        ApplyHorizontalMovement();

        // Compute the velocity vector and move
        var velocity = new Vector3
        (
            _hSpeed * Mathf.Cos(_hAngle),
            _vSpeed,
            _hSpeed * Mathf.Sin(_hAngle)
        );
        velocity += _groundVelocity;
        _controller.Move(velocity * Time.deltaTime);

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
        // Apply gravity
        _vSpeed -= _gravity * Time.deltaTime;

        // Stop falling if we hit the ground.
        if (IsGrounded())
            _vSpeed = 0;

        // Jump when the button is pressed and we're on the ground.
        // Well, OK, that's a little too strict.  
        // We should let the player press the jump button a little bit before hitting the ground.
        // And we should also let them do it a little bit after leaving the ground.
        bool jumpPressedRecently = (Time.time - _earlyJumpTime < _lastJumpButtonPressTime);
        bool wasGroundedRecently = (Time.time - _coyoteTime < _lastGroundedTime);
        if (wasGroundedRecently && jumpPressedRecently)
            _vSpeed = 20;
    }

    private void ApplyHorizontalMovement()
    {
        var inputVector = GetWalkInput();
        
        if (IsGrounded())
        {
            // Speed up/slow down with the left stick
            float hSpeedIntended = inputVector.magnitude * _hSpeedMax;
            float accel = _hSpeed < hSpeedIntended
                ? _hAccelMax
                : _friction;

            _hSpeed = Mathf.MoveTowards(_hSpeed, hSpeedIntended, accel * Time.deltaTime);


            // Rotate with the left stick
            if (inputVector.magnitude > 0.001f)
            {
                // Gradually rotate until we're facing the direction the stick
                // is pointing
                float targetAngleDeg = Mathf.Atan2(inputVector.z, inputVector.x) * Mathf.Rad2Deg;
                float hAngleDeg = _hAngle * Mathf.Rad2Deg;
                hAngleDeg = Mathf.MoveTowardsAngle(hAngleDeg, targetAngleDeg, _rotSpeedDeg * Time.deltaTime);

                // ...unless we're going really slow, then just pivot instantly.
                if (_hSpeed < 0.1f)
                    hAngleDeg = targetAngleDeg;

                _hAngle = hAngleDeg * Mathf.Deg2Rad;
            }
        }
        else
        {
            // TODO: different controls for when we're in the air
        }
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
            Input.GetAxisRaw("Horizontal"),
            0,
            Input.GetAxisRaw("Vertical")
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
    /// Capsules suck for platformer collision.
    /// Players demand that their feet be CYLINDERS!
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
            _groundDetectorRadius,
            _groundDetectorThickness,
            Vector3.down,
            _groundDetectorThickness / 2
        );

        foreach (var h in hits)
        {
            if (h.collider.transform != this.transform)
                return h.collider.transform;
        }
        return null;
    }
}
