using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(IPlayerInput))]
[RequireComponent(typeof(PlayerRollAttackHitbox))]
[RequireComponent(typeof(PlayerDiveAttackHitbox))]
[RequireComponent(typeof(PlayerMotor))]
public partial class PlayerStateMachine : MonoBehaviour
{
    // Required components
    private IPlayerInput _input;
    private PlayerRollAttackHitbox _rollHitbox;
    private PlayerDiveAttackHitbox _diveHitbox;
    private PlayerMotor _motor;

    // Events
    public event Action StartedJumping;
    public event Action StartedDiving;
    public event Action GrabbedLedge;
    public event Action Bonked;

    // Computed jump/gravity values
    private float _jumpSpeed;
    private float _secondJumpSpeed;
    private float _diveJumpVspeed;
    private float _fallGravity;
    private float _riseGravity;
    private float _shortJumpRiseGravity;
    private float _wallSlideGravity => _riseGravity;

    // Accessors
    public Vector3 Forward => AngleForward(HAngleDeg);

    public int ChainedJumpCount => (_chainedJumpCount + 1) % 2;

    // State
    public float HAngleDeg {get; private set;}
    public float HSpeed {get; private set;}
    private float _storedAirHSpeed; // Temporarily stores your air HSpeed after
                                    // landing, so it can be restored if you jump
                                    // again shortly after landing.

    public enum AnimationHint
    {
        Walking,
        FreeFall,
        WallSliding,
        WallJumping,
        LedgeGrabbing,
        Rolling,
        Diving,
        Bonking
    }
    public AnimationHint CurrentAnimationHint => _currentState.GetAnimationHint();

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

    private float _lastJumpButtonPressTime = float.NegativeInfinity;
    private bool _jumpReleased;

    private float _lastAttackButtonPressTime = float.NegativeInfinity;

    private float _lastChainedJumpLandTime = 0;
    private int _chainedJumpCount = 0;

    private float _jumpRedirectTimer = 0;
    
    private float _rollCooldown = 0;
    private float _lastRollStopTime = 0;

    // Debugging metrics
    private float _debugJumpStartY;
    private float _debugJumpMaxY;

    public void Awake()
    {
        _input = GetComponent<IPlayerInput>();
        _rollHitbox = GetComponent<PlayerRollAttackHitbox>();
        _diveHitbox = GetComponent<PlayerDiveAttackHitbox>();
        _motor = GetComponent<PlayerMotor>();

        // Compute jump parameters
        var jumpValues = GravityMath.ComputeGravity(
            PlayerConstants.FIRST_JUMP_HEIGHT,
            PlayerConstants.FIRST_JUMP_RISE_TIME,
            PlayerConstants.FIRST_JUMP_FALL_TIME
        );

        _jumpSpeed   = jumpValues.JumpVelocity;
        _fallGravity = jumpValues.FallGravity;
        _riseGravity = jumpValues.RiseGravity;

        _secondJumpSpeed = GravityMath.JumpVelForHeight(
            PlayerConstants.SECOND_JUMP_HEIGHT,
            _riseGravity
        );

        _diveJumpVspeed = GravityMath.JumpVelForHeight(
            PlayerConstants.DIVE_JUMP_HEIGHT,
            PlayerConstants.DIVE_GRAVITY
        );

        Walking = new WalkingState(this, _motor);
        FreeFall = new FreeFallState(this, _motor);
        WallSliding = new WallSlidingState(this, _motor);
        WallJumping = new WallJumpingState(this, _motor);
        Rolling = new RollingState(this, _motor);
        Diving = new DivingState(this, _motor);
        Bonking = new BonkingState(this, _motor);
        GrabbingLedge = new GrabbingLedgeState(this, _motor);

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

        Debug.Log("Jump speed: " + _jumpSpeed);
    }

    /// <summary>
    /// Resets the state to a consistent baseline
    /// </summary>
    public void ResetState()
    {
        HAngleDeg = 0;
        HSpeed = 0;
        _motor.RelativeFlatVelocity = Vector3.zero;
        _storedAirHSpeed = 0;

        _lastJumpButtonPressTime = float.NegativeInfinity;
        _jumpReleased = false;
        _lastChainedJumpLandTime = 0;
        _chainedJumpCount = 0;

        ChangeState(FreeFall);

        _motor.ResetState();
        
        // Tell all the state objects to reset as well.
        // Wow, the word "state" really is overused, huh?
        foreach (var state in _allStates)
            state.ResetState();
    }

    public void Update()
    {
        if (_input.JumpPressed)
            _lastJumpButtonPressTime = Time.time;

        if (_input.AttackPressed)
            _lastAttackButtonPressTime = Time.time;
    }

    public void FixedUpdate()
    {
        DebugDisplay.PrintLine($"Rise: {_riseGravity}");
        DebugDisplay.PrintLine($"Fall: {_fallGravity}");

        AdvanceCooldowns();

        // Many states use collision status(eg: are we touching the ground?)
        // to decide if they should change to a different state.
        // We need to update this information before 
        _motor.UpdateCollisionStatus();

        // Run state logic that needs to be done early.
        // Usually, this is where state transitions happen.
        _currentState.EarlyFixedUpdate();

        // Run the current state's main logic.
        // Note that CurrentState may have been changed by EarlyFixedUpdate()
        _currentState.FixedUpdate();

        // Tell the motor to move at its current velocity.
        _motor.Move();

        // Display debugging metrics
        DebugDisplay.PrintLine("HSpeed: " + HSpeed);
        DebugDisplay.PrintLine("VSpeed: " + _motor.RelativeVSpeed);
        DebugDisplay.PrintLine("HAngleDeg: " + HAngleDeg);
        DebugDisplay.PrintLine("Chained jump count: " + _chainedJumpCount);
        DebugDisplay.PrintLine("In chained jump window: " + ChainedJumpLandedRecently());
        DebugDisplay.PrintLine("Jump height: " + (_debugJumpMaxY - _debugJumpStartY));
        DebugDisplay.PrintLine("Current state: " + CurrentAnimationHint);
    }

    private void ChangeState(AbstractPlayerState newState)
    {
        var oldState = _currentState;
        _currentState = newState;

        oldState?.OnStateExit();
        newState.OnStateEnter();
    }

    /// <summary>
    /// Certain actions, like rolling, have a cooldown period.
    /// The timer for each cooldown needs to *always* be ticking down, independent
    /// of what state we're in.  Hence, we have a separate method for it.
    /// </summary>
    private void AdvanceCooldowns()
    {
        _rollCooldown -= Time.deltaTime;
    }

    private bool ChainedJumpLandedRecently()
    {
        return (Time.fixedTime - _lastChainedJumpLandTime < PlayerConstants.CHAINED_JUMP_TIME_WINDOW);
    }

    private Vector3 AngleForward(float angleDeg)
    {
        return new Vector3(
            Mathf.Cos(Mathf.Deg2Rad * angleDeg),
            0,
            Mathf.Sin(Mathf.Deg2Rad * angleDeg)
        );
    }
}