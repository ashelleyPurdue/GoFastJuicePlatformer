using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerStates
{
    public class WallJumpingState : StandardJumpingState
    {
        private bool _enableAirStrafing = false;

        public WallJumpingState(PlayerStateMachine shared)
            : base(shared) {}

        public override void OnStateEnter()
        {
            _player.Motor.RelativeVSpeed = PlayerConstants.WALL_JUMP_VSPEED;

            // Kick off of the wall at a speed that's *at least* WALL_JUMP_MIN_HSPEED.
            // If we were already going faster than that before touching the wall,
            // then use *that* speed instead.  This way, you'll never lose speed by
            // wall jumping.
            _player.FaceAwayFromWall();
            _player.HSpeed = Mathf.Max(
                PlayerConstants.WALL_JUMP_MIN_HSPEED,
                _player.HSpeed
            );

            // On top of that, give the player a *boost* to their HSpeed, as a reward
            // for wall jumping.
            _player.HSpeed *= PlayerConstants.WALL_JUMP_HSPEED_MULT;

            _player.SyncWalkVelocityToHSpeed();

            // Book keeping
            _player.RecordJumpStarted();
            _player.ChainedJumpCount = 1; // HACK: The next normal jump after
                                          // landing will always be a "second"
                                          // chained jump

            _enableAirStrafing = false;   // We will re-enable air-strafing after
                                          // travelling a minimum distance away
                                          // from the wall
            
            _player.LastJumpStartTime = 0;  // HACK: Disable jump redirection
                                            // entirely.
            
            // Trigger animation
            _player.Anim.Set(PlayerAnims.STANDARD_JUMP);
        }

        public override void FixedUpdate()
        {
            _player.DebugRecordWhileJumping();

            if (ShouldEnableAirStrafing())
                _enableAirStrafing = true;

            base.Physics();

            if (_enableAirStrafing)
            {
                _player.AirStrafingControls();
            }
            base.ButtonControls();
        }

        private bool ShouldEnableAirStrafing()
        {
            float distFromWall = Vector3.Distance(
                _player.LastJumpStartPos.Flattened(),
                _player.Motor.Position.Flattened()
            );

            return distFromWall >= PlayerConstants.WALL_JUMP_MIN_HDIST;
        }
    }
}