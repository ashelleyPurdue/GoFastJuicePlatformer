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
public class PlayerMovement : MonoBehaviour
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

    private abstract class AbstractPlayerState
    {
        protected PlayerMovement _shared;
        protected PlayerGroundDetector _ground => _shared._ground;
        protected PlayerWallDetector _wall => _shared._wall;
        protected PlayerLedgeDetector _ledge => _shared._ledge;

        protected IPlayerInput _input => _shared._input;

        protected float HAngleDeg
        {
            get => _shared.HAngleDeg;
            set => _shared.HAngleDeg = value;
        }
        protected float HSpeed
        {
            get => _shared.HSpeed;
            set => _shared.HSpeed = value;
        }
        protected float VSpeed
        {
            get => _shared.VSpeed;
            set => _shared.VSpeed = value;
        }
        
        public AbstractPlayerState(PlayerMovement shared)
        {
            _shared = shared;
        }

        public virtual void ResetState() {}

        public virtual void OnStateEnter() {}
        public virtual void OnStateExit() {}

        public virtual void EarlyFixedUpdate() {}
        public virtual void FixedUpdate() {}
        public virtual void LateFixedUpdate() {}

        protected void ChangeState(State newState)
        {
            _shared.ChangeState(newState);
        }

        protected void SyncWalkVelocityToHSpeed() => _shared.SyncWalkVelocityToHSpeed();
        protected void InstantlyFaceLeftStick() => _shared.InstantlyFaceLeftStick();

        protected bool WasGroundedRecently() => _shared.WasGroundedRecently();

        protected Vector3 GetWalkInput() => _shared.GetWalkInput();
        protected float GetHAngleDegInput() => _shared.GetHAngleDegInput();

        protected bool IsLeftStickNeutral() => _shared.IsLeftStickNeutral();
        protected float LeftStickForwardsComponent() => _shared.LeftStickForwardsComponent();

        protected bool JumpPressedRecently() => _shared.JumpPressedRecently();
        protected bool AttackPressedRecently() => _shared.AttackPressedRecently();
    }
    private Dictionary<State, AbstractPlayerState> _states;

    private void ChangeState(State newState)
    {
        var oldState = CurrentState;
        _currentState = newState;

        if (_states.ContainsKey(oldState))
            _states[oldState].OnStateExit();
        
        if (_states.ContainsKey(newState))
            _states[newState].OnStateEnter();
    }

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

    /// <summary>
    /// Certain actions, like rolling, have a cooldown period.
    /// The timer for each cooldown needs to *always* be ticking down, independent
    /// of what state we're in.  Hence, we have a separate method for it.
    /// </summary>
    private void AdvanceCooldowns()
    {
        _rollCooldown -= Time.deltaTime;
    }

    private class WalkingState : AbstractPlayerState
    {
        public WalkingState(PlayerMovement shared)
            : base(shared) {}

        public override void ResetState() {}

        public override void EarlyFixedUpdate()
        {
            if (!_ground.IsGrounded)
                ChangeState(State.FreeFall);
        }
        public override void FixedUpdate()
        {
            // Start the chained jump timer once we land
            if (!_ground.WasGroundedLastFrame)
                _shared._chainedJumpTimer = PlayerConstants.CHAINED_JUMP_TIME_WINDOW;

            // Reset the chained jump count if you wait too long after landing
            _shared._chainedJumpTimer -= Time.deltaTime;
            if (_shared._chainedJumpTimer < 0)
            {
                _shared._chainedJumpTimer = 0;
                _shared._chainedJumpCount = 0;
            }

            Physics();
            StickControls();
            ButtonControls();
            
            _shared.SyncWalkVelocityToHSpeed();
        }
        private void Physics()
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
        }

        private void StickControls()
        {
            // On the ground, we let the player turn without sliding around or losing
            // speed.
            // We do this by keeping track of their speed and angle separately.
            // The target speed is controlled by the magnitude of the left stick.
            // The target angle is controlled by the direction of the left stick.

            // Speed up/slow down with the left stick
            var inputVector = GetWalkInput();
            float hSpeedIntended = inputVector.magnitude * PlayerConstants.HSPEED_MAX_GROUND;

            if (hSpeedIntended < PlayerConstants.HSPEED_MIN)
                hSpeedIntended = 0;

            float accel = HSpeed < hSpeedIntended
                ? PlayerConstants.HACCEL_GROUND
                : PlayerConstants.FRICTION_GROUND;

            HSpeed = Mathf.MoveTowards(HSpeed, hSpeedIntended, accel * Time.deltaTime);

            // HACK: Immediately accelerate to the minimum speed.
            // This makes the controls feel snappy and responsive, while still
            // having a feeling of acceleration.
            if (hSpeedIntended > 0 && HSpeed < PlayerConstants.HSPEED_MIN)
                HSpeed = PlayerConstants.HSPEED_MIN;

            // Rotate with the left stick
            if (!IsLeftStickNeutral())
            {
                // Gradually rotate until we're facing the direction the stick
                // is pointing
                float targetAngleDeg = GetHAngleDegInput();

                HAngleDeg = Mathf.MoveTowardsAngle(
                    HAngleDeg,
                    targetAngleDeg,
                    PlayerConstants.ROT_SPEED_DEG * Time.deltaTime
                );

                // ...unless we're going really slow, then just pivot instantly.
                if (HSpeed < PlayerConstants.MAX_PIVOT_SPEED)
                    HAngleDeg = targetAngleDeg;
            }
        }
        private void ButtonControls()
        {
            if (JumpPressedRecently())
            {
                if (_shared.StoppedRollingRecently())
                    _shared.StartRollJump();
                else
                    _shared.StartGroundJump();
            }

            if (AttackPressedRecently() && _shared._rollCooldown <= 0)
            {
                ChangeState(State.Rolling);
            }
        }
    }

    private class FreeFallState : AbstractPlayerState
    {
        public FreeFallState(PlayerMovement shared)
            : base(shared) {}

        public override void ResetState() {}

        public override void EarlyFixedUpdate()
        {
            // Transition to walking if we're on the ground
            if (_ground.IsGrounded)
            {
                ChangeState(State.Walking);

                // Reduce the HSpeed based on the stick magnitude.
                // This lets you avoid sliding(AKA: "sticking" the landing) by
                // moving the left stick to neutral.
                // This doesn't take away *all* of your momentum, because that would
                // look stiff and unnatural.  
                float hSpeedMult = _input.LeftStick.magnitude + PlayerConstants.MIN_LANDING_HSPEED_MULT;
                if (hSpeedMult > 1)
                    hSpeedMult = 1;
                HSpeed *= hSpeedMult;

                return;
            }

            // Transition to either ledge grabbing or wall sliding
            bool isWallSliding =
                VSpeed < 0 &&
                _wall.IsTouchingWall &&
                _shared.Forward.ComponentAlong(-_wall.LastWallNormal) > 0;

            bool inLedgeGrabSweetSpot = 
                _ledge.LedgePresent &&
                _ledge.LastLedgeHeight >= PlayerConstants.BODY_HEIGHT / 2 &&
                _ledge.LastLedgeHeight <= PlayerConstants.BODY_HEIGHT;

            if (isWallSliding && inLedgeGrabSweetSpot)
            {
                ChangeState(State.LedgeGrabbing);
                return;
            }

            if (isWallSliding && !inLedgeGrabSweetSpot)
            {
                ChangeState(State.WallSliding);
                return;
            }
        }
        public override void FixedUpdate()
        {
            // DEBUG: Record stats
            if (_shared.transform.position.y > _shared._debugJumpMaxY)
                _shared._debugJumpMaxY = _shared.transform.position.y;

            Physics();
            StrafingControls();
            ButtonControls();
        }
        
        protected void Physics()
        {
            // Apply gravity
            // Use more gravity when we're falling so the jump arc feels "squishier"
            float gravity = VSpeed > 0
                ? _shared._riseGravity
                : _shared._fallGravity;

            VSpeed -= gravity * Time.deltaTime;

            // Cap the VSpeed at the terminal velocity
            if (VSpeed < PlayerConstants.TERMINAL_VELOCITY_AIR)
                VSpeed = PlayerConstants.TERMINAL_VELOCITY_AIR;

            // Start going downwards if you bonk your head on the ceiling.
            // Don't bonk your head!
            if (VSpeed > 0 && _ground.IsBonkingHead)
                VSpeed = PlayerConstants.BONK_SPEED;
        }
        protected void ButtonControls()
        {
            if (!_input.JumpHeld)
                _shared._jumpReleased = true;

            // Cut the jump short if the button was released on the way u
            // Immediately setting the VSpeed to 0 looks jarring, so instead we'll
            // exponentially decay it every frame.
            // Once it's decayed below a certain threshold, we'll let gravity do the
            // rest of the work so it still looks natural.
            if (VSpeed > (_shared._jumpSpeed / 2) && _shared._jumpReleased)
                VSpeed *= PlayerConstants.SHORT_JUMP_DECAY_RATE;

            // Let the player jump for a short period after walking off a ledge,
            // because everyone is human.  
            // This is called "coyote time", named after the tragic life of the late
            // Wile E. Coyote.
            if (VSpeed < 0 && WasGroundedRecently() && JumpPressedRecently())
            {
                _shared.StartGroundJump();
                DebugDisplay.PrintLine("Coyote-time jump!");
            }

            // Dive when the attack button is pressed.
            if (AttackPressedRecently())
            {
                ChangeState(State.Diving);
                return;
            }
        }
        protected void StrafingControls()
        {
            // Allow the player to change their direction for free for a short time
            // after jumping.  After that time is up, air strafing controls kick in
            if (_shared._jumpRedirectTimer >= 0)
            {
                InstantlyFaceLeftStick();
                SyncWalkVelocityToHSpeed();

                _shared._jumpRedirectTimer -= Time.deltaTime;
                return;
            }

            // In the air, we let the player "nudge" their velocity by applying a
            // force in the direction the stick is being pushed.
            // Unlike on the ground, you *will* lose speed and slide around if you
            // try to change your direction.
            var inputVector = GetWalkInput();

            Vector3 forward = _shared.AngleForward(HAngleDeg);
            bool pushingBackwards = inputVector.ComponentAlong(forward) < -0.5f;
            bool pushingForwards = inputVector.ComponentAlong(forward) > 0.75f;
            bool movingForwards = _shared._walkVelocity.normalized.ComponentAlong(forward) > 0;

            float accel = PlayerConstants.HACCEL_AIR;
            float maxSpeed = PlayerConstants.HSPEED_MAX_AIR;

            // Reduce the speed limit when moving backwards.
            // If you're wanna go fast, you gotta go forward.
            if (!movingForwards)
                maxSpeed = PlayerConstants.HSPEED_MAX_GROUND;

            // Give them a little bit of help if they're pushing backwards
            // on the stick, so it's easier to "abort" a poorly-timed jum
            if (pushingBackwards)
                accel = PlayerConstants.HACCEL_AIR_BACKWARDS;

            // Apply a force to get our new velocity.
            var oldVelocity = _shared._walkVelocity;
            var newVelocity = _shared._walkVelocity + (inputVector * accel * Time.deltaTime);
            
            // Only let the player accellerate up to the normal ground speed.
            // We won't slow them down if they're already going faster than that,
            // though (eg: due to the speed boost from chained jumping)
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

            // We WILL, however, slow them down if they're going past the max air
            // speed.  That's a hard maximum.
            if (newSpeed > maxSpeed)
                _shared._walkVelocity = _shared._walkVelocity.normalized * maxSpeed;

            _shared._walkVelocity = newVelocity.normalized * newSpeed;

            // Keep HSpeed up-to-date, so it'll be correct when we land.
            HSpeed = _shared._walkVelocity.ComponentAlong(_shared.Forward);
        }

    }

    private class WallSlidingState : AbstractPlayerState
    {
        public WallSlidingState(PlayerMovement shared)
            : base(shared) {}

        public override void EarlyFixedUpdate()
        {
            bool keepWallSliding = 
                !_ground.IsGrounded &&
                _wall.IsTouchingWall &&
                _shared.Forward.ComponentAlong(-_wall.LastWallNormal) > 0 &&
                VSpeed < 0;

            if (keepWallSliding)
                ChangeState(State.WallSliding);
            else if (_ground.IsGrounded)
                ChangeState(State.Walking);
            else
                ChangeState(State.FreeFall);
        }

        public override void FixedUpdate()
        {
            Physics();
            Controls();
        }

        private void Physics()
        {
            // Apply gravity
            float gravity = _shared._wallSlideGravity;
            VSpeed -= gravity * Time.deltaTime;

            if (VSpeed < PlayerConstants.TERMINAL_VELOCITY_WALL_SLIDE)
                VSpeed = PlayerConstants.TERMINAL_VELOCITY_WALL_SLIDE;

            // Cancel all walking velocity pointing "inside" the wall.
            // We're intentionally letting _walkVelocity and HSpeed get out of sync
            // here, so that the original HSpeed will be resumed when we wall kick.
            _shared._walkVelocity = _shared._walkVelocity.ProjectOnPlane(_wall.LastWallNormal);

            // Apply horizontal friction, since sliding on a wall naturally slows
            // you down.
            float slidingHSpeed = _shared._walkVelocity.magnitude;
            slidingHSpeed -= PlayerConstants.FRICTION_WALL_SLIDE * Time.deltaTime;
            if (slidingHSpeed < 0)
                slidingHSpeed = 0;

            _shared._walkVelocity = slidingHSpeed * _shared._walkVelocity.normalized;
        }

        private void Controls()
        {
            // Wall kick when we press the jump button
            if (JumpPressedRecently())
                _shared.StartWallJump();
        }
    }

    private class WallJumpingState : FreeFallState
    {
        private Vector3 _lastWallJumpPos;

        public WallJumpingState(PlayerMovement shared)
            : base(shared) {}

        public override void ResetState()
        {
            _lastWallJumpPos = Vector3.zero;
            base.ResetState();
        }

        public override void OnStateEnter()
        {
            base.OnStateEnter();
            _lastWallJumpPos = _shared.transform.position;
        }

        public override void EarlyFixedUpdate()
        {
            // After we have moved a minimum distance away from the wall, switch to
            // FreeFalling so air strafing can be re-enabled.
            float distFromWall = Vector3.Distance(
                _lastWallJumpPos.Flattened(),
                _shared.transform.position.Flattened()
            );
            if (distFromWall >= PlayerConstants.WALL_JUMP_MIN_HDIST)
                ChangeState(State.FreeFall);

            // All of the usual free fall transitions apply too.
            base.EarlyFixedUpdate();
        }

        public override void FixedUpdate()
        {
            // DEBUG: Record stats
            if (_shared.transform.position.y > _shared._debugJumpMaxY)
                _shared._debugJumpMaxY = _shared.transform.position.y;

            base.Physics();
            base.ButtonControls();
            // NOTE: Air strafing is intentionally disabled in this state.
            // It gets re-enabled when the state changes back to FreeFalling, after
            // the player has moved a minimum distance away from the wall.
        }
    }

    private class DivingState : AbstractPlayerState
    {
        public DivingState(PlayerMovement shared)
            : base(shared) {}

        public override void OnStateEnter()
        {
            InstantlyFaceLeftStick();

            HSpeed = PlayerConstants.DIVE_HSPEED_INITIAL;
            VSpeed = _shared._diveJumpVspeed;

            _shared._chainedJumpCount = 0;
            _shared.StartedDiving?.Invoke();
        }

        public override void EarlyFixedUpdate()
        {
            // Roll when we hit the ground
            if (_ground.IsGrounded)
                ChangeState(State.Rolling);

            // Bonk if we hit a wall
            if (_shared.ShouldBonkAgainstWall())
                ChangeState(State.Bonking);
        }
        public override void FixedUpdate()
        {
            // Damage things
            _shared._diveHitbox.ApplyDamage();

            // Apply gravity
            // Use more gravity when we're falling so the jump arc feels "squishier"
            VSpeed -= PlayerConstants.DIVE_GRAVITY * Time.deltaTime;

            // TODO: This logic is copy/pasted from WhileAirborn().  Refactor.
            // Cap the VSpeed at the terminal velocity
            if (VSpeed < PlayerConstants.TERMINAL_VELOCITY_AIR)
                VSpeed = PlayerConstants.TERMINAL_VELOCITY_AIR;

            // Reduce HSpeed until it's at the minimum
            // If the player is pushing backwards on the left stick, reduce the speed
            // faster and let them slow down more
            float initSpeed = PlayerConstants.DIVE_HSPEED_INITIAL;
            float finalSpeed = PlayerConstants.DIVE_HSPEED_FINAL_MAX;
            float slowTime = PlayerConstants.DIVE_HSPEED_SLOW_TIME;
            
            float stickBackwardsComponent = -LeftStickForwardsComponent();
            if (stickBackwardsComponent > 0)
            {
                finalSpeed = Mathf.Lerp(
                    PlayerConstants.DIVE_HSPEED_FINAL_MAX,
                    PlayerConstants.DIVE_HSPEED_FINAL_MIN,
                    stickBackwardsComponent
                );
            }

            float friction = (initSpeed - finalSpeed) / slowTime;
            HSpeed -= friction * Time.deltaTime;
            if (HSpeed < finalSpeed)
                HSpeed = finalSpeed;

            SyncWalkVelocityToHSpeed();
        }
    }

    private class GrabbingLedgeState : AbstractPlayerState
    {
        private float _timer;

        public GrabbingLedgeState(PlayerMovement shared)
            : base(shared) {}

        public override void ResetState()
        {
            _timer = 0;
        }

        public override void OnStateEnter()
        {
            _timer = PlayerConstants.LEDGE_GRAB_DURATION;
            _shared.GrabbedLedge?.Invoke();
        }

        public override void EarlyFixedUpdate()
        {
            if (_timer <= 0)
                _shared.CurrentState = State.FreeFall;
        }
        
        public override void FixedUpdate()
        {
            _shared.VSpeed = PlayerConstants.LEDGE_GRAB_VSPEED;
            _shared.HSpeed = PlayerConstants.LEDGE_GRAB_HSPEED;
            _shared.SyncWalkVelocityToHSpeed();

            _timer -= Time.deltaTime;
        }
    }

    private class RollingState : AbstractPlayerState
    {
        private float _timer;

        public RollingState(PlayerMovement shared)
            : base(shared) {}

        public override void ResetState()
        {
            _timer = 0;
        }

        public override void OnStateEnter()
        {
            VSpeed = 0;
            HSpeed = PlayerConstants.ROLL_DISTANCE / PlayerConstants.ROLL_TIME;
            InstantlyFaceLeftStick();
            SyncWalkVelocityToHSpeed();

            _timer = PlayerConstants.ROLL_TIME;
        }

        public override void OnStateExit()
        {
            // Start the cooldown, so the player can't immediately
            // roll again.
            _shared._rollCooldown = PlayerConstants.ROLL_COOLDOWN;
        }

        public override void EarlyFixedUpdate()
        {
            // Stop rolling after the timer expires
            if (_timer <= 0)
            {
                _shared._lastRollStopTime = Time.time;

                // Slow back down, so the player doesn't have ridiculous speed when
                // the roll stops
                HSpeed = 0;
                _shared._walkVelocity = Vector3.zero;

                // Transition to the correct state, based on if we're in the air
                // or not.
                if (_ground.IsGrounded)
                    ChangeState(State.Walking);
                else
                    ChangeState(State.FreeFall);
            }

            // Start bonking if we're moving into a wall.
            if (_shared.ShouldBonkAgainstWall())
            {
                ChangeState(State.Bonking);
                return;
            }
        }
        public override void FixedUpdate()
        {
            // Damage things
            _shared._rollHitbox.ApplyDamage();

            // Let the player change their direction for a very short about of time
            // at the beginning of their roll
            bool withinRedirectWindow = _timer > PlayerConstants.ROLL_TIME - PlayerConstants.ROLL_REDIRECT_TIME;
            if (withinRedirectWindow && !IsLeftStickNeutral())
                HAngleDeg = GetHAngleDegInput();

            SyncWalkVelocityToHSpeed();

            // Let the player jump out of a roll.
            if (JumpPressedRecently())
            {
                _shared.StartRollJump();
                return;
            }

            _timer -= Time.deltaTime;
        }
    }

    private class BonkingState : AbstractPlayerState
    {
        private float _timer;
        private int _bounceCount;

        public BonkingState(PlayerMovement shared)
            : base(shared) {}

        public override void ResetState()
        {
            _timer = 0;
            _bounceCount = 0;
        }

        public override void OnStateEnter()
        {
            VSpeed = PlayerConstants.BONK_START_VSPEED;
            HSpeed = PlayerConstants.BONK_START_HSPEED;
            HAngleDeg = _shared.GetHAngleDegFromForward(-_wall.LastWallNormal);
            SyncWalkVelocityToHSpeed();

            _timer = PlayerConstants.BONK_DURATION;
            _bounceCount = 0;

            _shared.Bonked?.Invoke();
        }

        public override void EarlyFixedUpdate()
        {
            if (_timer <= 0)
            {
                if (!_ground.IsGrounded)
                    ChangeState(State.FreeFall);
                else
                    ChangeState(State.Walking);
            }
        }

        public override void FixedUpdate()
        {
            // Apply gravity
            VSpeed -= PlayerConstants.BONK_GRAVITY * Time.deltaTime;
            if (VSpeed < PlayerConstants.TERMINAL_VELOCITY_AIR)
                VSpeed = PlayerConstants.TERMINAL_VELOCITY_AIR;

            // Bounce against the floor
            if (_ground.IsGrounded && VSpeed < 0 && _bounceCount < PlayerConstants.BONK_MAX_BOUNCE_COUNT)
            {
                VSpeed *= -PlayerConstants.BONK_BOUNCE_MULTIPLIER;
                _bounceCount++;
            }

            // Apply friction
            float bonkFriction = Mathf.Abs(PlayerConstants.BONK_START_HSPEED / PlayerConstants.BONK_SLOW_TIME);
            HSpeed = Mathf.MoveTowards(HSpeed, 0, bonkFriction * Time.deltaTime);
            SyncWalkVelocityToHSpeed();

            // Tick the timer down.  It starts after we've bounced once.
            if (_bounceCount >= 1)
                _timer -= Time.deltaTime;
        }
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