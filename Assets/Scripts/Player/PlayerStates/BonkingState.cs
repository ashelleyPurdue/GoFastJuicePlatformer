using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerMovement
{
    private class BonkingState : AbstractPlayerState
    {
        private float _timer;
        private int _bounceCount;

        public BonkingState(PlayerMovement shared)
            : base(shared) {}

        public override void ResetState()
        {
            _timer = 0;
            _bounceCount = 0;
        }

        public override void OnStateEnter()
        {
            VSpeed = PlayerConstants.BONK_START_VSPEED;
            HSpeed = PlayerConstants.BONK_START_HSPEED;
            HAngleDeg = GetHAngleDegFromForward(-_wall.LastWallNormal);
            SyncWalkVelocityToHSpeed();

            _timer = PlayerConstants.BONK_DURATION;
            _bounceCount = 0;

            _shared.Bonked?.Invoke();
        }

        public override void EarlyFixedUpdate()
        {
            if (_timer <= 0)
            {
                if (!_ground.IsGrounded)
                    ChangeState(State.FreeFall);
                else
                    ChangeState(State.Walking);
            }
        }

        public override void FixedUpdate()
        {
            // Apply gravity
            VSpeed -= PlayerConstants.BONK_GRAVITY * Time.deltaTime;
            if (VSpeed < PlayerConstants.TERMINAL_VELOCITY_AIR)
                VSpeed = PlayerConstants.TERMINAL_VELOCITY_AIR;

            // Bounce against the floor
            if (_ground.IsGrounded && VSpeed < 0 && _bounceCount < PlayerConstants.BONK_MAX_BOUNCE_COUNT)
            {
                VSpeed *= -PlayerConstants.BONK_BOUNCE_MULTIPLIER;
                _bounceCount++;
            }

            // Apply friction
            float bonkFriction = Mathf.Abs(PlayerConstants.BONK_START_HSPEED / PlayerConstants.BONK_SLOW_TIME);
            HSpeed = Mathf.MoveTowards(HSpeed, 0, bonkFriction * Time.deltaTime);
            SyncWalkVelocityToHSpeed();

            // Tick the timer down.  It starts after we've bounced once.
            if (_bounceCount >= 1)
                _timer -= Time.deltaTime;
        }
    }


}