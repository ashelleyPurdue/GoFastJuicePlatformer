using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerStates
{
    public class FreeFallState : AbstractPlayerState
    {
        public FreeFallState(PlayerStateMachine shared)
            : base(shared) {}

        public override void ResetState() {}

        public override void EarlyFixedUpdate()
        {
            // Transition to walking if we're on the ground and not moving upward
            if (_player.Motor.IsGrounded)
            {
                _player.ChangeState(_player.Walking);

                // Reduce the HSpeed based on the stick magnitude.
                // This lets you avoid sliding(AKA: "sticking" the landing) by
                // moving the left stick to neutral.
                // This doesn't take away *all* of your momentum, because that would
                // look stiff and unnatural.
                _player.StoredAirHSpeed = _player.HSpeed;
                float hSpeedMult = _player.Input.LeftStick.magnitude + PlayerConstants.MIN_LANDING_HSPEED_MULT;
                if (hSpeedMult > 1)
                    hSpeedMult = 1;
                _player.HSpeed *= hSpeedMult;

                return;
            }

            // Transition to either ledge grabbing or wall sliding
            bool isWallSliding =
                _player.Motor.RelativeVSpeed < 0 &&
                _player.Motor.IsTouchingWall &&
                _player.Forward.ComponentAlong(-_player.Motor.LastWallNormal) > 0;

            bool inLedgeGrabSweetSpot = 
                _player.Motor.LedgePresent &&
                _player.Motor.LastLedgeHeight >= PlayerConstants.BODY_HEIGHT / 2 &&
                _player.Motor.LastLedgeHeight <= PlayerConstants.BODY_HEIGHT;

            if (isWallSliding && inLedgeGrabSweetSpot)
            {
                _player.ChangeState(_player.GrabbingLedge);
                return;
            }

            if (isWallSliding && !inLedgeGrabSweetSpot)
            {
                _player.ChangeState(_player.WallSliding);
                return;
            }
        }
        public override void FixedUpdate()
        {
            _player.DebugRecordWhileJumping();

            Physics();
            _player.AirStrafingControls();
            ButtonControls();
            UpdateAnimation();
        }
        
        protected void Physics()
        {
            // Apply gravity
            _player.Motor.RelativeVSpeed -= PlayerConstants.FREE_FALL_GRAVITY * Time.deltaTime;

            // Cap the VSpeed at the terminal velocity
            if (_player.Motor.RelativeVSpeed < PlayerConstants.TERMINAL_VELOCITY_AIR)
                _player.Motor.RelativeVSpeed = PlayerConstants.TERMINAL_VELOCITY_AIR;
        }
        protected void ButtonControls()
        {
            // Let the player jump for a short period after walking off a ledge,
            // because everyone is human.  
            // This is called "coyote time", named after the tragic life of the 
            // late Wile E. Coyote.
            bool coyoteTime = WasGroundedRecently() && _player.Motor.RelativeVSpeed < 0;
            if (coyoteTime && _player.JumpPressedRecently())
            {
                _player.ChangeState(_player.StandardJumping);
                Debug.Log("Coyote-time jump!");
            }

            // Dive when the attack button is pressed.
            if (_player.AttackPressedRecently())
            {
                _player.ChangeState(_player.Diving);
                return;
            }
        }
    
        private bool WasGroundedRecently()
        {
            return (Time.time - PlayerConstants.COYOTE_TIME < _player.Motor.LastGroundedTime);
        }

        private void UpdateAnimation()
        {
            // Switch to the falling animation if we're falling
            if (_player.Motor.RelativeVSpeed < 0)
                _player.Anim.Set(PlayerAnims.FALL, 0.25f);
        }
    }
}