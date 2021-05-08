using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerStates
{
    public class ChainedJumpingState : StandardJumpingState
    {
        public ChainedJumpingState(PlayerStateMachine player) : base(player) {}

        public override void OnStateEnter()
        {
            _player.Motor.RelativeVSpeed = PlayerConstants.CHAIN_JUMP_VSPEED;
            _player.InstantlyFaceLeftStick();

            // If we just recently landed, restore their stored hspeed
            if (_player.ChainedJumpLandedRecently())
                _player.HSpeed = _player.StoredAirHSpeed;
            
            // Since this is a chained jump, give them a speed boost
            _player.HSpeed *= PlayerConstants.CHAINED_JUMP_HSPEED_MULT;

            _player.SyncWalkVelocityToHSpeed();

            // Book keeping
            _player.RecordJumpStarted();

            // Trigger animation
            _player.Anim.Set(PlayerAnims.CHAINED_JUMP);
        }
    }
}