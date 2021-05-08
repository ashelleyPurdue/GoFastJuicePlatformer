using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerStates
{
    public class RollingState : AbstractPlayerState
    {
        private float _lastRollStartTime;

        public RollingState(PlayerStateMachine shared)
            : base(shared) {}

        public override void OnStateEnter()
        {
            _player.Anim.Set(PlayerAnims.ROLL);

            _player.Motor.RelativeVSpeed = 0;
            _player.HSpeed = PlayerConstants.ROLL_DISTANCE / PlayerConstants.ROLL_TIME;
            _player.InstantlyFaceLeftStick();
            _player.SyncWalkVelocityToHSpeed();
            
            _lastRollStartTime = Time.time;
        }

        public override void OnStateExit()
        {
            // Start the roll cooldown
            _player.LastRollStopTime = Time.time;
        }

        public override void EarlyFixedUpdate()
        {
            // Stop rolling after the timer expires
            if (IsRollTimerExpired())
            {
                // Slow back down, so the player doesn't have ridiculous speed when
                // the roll stops
                _player.HSpeed = 0;
                _player.Motor.RelativeFlatVelocity = Vector3.zero;

                // Transition to the correct state, based on if we're in the air
                // or not.
                if (_player.Motor.IsGrounded)
                    _player.ChangeState(_player.Walking);
                else
                    _player.ChangeState(_player.FreeFall);
            }

            // Start bonking if we're moving into a wall.
            if (_player.ShouldBonkAgainstWall())
            {
                _player.ChangeState(_player.Bonking);
                return;
            }
        }
        public override void FixedUpdate()
        {
            // Damage things
            _player.RollHitbox.ApplyDamage();

            // Let the player turn a little bit
            if (!_player.IsLeftStickNeutral())
            {
                _player.HAngleDeg = Mathf.MoveTowardsAngle(
                    _player.HAngleDeg,
                    _player.GetHAngleDegInput(),
                    PlayerConstants.ROLL_ROT_SPEED_DEG * Time.deltaTime
                );
            }

            _player.SyncWalkVelocityToHSpeed();

            // Let the player jump out of a roll.
            if (_player.JumpPressedRecently())
            {
                _player.ChangeState(_player.RollJumping);
                return;
            }
        }
    
        private bool IsRollTimerExpired()
        {
            float rollEndTime = _lastRollStartTime + PlayerConstants.ROLL_TIME;
            return (Time.time > rollEndTime);
        }
    }

}