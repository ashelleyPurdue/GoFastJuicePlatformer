using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerMovement
{
    private class WallJumpingState : FreeFallState
    {
        private Vector3 _lastWallJumpPos;

        public WallJumpingState(PlayerMovement shared)
            : base(shared) {}

        public override void ResetState()
        {
            _lastWallJumpPos = Vector3.zero;
            base.ResetState();
        }

        public override void OnStateEnter()
        {
            base.OnStateEnter();
            _lastWallJumpPos = _shared.transform.position;
        }

        public override void EarlyFixedUpdate()
        {
            // After we have moved a minimum distance away from the wall, switch to
            // FreeFalling so air strafing can be re-enabled.
            float distFromWall = Vector3.Distance(
                _lastWallJumpPos.Flattened(),
                _shared.transform.position.Flattened()
            );
            if (distFromWall >= PlayerConstants.WALL_JUMP_MIN_HDIST)
                ChangeState(State.FreeFall);

            // All of the usual free fall transitions apply too.
            base.EarlyFixedUpdate();
        }

        public override void FixedUpdate()
        {
            // DEBUG: Record stats
            if (_shared.transform.position.y > _shared._debugJumpMaxY)
                _shared._debugJumpMaxY = _shared.transform.position.y;

            base.Physics();
            base.ButtonControls();
            // NOTE: Air strafing is intentionally disabled in this state.
            // It gets re-enabled when the state changes back to FreeFalling, after
            // the player has moved a minimum distance away from the wall.
        }
    }
}