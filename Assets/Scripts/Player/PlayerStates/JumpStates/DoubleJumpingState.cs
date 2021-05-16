using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerStates
{
    public class DoubleJumpingState : StandardJumpingState
    {
        protected override float MinDuration => PlayerConstants.DOUBLE_JUMP_MIN_DURATION;

        public DoubleJumpingState(PlayerStateMachine player) : base(player) {}

        public override void OnStateEnter()
        {
            _player.Motor.RelativeVSpeed = PlayerConstants.DOUBLE_JUMP_VSPEED;
            _player.InstantlyFaceLeftStick();

            // Since this is a double jump, give them a speed boost
            _player.HSpeed = _player.StoredAirHSpeed;
            _player.HSpeed *= PlayerConstants.DOUBLE_JUMP_HSPEED_MULT;

            _player.SyncWalkVelocityToHSpeed();

            // Book keeping
            _player.RecordJumpStarted();
            _player.DoubleJumpArmed = false;

            // Trigger animation
            _player.Anim.Set(PlayerAnims.DOUBLE_JUMP);
        }
    }
}