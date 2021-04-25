using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerStateMachine
{
    private class FreeFallState : AbstractPlayerState
    {
        public FreeFallState(PlayerStateMachine shared, PlayerMotor motor)
            : base(shared, motor) {}

        public override void ResetState() {}

        public override void EarlyFixedUpdate()
        {
            // Transition to walking if we're on the ground and not moving upward
            if (_motor.IsGrounded && _motor.RelativeVSpeed <= 0)
            {
                ChangeState(_sm.Walking);

                // Reduce the HSpeed based on the stick magnitude.
                // This lets you avoid sliding(AKA: "sticking" the landing) by
                // moving the left stick to neutral.
                // This doesn't take away *all* of your momentum, because that would
                // look stiff and unnatural.
                _sm._storedAirHSpeed = HSpeed;
                float hSpeedMult = _input.LeftStick.magnitude + PlayerConstants.MIN_LANDING_HSPEED_MULT;
                if (hSpeedMult > 1)
                    hSpeedMult = 1;
                HSpeed *= hSpeedMult;

                return;
            }

            // Transition to either ledge grabbing or wall sliding
            bool isWallSliding =
                _motor.RelativeVSpeed < 0 &&
                _motor.IsTouchingWall &&
                Forward.ComponentAlong(-_motor.LastWallNormal) > 0;

            bool inLedgeGrabSweetSpot = 
                _motor.LedgePresent &&
                _motor.LastLedgeHeight >= PlayerConstants.BODY_HEIGHT / 2 &&
                _motor.LastLedgeHeight <= PlayerConstants.BODY_HEIGHT;

            if (isWallSliding && inLedgeGrabSweetSpot)
            {
                ChangeState(_sm.GrabbingLedge);
                return;
            }

            if (isWallSliding && !inLedgeGrabSweetSpot)
            {
                ChangeState(_sm.WallSliding);
                return;
            }
        }
        public override void FixedUpdate()
        {
            // DEBUG: Record stats
            if (_motor.transform.position.y > _debugJumpMaxY)
                _debugJumpMaxY = _motor.transform.position.y;

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
            float gravity = _motor.RelativeVSpeed > 0
                ? PlayerConstants.JUMP_RISE_GRAVITY
                : PlayerConstants.FREE_FALL_GRAVITY;

            _motor.RelativeVSpeed -= gravity * Time.deltaTime;

            // Cap the VSpeed at the terminal velocity
            if (_motor.RelativeVSpeed < PlayerConstants.TERMINAL_VELOCITY_AIR)
                _motor.RelativeVSpeed = PlayerConstants.TERMINAL_VELOCITY_AIR;

            // Start going downwards if you bonk your head on the ceiling.
            // Don't bonk your head!
            if (_motor.RelativeVSpeed > 0 && _motor.IsBonkingHead)
                _motor.RelativeVSpeed = PlayerConstants.BONK_SPEED;
        }
        protected void ButtonControls()
        {
            if (!_input.JumpHeld)
                _sm._jumpReleased = true;

            // Cut the jump short if the button was released on the way up
            // Immediately setting the VSpeed to 0 looks jarring, so instead
            // we'll exponentially decay it every frame.
            // Once it's decayed below a certain threshold, we'll let gravity do 
            // the rest of the work so it still looks natural.
            bool shouldDecay =
                _sm._jumpReleased &&
                _motor.RelativeVSpeed > (PlayerConstants.STANDARD_JUMP_VSPEED / 2);

            if (shouldDecay)
                _motor.RelativeVSpeed *= PlayerConstants.SHORT_JUMP_DECAY_RATE;

            // Let the player jump for a short period after walking off a ledge,
            // because everyone is human.  
            // This is called "coyote time", named after the tragic life of the 
            // late Wile E. Coyote.
            bool coyoteTime = WasGroundedRecently() && _motor.RelativeVSpeed < 0;
            if (coyoteTime && JumpPressedRecently())
            {
                StartGroundJump();
                Debug.Log("Coyote-time jump!");
            }

            // Dive when the attack button is pressed.
            if (AttackPressedRecently())
            {
                ChangeState(_sm.Diving);
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
            InstantlyFaceLeftStick();

            // Allow the player to redirect their velocity for free for a short
            // time after jumping, in case they pressed the jump button while
            // they were still moving the stick.
            // After that time is up, air strafing controls kick in.
            if (_sm._jumpRedirectTimer >= 0)
            {
                SyncWalkVelocityToHSpeed();
                _sm._jumpRedirectTimer -= Time.deltaTime;
                return;
            }

            // In the air, we let the player "nudge" their velocity by applying
            // a force in the direction the stick is being pushed.
            // Unlike on the ground, you *will* lose speed and slide around if
            // you try to change your direction.
            var inputVector = GetWalkInput();

            float accel = PlayerConstants.HACCEL_AIR;
            float maxSpeed = PlayerConstants.HSPEED_MAX_AIR;

            // Apply a force to get our new velocity.
            var oldVelocity = _motor.RelativeFlatVelocity;
            var newVelocity = _motor.RelativeFlatVelocity + (inputVector * accel * Time.deltaTime);
            
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

            _motor.RelativeFlatVelocity = newVelocity.normalized * newSpeed;

            // Keep HSpeed up-to-date, so it'll be correct when we land.
            HSpeed = _motor.RelativeFlatVelocity.ComponentAlong(Forward);
        }
    
        private bool WasGroundedRecently()
        {
            return (Time.time - PlayerConstants.COYOTE_TIME < _motor.LastGroundedTime);
        }

        private void UpdateAnimation()
        {
            // Switch to the falling animation if we're falling
            if (_sm._motor.RelativeVSpeed < 0)
                _sm._anim.Set(PlayerAnims.FALL, 0.25f);
        }
    }
}