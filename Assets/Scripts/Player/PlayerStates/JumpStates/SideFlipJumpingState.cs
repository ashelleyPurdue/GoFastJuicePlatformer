using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerStates
{
    public class SideFlipJumpingState : StandardJumpingState
    {
        public SideFlipJumpingState(PlayerStateMachine player) : base(player) {}

        public override void OnStateEnter()
        {
            _player.DebugRecordJumpStart();

            // TODO: Use separate constants for this.
            _player.Motor.RelativeVSpeed = PlayerConstants.STANDARD_JUMP_VSPEED * 1.25f;
            _player.HSpeed = PlayerConstants.HSPEED_MAX_GROUND;
            _player.SyncWalkVelocityToHSpeed();

            // Book keeping
            // NOTE: A side flip never acts as a chained jump, but it still adds
            // to the chain jump count.
            _player.ChainedJumpCount++;
            _player.JumpReleased = false;
            _player.LastJumpStartTime = Time.time;
            
            // Trigger animation
            _player.Anim.Set(PlayerAnims.SIDE_FLIP);
        }
    }
}