using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(IPlayerInput))]
[RequireComponent(typeof(PlayerGroundDetector))]
public class CrappyPlayerVisuals : MonoBehaviour
{
    public Transform _model;
    public Animator _animator;

    private PlayerMovement _movement;
    private IPlayerInput _input;
    private PlayerGroundDetector _ground;

    void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
        _input = GetComponent<IPlayerInput>();
        _ground = GetComponent<PlayerGroundDetector>();
    }

    void Update()
    {
        UpdateAnimatorParams();
        FaceHAngle();
        TiltWithSpeed();
    }

    private void UpdateAnimatorParams()
    {
        float speedPercent = _movement.HSpeed / PlayerMovement.HSPEED_MAX_GROUND;

        _animator.SetFloat("RunSpeed", speedPercent);
        _animator.SetFloat("VSpeed", _movement.VSpeed);
        _animator.SetBool("IsGrounded", _ground.IsGrounded);
        _animator.SetBool("IsGrabbingLedge", _movement.IsGrabbingLedge);
    }

    private void FaceHAngle()
    {
        var eulers = _model.localEulerAngles;
        eulers.y = -_movement.HAngleDeg + 90;
        _model.localEulerAngles = eulers;
    }

    private void TiltWithSpeed()
    {
        float speedPercent = _movement.HSpeed / PlayerMovement.HSPEED_MAX_GROUND;

        var eulers = _model.localEulerAngles;
        eulers.x = SignedPow(speedPercent, 3) * 20;
        _model.localEulerAngles = eulers;
    }

    private float SignedPow(float f, float p)
    {
        return Mathf.Pow(f, p) * Mathf.Sign(f);
    }
}
