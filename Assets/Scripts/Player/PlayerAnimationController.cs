using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerStateMachine))]
[RequireComponent(typeof(PlayerMotor))]
public class PlayerAnimationController : MonoBehaviour
{
    private readonly float TWEEN_HALF_LIFE = 1f / 60;

    public Transform _model;
    public Animator _animator;

    private PlayerStateMachine _stateMachine;
    private PlayerMotor _motor;

    private string _currentState;
    private const string PLAYER_FALL = "PlayerFall";
    private const string PLAYER_WALL_SLIDE = "PlayerWallSlide";
    private const string PLAYER_IDLE = "PlayerIdle";
    private const string PLAYER_RUN = "PlayerRun";
    private const string PLAYER_LEDGE_GRAB = "PlayerLedgeGrab";
    private const string PLAYER_JUMP_0 = "PlayerJump_0";
    private const string PLAYER_JUMP_1 = "PlayerJump_1";
    private const string PLAYER_ROLL = "PlayerRoll";
    private const string PLAYER_DIVE = "PlayerDive";
    private const string PLAYER_BONK = "PlayerBonk";

    private float _forceSetStateTimer = 0;
    
    void Awake()
    {
        _stateMachine = GetComponent<PlayerStateMachine>();
        _motor = GetComponent<PlayerMotor>();

        // Subscribe to events
        _stateMachine.StartedJumping += OnStartedJumping;
        _stateMachine.StartedDiving += OnStartedDiving;
        _stateMachine.GrabbedLedge += OnGrabbedLedge;
        _stateMachine.Bonked += OnBonked;
    }

    
    void Update()
    {
        UpdateRotation();
        UpdateAnimatorParams();
        NaturalStateTransitions();
    }

    private void UpdateAnimatorParams()
    {
        float speedPercent = _stateMachine.HSpeed / PlayerConstants.HSPEED_MAX_GROUND;

        _animator.SetFloat("RunSpeed", speedPercent);
        _animator.SetFloat("VSpeed", _motor.RelativeVSpeed);
    }

    private void NaturalStateTransitions()
    {
        // Don't do any natural transitions if a state was force-set recently
        if (_forceSetStateTimer > 0)
        {
            _forceSetStateTimer -= Time.deltaTime;
            return;
        }

        switch (_stateMachine.CurrentAnimationHint)
        {
            case PlayerAnimationHint.Diving:
            {
                SetState(PLAYER_DIVE, 0.1f);
                break;
            }
            case PlayerAnimationHint.WallSliding:
            {
                SetState(PLAYER_WALL_SLIDE, 0.1f); 
                break;
            }
            case PlayerAnimationHint.Rolling:
            {
                SetState(PLAYER_ROLL, 0.1f);
                break;
            }
            case PlayerAnimationHint.Walking:
            {
                if (_stateMachine.HSpeed > 0)
                    SetState(PLAYER_RUN, 0.25f);
                else
                    SetState(PLAYER_IDLE, 0.25f); 
                break;
            }
            case PlayerAnimationHint.FreeFall:
            {
                if (_motor.RelativeVSpeed < 0)
                    SetState(PLAYER_FALL, 0.25f);
                break;
            }
        }
    }

    private void OnStartedJumping()
    {
        var state = _stateMachine.ChainedJumpCount % 2 == 0
            ? PLAYER_JUMP_0
            : PLAYER_JUMP_1;
        ForceSetState(state);
    }

    private void OnStartedDiving()
    {
        ForceSetState(PLAYER_DIVE, 0.1f, 0.25f);
    }

    private void OnGrabbedLedge()
    {
        ForceSetState(PLAYER_LEDGE_GRAB, 0, 0.25f);
    }

    private void OnBonked()
    {
        ForceSetState(PLAYER_BONK, 0, PlayerConstants.BONK_DURATION);
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
        _model.rotation = TweenUtils.DecayTowards(
            _model.rotation, 
            GetTargetRot(),
            TWEEN_HALF_LIFE,
            Time.deltaTime
        );
    }

    private Quaternion GetTargetRot()
    {
        switch (_stateMachine.CurrentAnimationHint)
        {
            case PlayerAnimationHint.Diving: return GetTargetRotDiving();
            case PlayerAnimationHint.WallSliding: return FaceWallSlide();
            case PlayerAnimationHint.Walking: return TiltWithSpeed(FaceHAngle());

            default: return FaceHAngle();
        }
    }

    private Quaternion FaceWallSlide()
    {
        var forward = _motor.LastWallNormal.Flattened();
        return Quaternion.LookRotation(forward);
    }

    private Quaternion FaceHAngle()
    {
        return Quaternion.Euler(
            0,
            -_stateMachine.HAngleDeg + 90,
            0
        );
    }

    private Quaternion TiltWithSpeed(Quaternion targetRot)
    {
        float speedPercent = _stateMachine.HSpeed / PlayerConstants.HSPEED_MAX_GROUND;

        var eulers = targetRot.eulerAngles;
        eulers.x = SignedPow(speedPercent, 3) * 20;
        targetRot.eulerAngles = eulers;

        return targetRot;
    }
    
    private Quaternion GetTargetRotDiving()
    {
        return Quaternion.LookRotation(_motor.TotalVelocity.normalized);
    }

    private float SignedPow(float f, float p)
    {
        return Mathf.Pow(f, p) * Mathf.Sign(f);
    }
}
