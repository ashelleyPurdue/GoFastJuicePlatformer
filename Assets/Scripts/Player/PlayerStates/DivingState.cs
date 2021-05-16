using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerStates
{
    public class DivingState : AbstractPlayerState
    {
        public DivingState(PlayerStateMachine shared)
            : base(shared) {}

        public override void OnStateEnter()
        {
            _player.Anim.Set(PlayerAnims.DIVE, 0.1f);

            _player.InstantlyFaceLeftStick();

            _player.HSpeed = PlayerConstants.DIVE_HSPEED_INITIAL;
            _player.Motor.RelativeVSpeed = PlayerConstants.DIVE_JUMP_VSPEED;

            _player.DoubleJumpArmed = false;
        }

        public override void OnStateExit()
        {
            _player.Anim.ForwardTiltAngleDeg = 0;
        }

        public override void EarlyFixedUpdate()
        {
            // Roll when we hit the ground
            if (_player.Motor.IsGrounded)
                _player.ChangeState(_player.Rolling);

            // Bonk if we hit a wall
            if (_player.ShouldBonkAgainstWall())
                _player.ChangeState(_player.Bonking);
        }

        public override void FixedUpdate()
        {
            // Point the model in the direction we're moving
            _player.Anim.ForwardTiltAngleDeg =
                Quaternion.LookRotation(_player.Motor.TotalVelocity.normalized)
                .eulerAngles
                .x;

            // Damage things
            _player.DiveHitbox.ApplyDamage();

            // Apply gravity
            // Use more gravity when we're falling so the jump arc feels "squishier"
            _player.Motor.RelativeVSpeed -= PlayerConstants.DIVE_GRAVITY * Time.deltaTime;

            // TODO: This logic is copy/pasted from WhileAirborn().  Refactor.
            // Cap the VSpeed at the terminal velocity
            if (_player.Motor.RelativeVSpeed < PlayerConstants.TERMINAL_VELOCITY_AIR)
                _player.Motor.RelativeVSpeed = PlayerConstants.TERMINAL_VELOCITY_AIR;

            // Reduce HSpeed until it's at the minimum
            // If the player is pushing backwards on the left stick, reduce the speed
            // faster and let them slow down more
            float initSpeed = PlayerConstants.DIVE_HSPEED_INITIAL;
            float finalSpeed = PlayerConstants.DIVE_HSPEED_FINAL_MAX;
            float slowTime = PlayerConstants.DIVE_HSPEED_SLOW_TIME;
            
            float stickBackwardsComponent = -_player.LeftStickForwardsComponent();
            if (stickBackwardsComponent > 0)
            {
                finalSpeed = Mathf.Lerp(
                    PlayerConstants.DIVE_HSPEED_FINAL_MAX,
                    PlayerConstants.DIVE_HSPEED_FINAL_MIN,
                    stickBackwardsComponent
                );
            }

            float friction = (initSpeed - finalSpeed) / slowTime;
            _player.HSpeed -= friction * Time.deltaTime;
            if (_player.HSpeed < finalSpeed)
                _player.HSpeed = finalSpeed;

            _player.SyncWalkVelocityToHSpeed();
        }
    }

}