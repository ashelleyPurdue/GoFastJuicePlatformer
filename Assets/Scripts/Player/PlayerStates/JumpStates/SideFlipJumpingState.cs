using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerStates
{
    public class SideFlipJumpingState : StandardJumpingState
    {
        protected override float MinDuration => PlayerConstants.SIDE_FLIP_MIN_DURATION;

        public SideFlipJumpingState(PlayerStateMachine player) : base(player) {}

        public override void OnStateEnter()
        {
            
            // TODO: Use separate constants for this.
            _player.Motor.RelativeVSpeed = PlayerConstants.SIDE_FLIP_VSPEED;
            _player.HSpeed = PlayerConstants.HSPEED_MAX_GROUND;
            _player.SyncWalkVelocityToHSpeed();

            // Book keeping
            // NOTE: A side flip never acts as a chained jump, but it still adds
            // to the chain jump count.
            _player.RecordJumpStarted();
            
            // Trigger animation
            _player.Anim.Set(PlayerAnims.SIDE_FLIP);
        }
    }
}