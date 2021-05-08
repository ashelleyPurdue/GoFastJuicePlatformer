using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using PlayerStates;

[RequireComponent(typeof(IPlayerInput))]
[RequireComponent(typeof(PlayerRollAttackHitbox))]
[RequireComponent(typeof(PlayerDiveAttackHitbox))]
[RequireComponent(typeof(PlayerMotor))]
[RequireComponent(typeof(IPlayerAnimationManager))]
public class PlayerStateMachine : MonoBehaviour
{
    // Required components
    public IPlayerInput Input {get; private set;}
    public IPlayerAnimationManager Anim {get; private set;}
    public PlayerRollAttackHitbox RollHitbox {get; private set;}
    public PlayerDiveAttackHitbox DiveHitbox {get; private set;}
    public PlayerMotor Motor {get; private set;}

    // Accessors
    public Vector3 Forward => AngleForward(HAngleDeg);

    public AbstractPlayerState Walking {get; private set;}
    public AbstractPlayerState StandardJumping {get; private set;}
    public AbstractPlayerState RollJumping {get; private set;}
    public AbstractPlayerState SideFlipJumping {get; private set;}
    public AbstractPlayerState FreeFall {get; private set;}
    public AbstractPlayerState WallSliding {get; private set;}
    public AbstractPlayerState WallJumping {get; private set;}
    public AbstractPlayerState GrabbingLedge {get; private set;}
    public AbstractPlayerState Rolling {get; private set;}
    public AbstractPlayerState Diving {get; private set;}
    public AbstractPlayerState Bonking {get; private set;}

    private AbstractPlayerState[] _allStates;
    private AbstractPlayerState _currentState;

    // State
    public float HAngleDeg {get; set;}
    public float HSpeed {get; set;}
    public float StoredAirHSpeed;   // Temporarily stores your air HSpeed after
                                    // landing, so it can be restored if you jump
                                    // again shortly after landing.
    public float LastJumpButtonPressTime = float.NegativeInfinity;
    public bool JumpReleased;

    public float LastAttackButtonPressTime = float.NegativeInfinity;

    public float LastChainedJumpLandTime = 0;
    public int ChainedJumpCount = 0;

    public float LastRollStopTime = 0;
    public float LastJumpStartTime = 0;

    // Debugging metrics
    private float _debugJumpStartY;
    private float _debugJumpMaxY;

    public void Awake()
    {
        Input = GetComponent<IPlayerInput>();
        Anim = GetComponent<IPlayerAnimationManager>();
        RollHitbox = GetComponent<PlayerRollAttackHitbox>();
        DiveHitbox = GetComponent<PlayerDiveAttackHitbox>();
        Motor = GetComponent<PlayerMotor>();

        Walking = new WalkingState(this);
        StandardJumping = new StandardJumpingState(this);
        RollJumping = new RollJumpingState(this);
        SideFlipJumping = new SideFlipJumpingState(this);
        FreeFall = new FreeFallState(this);
        WallSliding = new WallSlidingState(this);
        WallJumping = new WallJumpingState(this);
        Rolling = new RollingState(this);
        Diving = new DivingState(this);
        Bonking = new BonkingState(this);
        GrabbingLedge = new GrabbingLedgeState(this);

        _allStates = new[]
        {
            Walking,
            FreeFall,
            WallSliding,
            WallJumping,
            Rolling,
            Diving,
            Bonking,
            GrabbingLedge
        };

        // Start in FreeFall
        ChangeState(FreeFall);

        Debug.Log("Jump speed: " + PlayerConstants.STANDARD_JUMP_VSPEED);
    }

    /// <summary>
    /// Resets the state to a consistent baseline
    /// </summary>
    public void ResetState()
    {
        HAngleDeg = 0;
        HSpeed = 0;
        Motor.RelativeFlatVelocity = Vector3.zero;
        StoredAirHSpeed = 0;

        LastJumpButtonPressTime = float.NegativeInfinity;
        JumpReleased = false;
        LastChainedJumpLandTime = 0;
        ChainedJumpCount = 0;

        ChangeState(FreeFall);

        Motor.ResetState();
        
        // Tell all the state objects to reset as well.
        // Wow, the word "state" really is overused, huh?
        foreach (var state in _allStates)
            state.ResetState();
    }

