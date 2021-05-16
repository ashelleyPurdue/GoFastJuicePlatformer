using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerStates
{
    public class RollJumpingState : StandardJumpingState
    {
        public RollJumpingState(PlayerStateMachine player) : base(player) {}

        public override void OnStateEnter()
        {
            _player.InstantlyFaceLeftStick();

            // Cap their HSpeed at something reasonable.
            // Otherwise, they'd conserve their rolling HSpeed into the
            // jump, which would result in a *super* ridiculous long jump.
            // We only want rolling jumps to be *slightly* ridiculous.
            _player.HSpeed = PlayerConstants.ROLL_JUMP_HSPEED;
            _player.Motor.RelativeVSpeed = PlayerConstants.STANDARD_JUMP_VSPEED;
            _player.SyncWalkVelocityToHSpeed();

            _player.RecordJumpStarted();
            
            // Trigger animation
            _player.Anim.Set(PlayerAnims.STANDARD_JUMP);
        }
    }
}