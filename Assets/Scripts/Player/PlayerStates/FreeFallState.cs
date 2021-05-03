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
            if (_player.Motor.IsGrounded && _player.Motor.RelativeVSpeed <= 0)
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
            // DEBUG: Record stats
            if (_player.Motor.transform.position.y > _player.DebugJumpMaxYFooBar)
                _player.DebugJumpMaxYFooBar = _player.Motor.transform.position.y;

            Physics();
            StrafingControls();
            ButtonControls();
            UpdateAnimation();
        }
        
        protected void Physics()
        {
            // Apply gravity
            // Use more gravity when we're falling so the jump arc feels
            // "squishier"
            float gravity = _player.Motor.RelativeVSpeed > 0
                ? PlayerConstants.JUMP_RISE_GRAVITY
                : PlayerConstants.FREE_FALL_GRAVITY;

            _player.Motor.RelativeVSpeed -= gravity * Time.deltaTime;

            // Cap the VSpeed at the terminal velocity
            if (_player.Motor.RelativeVSpeed < PlayerConstants.TERMINAL_VELOCITY_AIR)
                _player.Motor.RelativeVSpeed = PlayerConstants.TERMINAL_VELOCITY_AIR;

            // Start going downwards if you bonk your head on the ceiling.
            // Don't bonk your head!
            if (_player.Motor.RelativeVSpeed > 0 && _player.Motor.IsBonkingHead)
                _player.Motor.RelativeVSpeed = PlayerConstants.BONK_SPEED;
        }
        protected void ButtonControls()
        {
            if (!_player.Input.JumpHeld)
                _player.JumpReleased = true;

            // Cut the jump short if the button was released on the way up
            // Immediately setting the VSpeed to 0 looks jarring, so instead
            // we'll exponentially decay it every frame.
            // Once it's decayed below a certain threshold, we'll let gravity do 
            // the rest of the work so it still looks natural.
            bool shouldDecay =
                _player.JumpReleased &&
                _player.Motor.RelativeVSpeed > (PlayerConstants.STANDARD_JUMP_VSPEED / 2);

            if (shouldDecay)
                _player.Motor.RelativeVSpeed *= PlayerConstants.SHORT_JUMP_DECAY_RATE;

            // Let the player jump for a short period after walking off a ledge,
            // because everyone is human.  
            // This is called "coyote time", named after the tragic life of the 
            // late Wile E. Coyote.
            bool coyoteTime = WasGroundedRecently() && _player.Motor.RelativeVSpeed < 0;
            if (coyoteTime && _player.JumpPressedRecently())
            {
                _player.StartGroundJump();
                Debug.Log("Coyote-time jump!");
            }

            // Dive when the attack button is pressed.
            if (_player.AttackPressedRecently())
            {
                _player.ChangeState(_player.Diving);
                return;
            }
        }
        protected void StrafingControls()
        {
            // Always be facing the left stick.
            // This gives the player the illusion of having more control,
            // without actually affecting their velocity.
            // It also makes it easier to tell which direction they would dive
            // in, if they were to press the dive button right now.
            _player.InstantlyFaceLeftStick();

            // Allow the player to redirect their velocity for free for a short
            // time after jumping, in case they pressed the jump button while
            // they were still moving the stick.
            // After that time is up, air strafing controls kick in.
            if (_player.IsInJumpRedirectTimeWindow())
            {
                _player.SyncWalkVelocityToHSpeed();
                return;
            }

            // In the air, we let the player "nudge" their velocity by applying
            // a force in the direction the stick is being pushed.
            // Unlike on the ground, you *will* lose speed and slide around if
            // you try to change your direction.
            var inputVector = _player.GetWalkInput();

            float accel = PlayerConstants.HACCEL_AIR;
            float maxSpeed = PlayerConstants.HSPEED_MAX_AIR;

            // Apply a force to get our new velocity.
            var oldVelocity = _player.Motor.RelativeFlatVelocity;
            var newVelocity = _player.Motor.RelativeFlatVelocity + (inputVector * accel * Time.deltaTime);
            
            // Only let the player accellerate up to the normal ground speed.
            // We won't slow them down if they're already going faster than
            // that, though (eg: due to a speed boost from wall jumping)
            float oldSpeed = oldVelocity.magnitude;
            float newSpeed = newVelocity.magnitude;

            bool wasAboveGroundSpeedLimit = oldSpeed > PlayerConstants.HSPEED_MAX_GROUND;
            bool nowAboveGroundSpeedLimit = newSpeed > PlayerConstants.HSPEED_MAX_GROUND;

            if (newSpeed > oldSpeed)
            {
                if (wasAboveGroundSpeedLimit)
                    newSpeed = oldSpeed;
                else if (nowAboveGroundSpeedLimit)
                    newSpeed = PlayerConstants.HSPEED_MAX_GROUND;
            }

            // We WILL, however, slow them down if they're going past the max
            // air speed.  That's a hard maximum.
            if (newSpeed > maxSpeed)
                newSpeed = maxSpeed;

            _player.Motor.RelativeFlatVelocity = newVelocity.normalized * newSpeed;

            // Keep HSpeed up-to-date, so it'll be correct when we land.
            _player.HSpeed = _player.Motor.RelativeFlatVelocity.ComponentAlong(_player.Forward);
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