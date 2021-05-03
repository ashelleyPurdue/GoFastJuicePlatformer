using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerStates
{
    public class GrabbingLedgeState : AbstractPlayerState
    {
        private float _lastLedgeGrabStartTime;

        public GrabbingLedgeState(PlayerStateMachine shared)
            : base(shared) {}

        public override void OnStateEnter()
        {
            _player.Anim.Set(PlayerAnims.LEDGE_GRAB);
            _lastLedgeGrabStartTime = Time.time;
        }

        public override void EarlyFixedUpdate()
        {
            if (IsLedgeGrabTimeExpired())
                _player.ChangeState(_player.FreeFall);
        }
        
        public override void FixedUpdate()
        {
            _player.Motor.RelativeVSpeed = PlayerConstants.LEDGE_GRAB_VSPEED;
            _player.HSpeed = PlayerConstants.LEDGE_GRAB_HSPEED;
            _player.SyncWalkVelocityToHSpeed();
        }

        private bool IsLedgeGrabTimeExpired()
        {
            return (Time.time >= _lastLedgeGrabStartTime + PlayerConstants.LEDGE_GRAB_DURATION);
        }
    }

}