    public void Update()
    {
        if (Input.JumpPressed)
            LastJumpButtonPressTime = Time.time;

        if (Input.AttackPressed)
            LastAttackButtonPressTime = Time.time;

        // Let the animator know our rotation
        Anim.HAngleDeg = HAngleDeg;

        // DEV CHEAT: slow down time with a button
        DebugDisplay.PrintLine("CheatSlowTime: " + UnityEngine.Input.GetAxisRaw("CheatSlowTime"));
        Time.timeScale = Input.CheatSlowTimeHeld
            ? 0.25f
            : 1;
    }

    public void FixedUpdate()
    {
        DebugDisplay.PrintLine($"Rise: {PlayerConstants.JUMP_RISE_GRAVITY}");
        DebugDisplay.PrintLine($"Fall: {PlayerConstants.FREE_FALL_GRAVITY}");

        // Many states use collision status(eg: are we touching the ground?)
        // to decide if they should change to a different state.
        // We need to update this information before 
        Motor.UpdateCollisionStatus();

        // Run state logic that needs to be done early.
        // Usually, this is where state transitions happen.
        _currentState.EarlyFixedUpdate();

        // Run the current state's main logic.
        // Note that CurrentState may have been changed by EarlyFixedUpdate()
        _currentState.FixedUpdate();

        // Tell the motor to move at its current velocity.
        Motor.Move();

        // Display debugging metrics
        DebugDisplay.PrintLine("HSpeed: " + HSpeed);
        DebugDisplay.PrintLine("VSpeed: " + Motor.RelativeVSpeed);
        DebugDisplay.PrintLine("HAngleDeg: " + HAngleDeg);
        DebugDisplay.PrintLine("Chained jump count: " + ChainedJumpCount);
        DebugDisplay.PrintLine("In chained jump window: " + ChainedJumpLandedRecently());
        DebugDisplay.PrintLine("Jump height: " + (_debugJumpMaxY - _debugJumpStartY));
    }

    public void ChangeState(AbstractPlayerState newState)
    {
        Debug.Log($"Transitioning from {_currentState?.GetType()} to {newState?.GetType()}");
        var oldState = _currentState;
        _currentState = newState;

        oldState?.OnStateExit();
        newState.OnStateEnter();
    }

    public bool ChainedJumpLandedRecently()
    {
        return (Time.fixedTime - LastChainedJumpLandTime < PlayerConstants.CHAINED_JUMP_TIME_WINDOW);
    }

    public Vector3 AngleForward(float angleDeg)
    {
        return new Vector3(
            Mathf.Cos(Mathf.Deg2Rad * angleDeg),
            0,
            Mathf.Sin(Mathf.Deg2Rad * angleDeg)
        );
    }

    public void SyncWalkVelocityToHSpeed()
    {
        // We're about to multiply HSpeed by the "forward" direction to
        // get our walking velocity.
        var forward = AngleForward(HAngleDeg);
        
        // If we're standing on a sloped surface, then that "forward" value
        // needs to be parallel to the ground we're standing on.  Otherwise,
        // walking downhill at high speeds will look like "stair stepping".
        if (Motor.IsGrounded)
        {
            forward = forward
                .ProjectOnPlane(Motor.LastGroundNormal)
                .normalized;
        }

        Motor.RelativeFlatVelocity = HSpeed * forward;
    }
    
    public void InstantlyFaceLeftStick()
    {
        if (!IsLeftStickNeutral())
            HAngleDeg = GetHAngleDegInput();
    }

    /// <summary>
    /// Faces away from the wall that we last touched.
    /// Our new forward will be equal to that wall's normal.
    /// </summary>
    public void FaceAwayFromWall()
    {
        HAngleDeg = GetHAngleDegFromForward(Motor.LastWallNormal.Flattened());
    }

