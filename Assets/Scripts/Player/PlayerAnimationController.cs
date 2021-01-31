using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerGroundDetector))]
[RequireComponent(typeof(PlayerWallDetector))]
public class PlayerAnimationController : MonoBehaviour
{
    private const float MODEL_ROT_SPEED_DEG_PER_SEC = 720;

    public Transform _model;
    public Animator _animator;

    private PlayerMovement _movement;
    private PlayerGroundDetector _ground;
    private PlayerWallDetector _wall;

    private string _currentState;
    private const string PLAYER_FALL = "PlayerFall";
    private const string PLAYER_WALL_SLIDE = "PlayerWallSlide";
    private const string PLAYER_IDLE = "PlayerIdle";
    private const string PLAYER_RUN = "PlayerRun";
    private const string PLAYER_LEDGE_GRAB = "PlayerLedgeGrab";
    private const string PLAYER_JUMP_0 = "PlayerJump_0";
    private const string PLAYER_JUMP_1 = "PlayerJump_1";

    private float _forceSetStateTimer = 0;
    
    void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
        _ground = GetComponent<PlayerGroundDetector>();
        _wall = GetComponent<PlayerWallDetector>();

        // Subscribe to events
        _movement.StartedJumping += OnStartedJumping;
        _movement.GrabbedLedge += OnGrabbedLedge;
    }

    
    void Update()
    {
        UpdateRotation();
        UpdateAnimatorParams();
        NaturalStateTransitions();
    }

    private void UpdateAnimatorParams()
    {
        float speedPercent = _movement.HSpeed / PlayerConstants.HSPEED_MAX_GROUND;

        _animator.SetFloat("RunSpeed", speedPercent);
        _animator.SetFloat("VSpeed", _movement.VSpeed);
    }

    private void NaturalStateTransitions()
    {
        // Don't do any natural transitions if a state was force-set recently
        if (_forceSetStateTimer > 0)
        {
            _forceSetStateTimer -= Time.deltaTime;
            return;
        }

        switch (_movement.CurrentState)
        {
            case PlayerMovement.State.WallSliding: SetState(PLAYER_WALL_SLIDE, 0.1f); break;
            
            case PlayerMovement.State.Walking:
                if (_movement.HSpeed > 0)
                    SetState(PLAYER_RUN, 0.25f);
                else
                    SetState(PLAYER_IDLE, 0.25f);
                break;

            case PlayerMovement.State.FreeFall:
                if (_movement.VSpeed < 0)
                    SetState(PLAYER_FALL, 0.25f);
                break;
        }
    }

    private void OnStartedJumping()
    {
        var state = _movement.ChainedJumpCount % 2 == 0
            ? PLAYER_JUMP_0
            : PLAYER_JUMP_1;
        ForceSetState(state);
    }

    private void OnGrabbedLedge()
    {
        ForceSetState(PLAYER_LEDGE_GRAB, 0, 0.25f);
    }

    /// <summary>
    /// Transitions to the given state, unless we're already in that state.
    /// </summary>
    /// <param name="stateName"></param>
    /// <param name="transitionDuration"></param>
    private void SetState(string stateName, float transitionDuration = 0)
    {
        // Error if the state doesn't exist
        if (!_animator.HasState(0, Animator.StringToHash(stateName)))
            throw new Exception($"The state {stateName} does not exist.");

        // Only transition if we're not already in that state
        if (_currentState == stateName)
            return;
        _currentState = stateName;

        // Actually do the transition
        if (transitionDuration == 0)
            _animator.Play(stateName);
        else
            _animator.CrossFadeInFixedTime(stateName, transitionDuration);
    }

    /// <summary>
    /// Just like SetState, except it temporarily disables future "natural"
    /// transitions.
    /// 
    /// Use this for animations that are triggered by events.
    /// </summary>
    /// <param name="stateName"></param>
    /// <param name="transitionDuration"></param>
    private void ForceSetState(
        string stateName, 
        float transitionDuration = 0,
        float? forcedDuration = null
    )
    {
        _forceSetStateTimer = forcedDuration ?? Time.fixedDeltaTime;
        SetState(stateName, transitionDuration);
    }

    private void UpdateRotation()
    {
        _model.rotation = Quaternion.RotateTowards(
            _model.rotation, 
            GetTargetRot(),
            MODEL_ROT_SPEED_DEG_PER_SEC * Time.deltaTime
        );
    }

    private Quaternion GetTargetRot()
    {
        var targetRot = FaceHAngle();
        
        if (_movement.CurrentState == PlayerMovement.State.WallSliding)
            targetRot = FaceWallSlide();

        if (_movement.CurrentState == PlayerMovement.State.Walking)
            targetRot = TiltWithSpeed(targetRot);

        return targetRot;
    }

    private Quaternion FaceWallSlide()
    {
        var forward = _wall.LastWallNormal.Flattened();
        return Quaternion.LookRotation(forward);
    }

    private Quaternion FaceHAngle()
    {
        return Quaternion.Euler(
            0,
            -_movement.HAngleDeg + 90,
            0
        );
    }

    private Quaternion TiltWithSpeed(Quaternion targetRot)
    {
        float speedPercent = _movement.HSpeed / PlayerConstants.HSPEED_MAX_GROUND;

        var eulers = targetRot.eulerAngles;
        eulers.x = SignedPow(speedPercent, 3) * 20;
        targetRot.eulerAngles = eulers;

        return targetRot;
    }
    
    private float SignedPow(float f, float p)
    {
        return Mathf.Pow(f, p) * Mathf.Sign(f);
    }
}
