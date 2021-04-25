using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerAnimationManager
{
    /// <summary>
    /// The angle (in degrees) that the player model should be facing
    /// The Y-angle, essentially
    /// </summary>
    /// <value></value>
    float HAngleDeg {get; set;}

    /// <summary>
    /// The X-angle, essentially
    /// </summary>
    float ForwardTiltAngleDeg {get; set;}

    /// <summary>
    ///     Starts playing the given animation, if it's not already playing.
    ///     Simultaneously sets the animation speed to the given value(default 1).
    ///     
    ///     Speed changes occur even if animName is currently playing.
    /// </summary>
    /// <param name="animName">
    ///     Which animation to switch to.
    /// </param>
    /// <param name="transitionDuration">
    ///     How long it should take to fade between animations, if transitioning
    ///     from a different animation.
    /// </param>
    /// <param name="speed">
    ///     Multiplier for how fast the animation plays.
    /// </param>
    void Set(
        string animName,
        float transitionDuration = 0,
        float speed = 1
    );

    /// <summary>
    ///     Starts playing the given animation from the beginning, and prevents
    ///     <see cref="Set"> from replacing it for a specified amount
    ///     of time.
    /// 
    ///     Can be overriden by another call to <see cref="Force">.
    /// 
    ///     If the specified animation is already playing, then it will start
    ///     over from the beginning.
    /// </summary>
    /// <param name="animName">
    ///     Which animation to play
    /// </param>
    /// <param name="transitionDuration">
    ///     How long it should take to fade between animations, if transitioning
    ///     from a different animation.
    /// </param>
    /// <param name="forcedDuration">
    /// 
    /// </param>
    void Force(
        string animName,
        float transitionDuration,
        float forcedDuration
    );
}

public static class PlayerAnims
{
    public const string FALL = "PlayerFall";
    public const string SKID = "PlayerSkid";
    public const string WALL_SLIDE = "PlayerWallSlide";
    public const string IDLE = "PlayerIdle";
    public const string RUN = "PlayerRun";
    public const string LEDGE_GRAB = "PlayerLedgeGrab";
    public const string STANDARD_JUMP = "PlayerJump_0";
    public const string CHAINED_JUMP = "PlayerJump_1";
    public const string SIDE_FLIP = "PlayerSideFlip";
    public const string ROLL = "PlayerRoll";
    public const string DIVE = "PlayerDive";
    public const string BONK = "PlayerBonk";
}

public class PlayerAnimationManager : MonoBehaviour, IPlayerAnimationManager
{
    private readonly float TWEEN_HALF_LIFE = 1f / 60;

    public Animator _animator;
    public Transform _model;

    public float HAngleDeg {get; set;}
    public float ForwardTiltAngleDeg {get; set;}

    private string _currentAnim;
    private float _forcedAnimEndTime = 0;

    void Update()
    {
        _model.rotation = TweenUtils.DecayTowards(
            _model.rotation, 
            GetTargetRot(),
            TWEEN_HALF_LIFE,
            Time.deltaTime
        );
    }

    public void Set(
        string animName,
        float transitionDuration,
        float speed
    )
    {
        // Error if the state doesn't exist
        if (!_animator.HasState(0, Animator.StringToHash(animName)))
            throw new Exception($"The state {animName} does not exist.");

        // Don't change anything if we're currently in a forced animation
        if (Time.time < _forcedAnimEndTime)
            return;

        // Always update the speed, even if we're already in the animation
        _animator.speed = speed;

        // Don't transition if we're already in that animation
        if (_currentAnim == animName)
            return;
        _currentAnim = animName;

        // Actually do the transition
        if (transitionDuration == 0)
            _animator.Play(animName);
        else
            _animator.CrossFadeInFixedTime(animName, transitionDuration);
    }

    public void Force(
        string animName,
        float transitionDuration,
        float forcedDuration
    )
    {
        _forcedAnimEndTime = Time.time + forcedDuration;
        Set(animName, transitionDuration, 1);
    }

    private Quaternion GetTargetRot()
    {
        return Quaternion.Euler(
            ForwardTiltAngleDeg,
            -HAngleDeg + 90,
            0
        );
    }
}
