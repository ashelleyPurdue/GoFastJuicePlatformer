using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(IPlayerInput))]
[RequireComponent(typeof(PlayerGroundDetector))]
[RequireComponent(typeof(PlayerLedgeDetector))]
[RequireComponent(typeof(PlayerWallDetector))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerRollAttackHitbox))]
[RequireComponent(typeof(PlayerDiveAttackHitbox))]
public partial class PlayerMovement : MonoBehaviour
{
    // Required components
    private IPlayerInput _input;
    private PlayerGroundDetector _ground;
    private PlayerLedgeDetector _ledge;
    private PlayerWallDetector _wall;
    private PlayerRollAttackHitbox _rollHitbox;
    private PlayerDiveAttackHitbox _diveHitbox;
    private CharacterController _controller;


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

    public Vector3 TotalVelocity => 
        _ground.GroundVelocity +
        _walkVelocity +
        (Vector3.up * VSpeed);

    public int ChainedJumpCount => (_chainedJumpCount + 1) % 2;

    // State
    public float HAngleDeg {get; private set;}
    public float HSpeed {get; private set;}
    public float VSpeed {get; private set;}
    private Vector3 _walkVelocity;

    public enum State
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
    public State CurrentState
    {
        get => _currentState;
        set => ChangeState(value);
    }
    private State _currentState;

    private Dictionary<State, AbstractPlayerState> _states;



    private float _lastJumpButtonPressTime = float.NegativeInfinity;
    private bool _jumpReleased;

    private float _lastAttackButtonPressTime = float.NegativeInfinity;

    private float _chainedJumpTimer = 0;
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
        _ground = GetComponent<PlayerGroundDetector>();
        _ledge = GetComponent<PlayerLedgeDetector>();
        _wall = GetComponent<PlayerWallDetector>();
        _rollHitbox = GetComponent<PlayerRollAttackHitbox>();
        _diveHitbox = GetComponent<PlayerDiveAttackHitbox>();
        _controller = GetComponent<CharacterController>();

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

        _states = new Dictionary<State, AbstractPlayerState>
        {
            {State.Walking, new WalkingState(this)},
            {State.FreeFall, new FreeFallState(this)},
            {State.WallSliding, new WallSlidingState(this)},
            {State.WallJumping, new WallJumpingState(this)},
            {State.Rolling, new RollingState(this)},
            {State.Diving, new DivingState(this)},
            {State.Bonking, new BonkingState(this)},
            {State.LedgeGrabbing, new GrabbingLedgeState(this)}
        };

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
        _chainedJumpTimer = 0;
        _chainedJumpCount = 0;

        CurrentState = State.FreeFall;

        _ground.RecordFootprintPos();
        _ground.UpdateGroundState();
        _ground.RecordFootprintPos();

        _wall.UpdateWallState();
        _ledge.UpdateLedgeDetectorState();

        // Tell all the state objects to reset as well.
        // Wow, the word "state" really is overused, huh?
        foreach (var state in _states.Keys)
            _states[state].ResetState();
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

