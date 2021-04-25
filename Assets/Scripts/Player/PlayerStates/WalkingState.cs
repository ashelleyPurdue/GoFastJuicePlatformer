using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerStateMachine
{
    private class WalkingState : AbstractPlayerState
    {
        private float _lastSkidStartTime;

        public WalkingState(PlayerStateMachine shared, PlayerMotor motor)
            : base(shared, motor) {}

        public override PlayerAnimationHint GetAnimationHint()
        {
            if (IsSkidding())
                return PlayerAnimationHint.Skidding;
            return PlayerAnimationHint.Walking;
        }

        public override void ResetState()
        {
            _lastSkidStartTime = 0;
        }

        public override void EarlyFixedUpdate()
        {
            if (!_motor.IsGrounded)
                ChangeState(_sm.FreeFall);
        }

        public override void FixedUpdate()
        {
            // Start the chained jump timer once we land
            if (!_motor.WasGroundedLastFrame)
                _sm._lastChainedJumpLandTime = Time.fixedTime;

            // Reset the chained jump count if you wait too long after landing
            if (!ChainedJumpLandedRecently())
            {
                _sm._chainedJumpCount = 0;
            }

            Physics();
            StickControls();
            ButtonControls();
            
            SyncWalkVelocityToHSpeed();
        }

        private void Physics()
        {
            // Stop falling when we hit the ground.
            _motor.RelativeVSpeed = 0;

            // HACK: Snap to the ground if we're hovering over it a little bit.
            if (_motor.HeightAboveGround > 0)
                _motor.RelativeVSpeed = -_motor.HeightAboveGround / Time.deltaTime;
            
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
            UpdateHSpeed();
            UpdateHAngle();
            StartSkiddingIfDoing180();
        }
        
        private void UpdateHSpeed()
        {
            // Speed up/slow down with the left stick
            var inputVector = GetWalkInput();
            float hSpeedIntended = inputVector.magnitude * PlayerConstants.HSPEED_MAX_GROUND;

            if (hSpeedIntended < PlayerConstants.HSPEED_MIN)
                hSpeedIntended = 0;

            float accel = PlayerConstants.HACCEL_GROUND;
            HSpeed = Mathf.MoveTowards(HSpeed, hSpeedIntended, accel * Time.deltaTime);

            // HACK: Immediately accelerate to the minimum speed.
            // This makes the controls feel snappy and responsive, while still
            // having a feeling of acceleration.
            if (hSpeedIntended > 0 && HSpeed < PlayerConstants.HSPEED_MIN)
                HSpeed = PlayerConstants.HSPEED_MIN;
        }

        private void UpdateHAngle()
        {
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

        private void StartSkiddingIfDoing180()
        {
            float stickForwardComponent = GetWalkInput().ComponentAlong(Forward);
            if (stickForwardComponent < 0)
            {
                InstantlyFaceLeftStick();
                HSpeed = PlayerConstants.HSPEED_MIN;
                _lastSkidStartTime = Time.time;
            }
        }

        private void ButtonControls()
        {
            if (JumpPressedRecently())
            {
                if (StoppedRollingRecently())
                    StartRollJump();
                else if (IsSkidding())
                    StartSideFlipJump();
                else
                    StartGroundJump();
            }

            if (AttackPressedRecently() && _sm._rollCooldown <= 0)
            {
                ChangeState(_sm.Rolling);
            }
        }

        private bool StoppedRollingRecently()
        {
            return (Time.time - PlayerConstants.COYOTE_TIME < _sm._lastRollStopTime);
        }

        private bool IsSkidding()
        {
            return (Time.time - PlayerConstants.SKID_DURATION < _lastSkidStartTime);
        }
    }
}