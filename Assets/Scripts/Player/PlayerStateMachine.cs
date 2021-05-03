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
    public float DebugJumpStartYFooBar;
    public float DebugJumpMaxYFooBar;

    public void Awake()
    {
        Input = GetComponent<IPlayerInput>();
        Anim = GetComponent<IPlayerAnimationManager>();
        RollHitbox = GetComponent<PlayerRollAttackHitbox>();
        DiveHitbox = GetComponent<PlayerDiveAttackHitbox>();
        Motor = GetComponent<PlayerMotor>();

        Walking = new WalkingState(this);
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
        DebugDisplay.PrintLine("Jump height: " + (DebugJumpMaxYFooBar - DebugJumpStartYFooBar));
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

    public void StartGroundJump()
    {
        // DEBUG: Record debug stats
        DebugJumpStartYFooBar = Motor.transform.position.y;
        DebugJumpMaxYFooBar = Motor.transform.position.y;

        InstantlyFaceLeftStick();

        Motor.RelativeVSpeed = PlayerConstants.STANDARD_JUMP_VSPEED;

        // If this was a chained jump, restore their stored hspeed
        if (ChainedJumpLandedRecently())
            HSpeed = StoredAirHSpeed;

        // Jump heigher and get a speed boost every time they do 2 chained jumps
        bool isChainedJump = ChainedJumpCount % 2 == 1;
        if (isChainedJump)
        {
            Motor.RelativeVSpeed = PlayerConstants.CHAIN_JUMP_VSPEED;
            HSpeed *= PlayerConstants.CHAINED_JUMP_HSPEED_MULT;
        }

        SyncWalkVelocityToHSpeed();

        // Book keeping
        ChainedJumpCount++;
        JumpReleased = false;
        LastJumpStartTime = Time.time;

        // Trigger animation
        string anim = isChainedJump
            ? PlayerAnims.CHAINED_JUMP
            : PlayerAnims.STANDARD_JUMP;
        Anim.Set(anim);
    }
    public void StartWallJump()
    {
        // DEBUG: Record debug stats
        DebugJumpStartYFooBar = Motor.transform.position.y;
        DebugJumpMaxYFooBar = Motor.transform.position.y;

        Motor.RelativeVSpeed = PlayerConstants.WALL_JUMP_VSPEED;

        // Kick off of the wall at a speed that's *at least* WALL_JUMP_MIN_HSPEED.
        // If we were already going faster than that before touching the wall,
        // then use *that* speed instead.  This way, you'll never lose speed by
        // wall jumping.
        FaceAwayFromWall();
        HSpeed = Mathf.Max(
            PlayerConstants.WALL_JUMP_MIN_HSPEED,
            HSpeed
        );

        // On top of that, give the player a *boost* to their HSpeed, as a reward
        // for wall jumping.
        HSpeed *= PlayerConstants.WALL_JUMP_HSPEED_MULT;

        SyncWalkVelocityToHSpeed();

        // Book keeping
        ChainedJumpCount = 1;  // The next normal jump after landing will
                                // be a chained jump.
        JumpReleased = false;
        ChangeState(WallJumping);

        // Trigger animation
        Anim.Set(PlayerAnims.STANDARD_JUMP);
    }

    public void StartRollJump()
    {
        // DEBUG: Record debug stats
        DebugJumpStartYFooBar = Motor.transform.position.y;
        DebugJumpMaxYFooBar = Motor.transform.position.y;

        InstantlyFaceLeftStick();

        // Cap their HSpeed at something reasonable.
        // Otherwise, they'd conserve their rolling HSpeed into the
        // jump, which would result in a *super* ridiculous long jump.
        // We only want rolling jumps to be *slightly* ridiculous.
        HSpeed = PlayerConstants.ROLL_JUMP_HSPEED;
        Motor.RelativeVSpeed = PlayerConstants.STANDARD_JUMP_VSPEED;
        SyncWalkVelocityToHSpeed();

        ChainedJumpCount = 0;
        JumpReleased = false;
        LastJumpStartTime = Time.time;
        ChangeState(Walking);
        
        // Trigger animation
        Anim.Set(PlayerAnims.STANDARD_JUMP);
    }

    public void StartSideFlipJump()
    {
        // DEBUG: Record debug stats
        DebugJumpStartYFooBar = Motor.transform.position.y;
        DebugJumpMaxYFooBar = Motor.transform.position.y;

        // TODO: Use separate constants for this.
        Motor.RelativeVSpeed = PlayerConstants.STANDARD_JUMP_VSPEED * 1.25f;
        HSpeed = PlayerConstants.HSPEED_MAX_GROUND;
        SyncWalkVelocityToHSpeed();

        // Book keeping
        // NOTE: A side flip never acts as a chained jump, but it still adds
        // to the chain jump count.
        ChainedJumpCount++;
        JumpReleased = false;
        LastJumpStartTime = Time.time;
        
        // Trigger animation
        Anim.Set(PlayerAnims.SIDE_FLIP);
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