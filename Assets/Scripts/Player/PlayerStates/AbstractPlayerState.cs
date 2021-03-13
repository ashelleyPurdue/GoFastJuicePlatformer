using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerMovement
{
    private abstract class AbstractPlayerState
    {
        protected PlayerMovement _shared;
        
        protected PlayerGroundDetector _ground => _shared._ground;
        protected PlayerWallDetector _wall => _shared._wall;
        protected PlayerLedgeDetector _ledge => _shared._ledge;

        protected IPlayerInput _input => _shared._input;

        protected Transform transform => _shared.transform;

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
        protected Vector3 Forward => _shared.Forward;


        protected float _jumpSpeed
        {
            get => _shared._jumpSpeed;
            set => _shared._jumpSpeed = value;
        }
        protected float _secondJumpSpeed
        {
            get => _shared._secondJumpSpeed;
            set => _shared._secondJumpSpeed = value;
        }
        protected int _chainedJumpCount
        {
            get => _shared._chainedJumpCount;
            set => _shared._chainedJumpCount = value;
        }
        protected bool _jumpReleased
        {
            get => _shared._jumpReleased;
            set => _shared._jumpReleased = value;
        }
        protected float _jumpRedirectTimer
        {
            get => _shared._jumpRedirectTimer;
            set => _shared._jumpRedirectTimer = value;
        }


        protected float _debugJumpStartY
        {
            get => _shared._debugJumpStartY;
            set => _shared._debugJumpStartY = value;
        }
        protected float _debugJumpMaxY
        {
            get => _shared._debugJumpMaxY;
            set => _shared._debugJumpMaxY = value;
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

        protected void StartGroundJump()
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
            _shared.StartedJumping?.Invoke();
        }
        protected void StartWallJump()
        {
            // DEBUG: Record debug stats
            _debugJumpStartY = transform.position.y;
            _debugJumpMaxY = transform.position.y;

            VSpeed = _jumpSpeed;

            // Reflect off of the wall at the angle we approached it at
            var kickDir = Forward.ReflectOffOfSurface(_wall.LastWallNormal);
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
            _shared.StartedJumping?.Invoke();
        }

        protected void StartRollJump()
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
            ChangeState(State.Walking);
            _shared.StartedJumping?.Invoke();
        }
    

        protected void SyncWalkVelocityToHSpeed()
        {
            // We're about to multiply HSpeed by the "forward" direction to
            // get our walking velocity.
            var forward = AngleForward(HAngleDeg);
            
            // If we're standing on a sloped surface, then that "forward" value
            // needs to be parallel to the ground we're standing on.  Otherwise,
            // walking downhill at high speeds will look like "stair stepping".
            if (_ground.IsGrounded)
            {
                forward = forward
                    .ProjectOnPlane(_ground.LastGroundNormal)
                    .normalized;
            }

            _shared._walkVelocity = HSpeed * forward;
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


        protected Vector3 AngleForward(float angleDeg) => _shared.AngleForward(angleDeg);
        
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
            return (Time.time - PlayerConstants.EARLY_JUMP_TIME < _shared._lastJumpButtonPressTime);
        }

        protected bool AttackPressedRecently()
        {
            return (Time.time - Time.fixedDeltaTime < _shared._lastAttackButtonPressTime);
        }

        protected bool ChainedJumpLandedRecently()
            => _shared.ChainedJumpLandedRecently();

        protected bool ShouldBonkAgainstWall()
        {
            return 
                _wall.IsTouchingWall &&
                Forward.ComponentAlong(-_wall.LastWallNormal) > 0.5f;
        }
    }
}
