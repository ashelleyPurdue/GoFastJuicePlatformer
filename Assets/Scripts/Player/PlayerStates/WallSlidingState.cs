using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerStates
{
    public class WallSlidingState : AbstractPlayerState
    {
        public WallSlidingState(PlayerStateMachine shared)
            : base(shared) {}

        public override void OnStateEnter()
        {
            _player.Anim.Set(PlayerAnims.WALL_SLIDE, 0.1f);
        }

        public override void EarlyFixedUpdate()
        {
            bool keepWallSliding = 
                !_player.Motor.IsGrounded &&
                _player.Motor.IsTouchingWall &&
                -_player.Forward.ComponentAlong(-_player.Motor.LastWallNormal) > 0 &&
                _player.Motor.RelativeVSpeed < 0;

            if (keepWallSliding)
                _player.ChangeState(_player.WallSliding);
            else if (_player.Motor.IsGrounded)
                _player.ChangeState(_player.Walking);
            else
                _player.ChangeState(_player.FreeFall);
        }

        public override void FixedUpdate()
        {
            _player.FaceAwayFromWall();
            Physics();
            Controls();
        }

        private void Physics()
        {
            // Apply gravity
            float gravity = PlayerConstants.WALL_SLIDE_GRAVITY;
            _player.Motor.RelativeVSpeed -= gravity * Time.deltaTime;

            if (_player.Motor.RelativeVSpeed < PlayerConstants.TERMINAL_VELOCITY_WALL_SLIDE)
                _player.Motor.RelativeVSpeed = PlayerConstants.TERMINAL_VELOCITY_WALL_SLIDE;

            // Cancel all walking velocity pointing "inside" the wall.
            // We're intentionally letting _walkVelocity and HSpeed get out of sync
            // here, so that the original HSpeed will be resumed when we wall kick.
            _player.Motor.RelativeFlatVelocity = _player.Motor.RelativeFlatVelocity.ProjectOnPlane(_player.Motor.LastWallNormal);

            // Apply horizontal friction, since sliding on a wall naturally slows
            // you down.
            float slidingHSpeed = _player.Motor.RelativeFlatVelocity.magnitude;
            slidingHSpeed -= PlayerConstants.FRICTION_WALL_SLIDE * Time.deltaTime;
            if (slidingHSpeed < 0)
                slidingHSpeed = 0;

            _player.Motor.RelativeFlatVelocity = slidingHSpeed * _player.Motor.RelativeFlatVelocity.normalized;
        }

        private void Controls()
        {
            // Wall kick when we press the jump button
            if (_player.JumpPressedRecently())
                _player.StartWallJump();
        }
    }
}