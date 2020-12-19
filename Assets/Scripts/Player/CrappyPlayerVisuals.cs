using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerGroundDetector))]
[RequireComponent(typeof(PlayerWallDetector))]
public class CrappyPlayerVisuals : MonoBehaviour
{
    public Transform _model;
    public Animator _animator;

    private PlayerMovement _movement;
    private PlayerGroundDetector _ground;
    private PlayerWallDetector _wall;

    void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
        _ground = GetComponent<PlayerGroundDetector>();
        _wall = GetComponent<PlayerWallDetector>();
    }

    void Update()
    {
        UpdateAnimatorParams();

        if (!_movement.IsWallSliding)
        {
            FaceHAngle();
            TiltWithSpeed();
        }
        else
        {
            FaceWallSlide();
        }
    }

    private void UpdateAnimatorParams()
    {
        float speedPercent = _movement.HSpeed / PlayerMovement.HSPEED_MAX_GROUND;

        _animator.SetFloat("RunSpeed", speedPercent);
        _animator.SetFloat("VSpeed", _movement.VSpeed);
        _animator.SetBool("IsGrounded", _ground.IsGrounded);
        _animator.SetBool("IsGrabbingLedge", _movement.IsGrabbingLedge);
        _animator.SetBool("IsWallSliding", _movement.IsWallSliding);
    }

    private void FaceWallSlide()
    {
        _model.forward = _wall.LastWallNormal.Flattened();
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
