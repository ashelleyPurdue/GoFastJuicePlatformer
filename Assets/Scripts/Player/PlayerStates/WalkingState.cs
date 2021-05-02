using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerStates
{
    public class WalkingState : AbstractPlayerState
    {
        private float _lastSkidStartTime;

        public WalkingState(PlayerStateMachine shared)
            : base(shared) {}

        public override void ResetState()
        {
            _lastSkidStartTime = 0;
        }

        public override void OnStateExit()
        {
            _player.Anim.ForwardTiltAngleDeg = 0;
        }

        public override void EarlyFixedUpdate()
        {
            if (!_player.Motor.IsGrounded)
                _player.ChangeState(_player.FreeFall);
        }

        public override void FixedUpdate()
        {
            UpdateAnimation();

            // Start the chained jump timer once we land
            if (!_player.Motor.WasGroundedLastFrame)
                _player.LastChainedJumpLandTime = Time.fixedTime;

            // Reset the chained jump count if you wait too long after landing
            if (!_player.ChainedJumpLandedRecently())
            {
                _player.ChainedJumpCount = 0;
            }

            Physics();
            StickControls();
            ButtonControls();
            
            _player.SyncWalkVelocityToHSpeed();
        }

        private void UpdateAnimation()
        {
            // Tilt depending on how fast we're moving
            float speedPercent = _player.HSpeed / PlayerConstants.HSPEED_MAX_GROUND;
            _player.Anim.ForwardTiltAngleDeg = Mathf.Pow(speedPercent, 3) * 20;

            // Choose the right animation
            if (IsSkidding())
            {
                _player.Anim.Set(
                    PlayerAnims.SKID,
                    transitionDuration: 0.1f,
                    speed: 1f / PlayerConstants.SKID_DURATION
                );
            }
            else if (Mathf.Abs(_player.HSpeed) > 0)
            {
                _player.Anim.Set(
                    PlayerAnims.RUN,
                    transitionDuration: 0.1f,
                    speed: (_player.HSpeed / PlayerConstants.HSPEED_MAX_GROUND)
                );
            }
            else
            {
                _player.Anim.Set(
                    PlayerAnims.IDLE,
                    transitionDuration: 0.1f
                );
            }
        }

        private void Physics()
        {
            // Stop falling when we hit the ground.
            _player.Motor.RelativeVSpeed = 0;

            // HACK: Snap to the ground if we're hovering over it a little bit.
            if (_player.Motor.HeightAboveGround > 0)
                _player.Motor.RelativeVSpeed = -_player.Motor.HeightAboveGround / Time.deltaTime;
            
            // If we obtained negative hspeed while in the air(EG: from air braking),
            // bring it back to zero so the player doesn't go flying backwards.
            if (_player.HSpeed < 0)
                _player.HSpeed = 0;
        }

        private void StickControls()
        {
            // On the ground, we let the player turn without sliding around or losing
            // speed.
            // We do this by keeping track of their speed and angle separately.
            // The target speed is controlled by the magnitude of the left stick.
            // The target angle is controlled by the direction of the left stick.
            UpdateHSpeed();
            UpdateHAngle();
            StartSkiddingIfDoing180();
        }
        
        private void UpdateHSpeed()
        {
            // Speed up/slow down with the left stick
            var inputVector = _player.GetWalkInput();
            float hSpeedIntended = inputVector.magnitude * PlayerConstants.HSPEED_MAX_GROUND;

            if (hSpeedIntended < PlayerConstants.HSPEED_MIN)
                hSpeedIntended = 0;

            float accel = PlayerConstants.HACCEL_GROUND;
            _player.HSpeed = Mathf.MoveTowards(_player.HSpeed, hSpeedIntended, accel * Time.deltaTime);

            // HACK: Immediately accelerate to the minimum speed.
            // This makes the controls feel snappy and responsive, while still
            // having a feeling of acceleration.
            if (hSpeedIntended > 0 && _player.HSpeed < PlayerConstants.HSPEED_MIN)
                _player.HSpeed = PlayerConstants.HSPEED_MIN;
        }

        private void UpdateHAngle()
        {
            // Rotate with the left stick
            if (!_player.IsLeftStickNeutral())
            {
                // Gradually rotate until we're facing the direction the stick
                // is pointing
                float targetAngleDeg = _player.GetHAngleDegInput();

                _player.HAngleDeg = Mathf.MoveTowardsAngle(
                    _player.HAngleDeg,
                    targetAngleDeg,
                    PlayerConstants.ROT_SPEED_DEG * Time.deltaTime
                );

                // ...unless we're going really slow, then just pivot instantly.
                if (_player.HSpeed < PlayerConstants.MAX_PIVOT_SPEED)
                    _player.HAngleDeg = targetAngleDeg;
            }
        }

        private void StartSkiddingIfDoing180()
        {
            float stickForwardComponent = _player.GetWalkInput().ComponentAlong(_player.Forward);
            if (stickForwardComponent < 0)
            {
                _player.InstantlyFaceLeftStick();
                _player.HSpeed = PlayerConstants.HSPEED_MIN;
                _lastSkidStartTime = Time.time;
            }
        }

        private void ButtonControls()
        {
            if (_player.JumpPressedRecently())
            {
                if (StoppedRollingRecently())
                    _player.StartRollJump();
                else if (IsSkidding())
                    _player.StartSideFlipJump();
                else
                    _player.StartGroundJump();
            }

            if (_player.AttackPressedRecently() && _player.RollCooldown <= 0)
            {
                _player.ChangeState(_player.Rolling);
            }
        }

        private bool StoppedRollingRecently()
        {
            return (Time.time - PlayerConstants.COYOTE_TIME < _player.LastRollStopTime);
        }

        private bool IsSkidding()
        {
            return (Time.time - PlayerConstants.SKID_DURATION < _lastSkidStartTime);
        }
    }
}