        if (_input.AttackPressed)
            _lastAttackButtonPressTime = Time.time;
    }

    public void FixedUpdate()
    {
        DebugDisplay.PrintLine($"Rise: {_riseGravity}");
        DebugDisplay.PrintLine($"Fall: {_fallGravity}");

        // Detect various states
        _ground.UpdateGroundState();
        _ledge.UpdateLedgeDetectorState();
        _wall.UpdateWallState();

        AdvanceCooldowns();

        // Run state logic that needs to be done early.
        // Usually, this is where state transitions happen.
        _states[CurrentState].EarlyFixedUpdate();

        // Run the current state's main logic.
        // Note that CurrentState may have been changed by EarlyFixedUpdate()
        _states[CurrentState].FixedUpdate();

        // Move with the current velocity
        _controller.Move(TotalVelocity * Time.deltaTime);

        // Run state logic that needs to be done after the player has been
        // moved.
        _states[CurrentState].LateFixedUpdate();

        // Remember moving-platform stuff for next frame
        _ground.RecordFootprintPos();

        // Display debugging metrics
        DebugDisplay.PrintLine("HSpeed: " + HSpeed);
        DebugDisplay.PrintLine("VSpeed: " + VSpeed);
        DebugDisplay.PrintLine("HAngleDeg: " + HAngleDeg);
        DebugDisplay.PrintLine("Chained jump count: " + _chainedJumpCount);
        DebugDisplay.PrintLine("Chained jump timer: " + _chainedJumpTimer);
        DebugDisplay.PrintLine("Jump height: " + (_debugJumpMaxY - _debugJumpStartY));
        DebugDisplay.PrintLine("Current state: " + CurrentState);
    }

    private void ChangeState(State newState)
    {
        var oldState = CurrentState;
        _currentState = newState;

        if (_states.ContainsKey(oldState))
            _states[oldState].OnStateExit();
        
        if (_states.ContainsKey(newState))
            _states[newState].OnStateEnter();
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

    private void StartGroundJump()
    {
        // DEBUG: Record debug stats
        _debugJumpStartY = transform.position.y;
        _debugJumpMaxY = transform.position.y;

        InstantlyFaceLeftStick();

        VSpeed = _jumpSpeed;

        // Jump heigher and get a speed boost every time they do 2 chained jumps
        if (_chainedJumpCount % 2 == 1)
        {
            VSpeed = _secondJumpSpeed;
            HSpeed *= PlayerConstants.CHAINED_JUMP_HSPEED_MULT;
        }

        SyncWalkVelocityToHSpeed();

        // Book keeping
        _chainedJumpCount++;
        _jumpReleased = false;
        _jumpRedirectTimer = PlayerConstants.JUMP_REDIRECT_TIME;
        StartedJumping?.Invoke();
    }

    private void StartWallJump()
    {
        // DEBUG: Record debug stats
        _debugJumpStartY = transform.position.y;
        _debugJumpMaxY = transform.position.y;

        VSpeed = _jumpSpeed;

        // Reflect off of the wall at the angle we approached it at
        var kickDir = ReflectOffOfSurface(Forward, _wall.LastWallNormal);
        HAngleDeg = Mathf.Rad2Deg * Mathf.Atan2(kickDir.z, kickDir.x);

        // Kick off of the wall at a speed that's *at least* WALL_JUMP_MIN_HSPEED.
        // If we were already going faster than that before touching the wall,
        // then use *that* speed instead.  This way, you'll never lose speed by
        // wall jumping.
        HSpeed = Mathf.Max(
            PlayerConstants.WALL_JUMP_MIN_HSPEED,
            HSpeed
        );

        // On top of that, give the player a *boost* to their HSpeed, as a reward
        // for wall jumping.
        HSpeed *= PlayerConstants.WALL_JUMP_HSPEED_MULT;

        SyncWalkVelocityToHSpeed();

        // Book keeping
        _chainedJumpCount = 0;
        _jumpReleased = false;
        ChangeState(State.WallJumping);
        StartedJumping?.Invoke();
    }

    private void StartRollJump()
    {
        // DEBUG: Record debug stats
        _debugJumpStartY = transform.position.y;
        _debugJumpMaxY = transform.position.y;

        InstantlyFaceLeftStick();

        // Cap their HSpeed at something reasonable.
        // Otherwise, they'd conserve their rolling HSpeed into the
        // jump, which would result in a *super* ridiculous long jump.
        // We only want rolling jumps to be *slightly* ridiculous.
        HSpeed = PlayerConstants.ROLL_JUMP_HSPEED;
        VSpeed = _jumpSpeed;
        SyncWalkVelocityToHSpeed();

        _chainedJumpCount = 0;
        _jumpReleased = false;
        _jumpRedirectTimer = PlayerConstants.JUMP_REDIRECT_TIME;
        CurrentState = State.Walking;
        StartedJumping?.Invoke();
    }
    
    private void InstantlyFaceLeftStick()
    {
        if (!IsLeftStickNeutral())
            HAngleDeg = GetHAngleDegInput();
    }

    private void SyncWalkVelocityToHSpeed()
    {
        _walkVelocity = HSpeed * AngleForward(HAngleDeg);
    }

    /// <summary>
    /// Returns a vector representing the left control stick, relative to camera
    /// space.
    /// </summary>
    /// <returns></returns>
    private Vector3 GetWalkInput()
    {
        return InputUtils.LeftStickToWorldSpace(_input.LeftStick);
    }

    /// <summary>
    /// Returns the intended HAngleDeg based on the left stick's input, relative
    /// to camera space.
    /// </summary>
    /// <returns></returns>
    private float GetHAngleDegInput()
    {
        var inputVector = GetWalkInput();
        return Mathf.Atan2(inputVector.z, inputVector.x) * Mathf.Rad2Deg;
    }

    /// <summary>
    /// Is the left stick in a neutral position(IE: in the deadzone?)
    /// </summary>
    /// <returns></returns>
    private bool IsLeftStickNeutral()
    {
        return _input.LeftStick.magnitude < PlayerConstants.LEFT_STICK_DEADZONE;
    }

    private float LeftStickForwardsComponent()
    {
        var inputVector = GetWalkInput();
        var forward = AngleForward(HAngleDeg);
        return inputVector.ComponentAlong(forward);
    }

    private bool WasGroundedRecently()
    {
        return (Time.time - PlayerConstants.COYOTE_TIME < _ground.LastGroundedTime);
    }

    private bool StoppedRollingRecently()
    {
        return (Time.time - PlayerConstants.COYOTE_TIME < _lastRollStopTime);
    }

    private bool JumpPressedRecently()
    {
        return (Time.time - PlayerConstants.EARLY_JUMP_TIME < _lastJumpButtonPressTime);
    }

    private bool AttackPressedRecently()
    {
        return (Time.time - Time.fixedDeltaTime < _lastAttackButtonPressTime);
    }

    private Vector3 AngleForward(float angleDeg)
    {
        return new Vector3(
            Mathf.Cos(Mathf.Deg2Rad * angleDeg),
            0,
            Mathf.Sin(Mathf.Deg2Rad * angleDeg)
        );
    }

    /// <summary>
    /// Returns the HAngleDeg that would result in the given forward.
    /// </summary>
    /// <param name="forward"></param>
    /// <returns></returns>
    private float GetHAngleDegFromForward(Vector3 forward)
    {
        var flatForward = forward.Flattened();
        float radians = Mathf.Atan2(flatForward.z, flatForward.x);
        return radians * Mathf.Rad2Deg;
    }

    private Vector3 ReflectOffOfSurface(Vector3 v, Vector3 surfaceNormal)
    {
        var vectorAlongSurface = v.ProjectOnPlane(surfaceNormal);
        var vectorIntoSurface = v - vectorAlongSurface;

        return -vectorIntoSurface + vectorAlongSurface;
    }

    private bool ShouldBonkAgainstWall()
    {
        return 
            _wall.IsTouchingWall &&
            Forward.ComponentAlong(-_wall.LastWallNormal) > 0.5f;
    }
}