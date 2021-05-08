using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerStates
{
    public class StandardJumpingState : AbstractPlayerState
    {
        public StandardJumpingState(PlayerStateMachine player) : base(player) {}

        public override void OnStateEnter()
        {
            _player.DebugRecordJumpStart();

            _player.InstantlyFaceLeftStick();

            _player.Motor.RelativeVSpeed = PlayerConstants.STANDARD_JUMP_VSPEED;

            // If this was a chained jump, restore their stored hspeed
            if (_player.ChainedJumpLandedRecently())
                _player.HSpeed = _player.StoredAirHSpeed;

            // Jump heigher and get a speed boost every time they do 2 chained jumps
            bool isChainedJump = _player.ChainedJumpCount % 2 == 1;
            if (isChainedJump)
            {
                _player.Motor.RelativeVSpeed = PlayerConstants.CHAIN_JUMP_VSPEED;
                _player.HSpeed *= PlayerConstants.CHAINED_JUMP_HSPEED_MULT;
            }

            _player.SyncWalkVelocityToHSpeed();

            // Book keeping
            _player.ChainedJumpCount++;
            _player.JumpReleased = false;
            _player.LastJumpStartTime = Time.time;

            // Trigger animation
            string anim = isChainedJump
                ? PlayerAnims.CHAINED_JUMP
                : PlayerAnims.STANDARD_JUMP;
            _player.Anim.Set(anim);
        }

        public override void EarlyFixedUpdate()
        {
            // Transition to free falling if we're moving downwards
            if (_player.Motor.RelativeVSpeed <= 0)
            {
                _player.ChangeState(_player.FreeFall);
                
                // HACK: Do FreeFall's state changes in the same frame, to
                // preserve the old behavior from before the refactor.
                _player.FreeFall.EarlyFixedUpdate();
                return;
            }
        }

        public override void FixedUpdate()
        {
            _player.DebugRecordWhileJumping();

            Physics();
            _player.AirStrafingControls();
            ButtonControls();
        }

        protected void Physics()
        {
            // Apply gravity
            _player.Motor.RelativeVSpeed -= PlayerConstants.JUMP_RISE_GRAVITY * Time.deltaTime;

            // Start going downwards if you bonk your head on the ceiling.
            // Don't bonk your head!
            if (_player.Motor.IsBonkingHead)
            {
                _player.Motor.RelativeVSpeed = PlayerConstants.BONK_SPEED;
                _player.ChangeState(_player.FreeFall);
            }
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
            
            // Dive when the attack button is pressed.
            if (_player.AttackPressedRecently())
            {
                _player.ChangeState(_player.Diving);
                return;
            }
        }
    }
}