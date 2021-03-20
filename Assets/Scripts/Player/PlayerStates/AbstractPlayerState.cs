using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerStateMachine
{
    public abstract class AbstractPlayerState
    {
        protected PlayerStateMachine _sm;
        protected PlayerMotor _motor;

        protected IPlayerInput _input => _sm._input;

        protected float HAngleDeg
        {
            get => _sm.HAngleDeg;
            set => _sm.HAngleDeg = value;
        }
        protected float HSpeed
        {
            get => _sm.HSpeed;
            set => _sm.HSpeed = value;
        }

        protected Vector3 Forward => _sm.Forward;


        protected float _jumpSpeed
        {
            get => _sm._jumpSpeed;
            set => _sm._jumpSpeed = value;
        }
        protected float _secondJumpSpeed
        {
            get => _sm._secondJumpSpeed;
            set => _sm._secondJumpSpeed = value;
        }
        protected int _chainedJumpCount
        {
            get => _sm._chainedJumpCount;
            set => _sm._chainedJumpCount = value;
        }
        protected bool _jumpReleased
        {
            get => _sm._jumpReleased;
            set => _sm._jumpReleased = value;
        }
        protected float _jumpRedirectTimer
        {
            get => _sm._jumpRedirectTimer;
            set => _sm._jumpRedirectTimer = value;
        }


        protected float _debugJumpStartY
        {
            get => _sm._debugJumpStartY;
            set => _sm._debugJumpStartY = value;
        }
        protected float _debugJumpMaxY
        {
            get => _sm._debugJumpMaxY;
            set => _sm._debugJumpMaxY = value;
        }
        
        public AbstractPlayerState(PlayerStateMachine shared, PlayerMotor motor)
        {
            _sm = shared;
            _motor = motor;
        }

        public abstract State GetEnumVal();

        public virtual void ResetState() {}

        public virtual void OnStateEnter() {}
        public virtual void OnStateExit() {}

        public virtual void EarlyFixedUpdate() {}
        public virtual void FixedUpdate() {}

        protected void ChangeState(AbstractPlayerState newState)
        {
            _sm.ChangeState(newState);
        }

        protected void StartGroundJump()
        {
            // DEBUG: Record debug stats
            _debugJumpStartY = _motor.transform.position.y;
            _debugJumpMaxY = _motor.transform.position.y;

            InstantlyFaceLeftStick();

            _motor.RelativeVSpeed = _jumpSpeed;

            // If this was a chained jump, restore their stored hspeed
            if (ChainedJumpLandedRecently())
                HSpeed = _sm._storedAirHSpeed;

            // Jump heigher and get a speed boost every time they do 2 chained jumps
            if (_chainedJumpCount % 2 == 1)
            {
                _motor.RelativeVSpeed = _secondJumpSpeed;
                HSpeed *= PlayerConstants.CHAINED_JUMP_HSPEED_MULT;
            }

            SyncWalkVelocityToHSpeed();

            // Book keeping
            _chainedJumpCount++;
            _jumpReleased = false;
            _jumpRedirectTimer = PlayerConstants.JUMP_REDIRECT_TIME;
            _sm.StartedJumping?.Invoke();
        }
        protected void StartWallJump()
        {
            // DEBUG: Record debug stats
            _debugJumpStartY = _motor.transform.position.y;
            _debugJumpMaxY = _motor.transform.position.y;

            _motor.RelativeVSpeed = _jumpSpeed;

            // Reflect off of the wall at the angle we approached it at
            var kickDir = Forward.ReflectOffOfSurface(_motor.LastWallNormal);
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
            ChangeState(_sm.WallJumping);
            _sm.StartedJumping?.Invoke();
        }

        protected void StartRollJump()
        {
            // DEBUG: Record debug stats
            _debugJumpStartY = _motor.transform.position.y;
            _debugJumpMaxY = _motor.transform.position.y;

            InstantlyFaceLeftStick();

            // Cap their HSpeed at something reasonable.
            // Otherwise, they'd conserve their rolling HSpeed into the
            // jump, which would result in a *super* ridiculous long jump.
            // We only want rolling jumps to be *slightly* ridiculous.
            HSpeed = PlayerConstants.ROLL_JUMP_HSPEED;
            _motor.RelativeVSpeed = _jumpSpeed;
            SyncWalkVelocityToHSpeed();

            _chainedJumpCount = 0;
            _jumpReleased = false;
            _jumpRedirectTimer = PlayerConstants.JUMP_REDIRECT_TIME;
            ChangeState(_sm.Walking);
            _sm.StartedJumping?.Invoke();
        }
    

        protected void SyncWalkVelocityToHSpeed()
        {
            // We're about to multiply HSpeed by the "forward" direction to
            // get our walking velocity.
            var forward = AngleForward(HAngleDeg);
            
            // If we're standing on a sloped surface, then that "forward" value
            // needs to be parallel to the ground we're standing on.  Otherwise,
            // walking downhill at high speeds will look like "stair stepping".
            if (_motor.IsGrounded)
            {
                forward = forward
                    .ProjectOnPlane(_motor.LastGroundNormal)
                    .normalized;
            }

            _motor.RelativeFlatVelocity = HSpeed * forward;
        }
        
        protected void InstantlyFaceLeftStick()
        {
            if (!IsLeftStickNeutral())
                HAngleDeg = GetHAngleDegInput();
        }

        /// <summary>
        /// Returns a vector representing the left control stick, relative to camera
        /// space.
        /// </summary>
        /// <returns></returns>
        protected Vector3 GetWalkInput()
        {
            return InputUtils.LeftStickToWorldSpace(_input.LeftStick);
        }
        
        /// <summary>
        /// Returns the intended HAngleDeg based on the left stick's input, relative
        /// to camera space.
        /// </summary>
        /// <returns></returns>
        protected float GetHAngleDegInput()
        {
            var inputVector = GetWalkInput();
            return Mathf.Atan2(inputVector.z, inputVector.x) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Is the left stick in a neutral position(IE: in the deadzone?)
        /// </summary>
        /// <returns></returns>
        protected bool IsLeftStickNeutral()
        {
            return _input.LeftStick.magnitude < PlayerConstants.LEFT_STICK_DEADZONE;
        }
        protected float LeftStickForwardsComponent()
        {
            var inputVector = GetWalkInput();
            var forward = AngleForward(HAngleDeg);
            return inputVector.ComponentAlong(forward);
        }


        protected Vector3 AngleForward(float angleDeg) => _sm.AngleForward(angleDeg);
        
        /// <summary>
        /// Returns the HAngleDeg that would result in the given forward.
        /// </summary>
        /// <param name="forward"></param>
        /// <returns></returns>
        protected float GetHAngleDegFromForward(Vector3 forward)
        {
            var flatForward = forward.Flattened();
            float radians = Mathf.Atan2(flatForward.z, flatForward.x);
            return radians * Mathf.Rad2Deg;
        }

        protected bool JumpPressedRecently()
        {
            return (Time.time - PlayerConstants.EARLY_JUMP_TIME < _sm._lastJumpButtonPressTime);
        }

        protected bool AttackPressedRecently()
        {
            return (Time.time - Time.fixedDeltaTime < _sm._lastAttackButtonPressTime);
        }

        protected bool ChainedJumpLandedRecently()
            => _sm.ChainedJumpLandedRecently();

        protected bool ShouldBonkAgainstWall()
        {
            return 
                _motor.IsTouchingWall &&
                Forward.ComponentAlong(-_motor.LastWallNormal) > 0.5f;
        }
    }
}