    /// <summary>
    /// Common logic that is shared by all jumping/falling states.
    /// Allows the player to influence their velocity in mid-air using the
    /// left stick.
    /// </summary>
    public void AirStrafingControls()
    {
        // Always be facing the left stick.
        // This gives the player the illusion of having more control,
        // without actually affecting their velocity.
        // It also makes it easier to tell which direction they would dive
        // in, if they were to press the dive button right now.
        InstantlyFaceLeftStick();

        // Allow the player to redirect their velocity for free for a short
        // time after jumping, in case they pressed the jump button while
        // they were still moving the stick.
        // After that time is up, air strafing controls kick in.
        if (IsInJumpRedirectTimeWindow())
        {
            SyncWalkVelocityToHSpeed();
            return;
        }

        // In the air, we let the player "nudge" their velocity by applying
        // a force in the direction the stick is being pushed.
        // Unlike on the ground, you *will* lose speed and slide around if
        // you try to change your direction.
        var inputVector = GetWalkInput();

        float accel = PlayerConstants.HACCEL_AIR;
        float maxSpeed = PlayerConstants.HSPEED_MAX_AIR;

        // Apply a force to get our new velocity.
        var oldVelocity = Motor.RelativeFlatVelocity;
        var newVelocity = Motor.RelativeFlatVelocity + (inputVector * accel * Time.deltaTime);
        
        // Only let the player accellerate up to the normal ground speed.
        // We won't slow them down if they're already going faster than
        // that, though (eg: due to a speed boost from wall jumping)
        float oldSpeed = oldVelocity.magnitude;
        float newSpeed = newVelocity.magnitude;

        bool wasAboveGroundSpeedLimit = oldSpeed > PlayerConstants.HSPEED_MAX_GROUND;
        bool nowAboveGroundSpeedLimit = newSpeed > PlayerConstants.HSPEED_MAX_GROUND;

        if (newSpeed > oldSpeed)
        {
            if (wasAboveGroundSpeedLimit)
                newSpeed = oldSpeed;
            else if (nowAboveGroundSpeedLimit)
                newSpeed = PlayerConstants.HSPEED_MAX_GROUND;
        }

        // We WILL, however, slow them down if they're going past the max
        // air speed.  That's a hard maximum.
        if (newSpeed > maxSpeed)
            newSpeed = maxSpeed;

        Motor.RelativeFlatVelocity = newVelocity.normalized * newSpeed;

        // Keep HSpeed up-to-date, so it'll be correct when we land.
        HSpeed = Motor.RelativeFlatVelocity.ComponentAlong(Forward);
    }

    public void DebugRecordJumpStart()
    {
        _debugJumpStartY = Motor.transform.position.y;
        _debugJumpMaxY = Motor.transform.position.y;
    }

    public void DebugRecordWhileJumping()
    {
        if (Motor.transform.position.y > _debugJumpMaxY)
            _debugJumpMaxY = Motor.transform.position.y;
    }

    /// <summary>
    /// Returns a vector representing the left control stick, relative to camera
    /// space.
    /// </summary>
    /// <returns></returns>
    public Vector3 GetWalkInput()
    {
        return InputUtils.LeftStickToWorldSpace(Input.LeftStick);
    }
    
    /// <summary>
    /// Returns the intended HAngleDeg based on the left stick's input, relative
    /// to camera space.
    /// </summary>
    /// <returns></returns>
    public float GetHAngleDegInput()
    {
        var inputVector = GetWalkInput();
        return Mathf.Atan2(inputVector.z, inputVector.x) * Mathf.Rad2Deg;
    }

    /// <summary>
    /// Is the left stick in a neutral position(IE: in the deadzone?)
    /// </summary>
    /// <returns></returns>
    public bool IsLeftStickNeutral()
    {
        return Input.LeftStick.magnitude < PlayerConstants.LEFT_STICK_DEADZONE;
    }
    public float LeftStickForwardsComponent()
    {
        var inputVector = GetWalkInput();
        var forward = AngleForward(HAngleDeg);
        return inputVector.ComponentAlong(forward);
    }

    /// <summary>
    /// Returns the HAngleDeg that would result in the given forward.
    /// </summary>
    /// <param name="forward"></param>
    /// <returns></returns>
    public float GetHAngleDegFromForward(Vector3 forward)
    {
        var flatForward = forward.Flattened();
        float radians = Mathf.Atan2(flatForward.z, flatForward.x);
        return radians * Mathf.Rad2Deg;
    }

    public bool JumpPressedRecently()
    {
        return (Time.time - PlayerConstants.EARLY_JUMP_TIME < LastJumpButtonPressTime);
    }

    public bool AttackPressedRecently()
    {
        return (Time.time - Time.fixedDeltaTime < LastAttackButtonPressTime);
    }

    public bool IsRollOnCooldown()
    {
        return (Time.time - PlayerConstants.ROLL_COOLDOWN < LastRollStopTime);
    }

    public bool IsInJumpRedirectTimeWindow()
    {
        return (Time.time - PlayerConstants.JUMP_REDIRECT_TIME < LastJumpStartTime);
    }

    public bool ShouldBonkAgainstWall()
    {
        return 
            Motor.IsTouchingWall &&
            Forward.ComponentAlong(-Motor.LastWallNormal) > 0.5f;
    }
}