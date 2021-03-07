using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerMovement
{
    private class GrabbingLedgeState : AbstractPlayerState
    {
        private float _timer;

        public GrabbingLedgeState(PlayerMovement shared)
            : base(shared) {}

        public override void ResetState()
        {
            _timer = 0;
        }

        public override void OnStateEnter()
        {
            _timer = PlayerConstants.LEDGE_GRAB_DURATION;
            _shared.GrabbedLedge?.Invoke();
        }

        public override void EarlyFixedUpdate()
        {
            if (_timer <= 0)
                _shared.CurrentState = State.FreeFall;
        }
        
        public override void FixedUpdate()
        {
            _shared.VSpeed = PlayerConstants.LEDGE_GRAB_VSPEED;
            _shared.HSpeed = PlayerConstants.LEDGE_GRAB_HSPEED;
            SyncWalkVelocityToHSpeed();

            _timer -= Time.deltaTime;
        }
    }

}