using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerMovement
{
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
                _shared._lastChainedJumpLandTime = Time.fixedTime;

            // Reset the chained jump count if you wait too long after landing
            if (!ChainedJumpLandedRecently())
            {
                _shared._chainedJumpCount = 0;
            }

            Physics();
            StickControls();
            ButtonControls();
            
            SyncWalkVelocityToHSpeed();
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
                if (StoppedRollingRecently())
                    StartRollJump();
                else
                    StartGroundJump();
            }

            if (AttackPressedRecently() && _shared._rollCooldown <= 0)
            {
                ChangeState(State.Rolling);
            }
        }

        private bool StoppedRollingRecently()
        {
            return (Time.time - PlayerConstants.COYOTE_TIME < _shared._lastRollStopTime);
        }
    }
}