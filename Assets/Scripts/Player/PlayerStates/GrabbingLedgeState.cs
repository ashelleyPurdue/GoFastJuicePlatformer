using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerStates
{
    public class GrabbingLedgeState : AbstractPlayerState
    {
        private float _timer;

        public GrabbingLedgeState(PlayerStateMachine shared)
            : base(shared) {}

        public override void ResetState()
        {
            _timer = 0;
        }

        public override void OnStateEnter()
        {
            _player.Anim.Set(PlayerAnims.LEDGE_GRAB);
            _timer = PlayerConstants.LEDGE_GRAB_DURATION;
        }

        public override void EarlyFixedUpdate()
        {
            if (_timer <= 0)
                _player.ChangeState(_player.FreeFall);
        }
        
        public override void FixedUpdate()
        {
            _player.Motor.RelativeVSpeed = PlayerConstants.LEDGE_GRAB_VSPEED;
            _player.HSpeed = PlayerConstants.LEDGE_GRAB_HSPEED;
            _player.SyncWalkVelocityToHSpeed();

            _timer -= Time.deltaTime;
        }
    }

}