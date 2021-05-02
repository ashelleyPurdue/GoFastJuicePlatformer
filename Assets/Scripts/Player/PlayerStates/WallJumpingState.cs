using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerStates
{
    public class WallJumpingState : FreeFallState
    {
        private Vector3 _lastWallJumpPos;

        public WallJumpingState(PlayerStateMachine shared)
            : base(shared) {}

        public override void ResetState()
        {
            _lastWallJumpPos = Vector3.zero;
            base.ResetState();
        }

        public override void OnStateEnter()
        {
            base.OnStateEnter();
            _lastWallJumpPos = _player.Motor.transform.position;
        }

        public override void EarlyFixedUpdate()
        {
            // After we have moved a minimum distance away from the wall, switch to
            // FreeFalling so air strafing can be re-enabled.
            float distFromWall = Vector3.Distance(
                _lastWallJumpPos.Flattened(),
                _player.Motor.transform.position.Flattened()
            );
            if (distFromWall >= PlayerConstants.WALL_JUMP_MIN_HDIST)
                _player.ChangeState(_player.FreeFall);

            // All of the usual free fall transitions apply too.
            base.EarlyFixedUpdate();
        }

        public override void FixedUpdate()
        {
            // DEBUG: Record stats
            if (_player.Motor.transform.position.y > _player.DebugJumpMaxYFooBar)
                _player.DebugJumpMaxYFooBar = _player.Motor.transform.position.y;

            base.Physics();
            base.ButtonControls();
            // NOTE: Air strafing is intentionally disabled in this state.
            // It gets re-enabled when the state changes back to FreeFalling, after
            // the player has moved a minimum distance away from the wall.
        }
    }
}