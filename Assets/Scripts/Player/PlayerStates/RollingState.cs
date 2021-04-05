using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerStateMachine
{
    private class RollingState : AbstractPlayerState
    {
        private float _timer;

        public RollingState(PlayerStateMachine shared, PlayerMotor motor)
            : base(shared, motor) {}

        public override PlayerAnimationHint GetAnimationHint() => PlayerAnimationHint.Rolling;

        public override void ResetState()
        {
            _timer = 0;
        }

        public override void OnStateEnter()
        {
            _motor.RelativeVSpeed = 0;
            HSpeed = PlayerConstants.ROLL_DISTANCE / PlayerConstants.ROLL_TIME;
            InstantlyFaceLeftStick();
            SyncWalkVelocityToHSpeed();

            _timer = PlayerConstants.ROLL_TIME;
        }

        public override void OnStateExit()
        {
            // Start the cooldown, so the player can't immediately
            // roll again.
            _sm._rollCooldown = PlayerConstants.ROLL_COOLDOWN;
        }

        public override void EarlyFixedUpdate()
        {
            // Stop rolling after the timer expires
            if (_timer <= 0)
            {
                _sm._lastRollStopTime = Time.time;

                // Slow back down, so the player doesn't have ridiculous speed when
                // the roll stops
                HSpeed = 0;
                _motor.RelativeFlatVelocity = Vector3.zero;

                // Transition to the correct state, based on if we're in the air
                // or not.
                if (_motor.IsGrounded)
                    ChangeState(_sm.Walking);
                else
                    ChangeState(_sm.FreeFall);
            }

            // Start bonking if we're moving into a wall.
            if (ShouldBonkAgainstWall())
            {
                ChangeState(_sm.Bonking);
                return;
            }
        }
        public override void FixedUpdate()
        {
            // Damage things
            _sm._rollHitbox.ApplyDamage();

            // Let the player turn a little bit
            if (!IsLeftStickNeutral())
            {
                HAngleDeg = Mathf.MoveTowardsAngle(
                    HAngleDeg,
                    GetHAngleDegInput(),
                    PlayerConstants.ROLL_ROT_SPEED_DEG * Time.deltaTime
                );
            }

            SyncWalkVelocityToHSpeed();

            // Let the player jump out of a roll.
            if (JumpPressedRecently())
            {
                StartRollJump();
                return;
            }

            _timer -= Time.deltaTime;
        }
    }

}