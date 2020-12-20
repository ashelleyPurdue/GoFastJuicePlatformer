using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerGroundDetector))]
[RequireComponent(typeof(PlayerWallDetector))]
public class CrappyPlayerVisuals : MonoBehaviour
{
    private const float MODEL_ROT_SPEED_DEG_PER_SEC = 720;

    public Transform _model;
    public Animator _animator;

    private PlayerMovement _movement;
    private PlayerGroundDetector _ground;
    private PlayerWallDetector _wall;

    private Quaternion _targetRot;

    void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
        _ground = GetComponent<PlayerGroundDetector>();
        _wall = GetComponent<PlayerWallDetector>();

        _targetRot = _model.transform.localRotation;
    }

    void Update()
    {
        UpdateAnimatorParams();

        if (_ground.IsGrounded)
            TiltWithSpeed();
        else
            DoNotTilt();

        if (!_movement.IsWallSliding)
            FaceHAngle();
        else
            FaceWallSlide();

        // Rotate toward the target rot
        var rot = _model.transform.localRotation;
        rot = Quaternion.RotateTowards(rot, _targetRot, MODEL_ROT_SPEED_DEG_PER_SEC * Time.deltaTime);
        _model.transform.localRotation = rot;
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
        var forward = _wall.LastWallNormal.Flattened();
        _targetRot = Quaternion.LookRotation(forward);
    }

    private void FaceHAngle()
    {
        var eulers = _targetRot.eulerAngles;
        eulers.y = -_movement.HAngleDeg + 90;
        _targetRot.eulerAngles = eulers;
    }

    private void TiltWithSpeed()
    {
        float speedPercent = _movement.HSpeed / PlayerMovement.HSPEED_MAX_GROUND;

        var eulers = _targetRot.eulerAngles;
        eulers.x = SignedPow(speedPercent, 3) * 20;
        _targetRot.eulerAngles = eulers;
    }

    private void DoNotTilt()
    {
        var eulers = _targetRot.eulerAngles;
        eulers.x = 0;
        _targetRot.eulerAngles = eulers;
    }

    private float SignedPow(float f, float p)
    {
        return Mathf.Pow(f, p) * Mathf.Sign(f);
    }
}
