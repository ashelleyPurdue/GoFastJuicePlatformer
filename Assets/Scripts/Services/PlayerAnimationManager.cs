using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    /// 
    ///     If you try to play animation C while the player is in the middle
    ///     of a transtion from A -> B, then the transition will be interrupted
    ///     and replaced with A -> C.
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
    /// 
    ///     If you try to play animation C while the player is in the middle
    ///     of a transtion from A -> B, then the transition will be interrupted
    ///     and replaced with A -> C.
    /// </summary>
    /// <param name="animName">
    ///     Which animation to play
    /// </param>
    /// <param name="transitionDuration">
    ///     How long it should take to fade between animations, if transitioning
    ///     from a different animation.
    /// </param>
    /// <param name="forcedDuration">
    ///     How long to disable <see cref="Set"> for
    /// </param>
    /// <param name="speed">
    ///     Multiplier for how fast the animation plays.
    /// </param>
    void Force(
        string animName,
        float transitionDuration = 0,
        float forcedDuration = 1,
        float speed = 1
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
    private const float TWEEN_HALF_LIFE = 1f / 60;

    public Animator _animator;
    public Transform _model;

    public float HAngleDeg {get; set;}
    public float ForwardTiltAngleDeg {get; set;}

    private string _currentAnim;
    private float _forcedAnimEndTime = 0;

    private float _transitionStartTime;
    private float _transitionEndTime;

    private Animator _currentAnimator;
    private Animator _prevAnimator;
    private Transform[] _visibleBones;
    private Transform[] _currentBones;
    private Transform[] _prevBones;

    void Awake()
    {
        // Make two clones of the animator: one for playing the current animation,
        // and another for playing the next animation(during a transition).
        // We will smoothly lerp between these animators during a transition.
        _currentAnimator = CloneAnimatorStructure(_animator);
        _prevAnimator = CloneAnimatorStructure(_animator);

        _visibleBones = TraverseChildren(_animator.transform).ToArray();
        _currentBones = TraverseChildren(_currentAnimator.transform).ToArray();
        _prevBones = TraverseChildren(_prevAnimator.transform).ToArray();

        // Disable the animators, so we can update them manually.
        _animator.enabled = false;
        _currentAnimator.enabled = false;
        _prevAnimator.enabled = false;
    }

    void Update()
    {
        DebugDisplay.PrintLine("Delta time: " + (Time.time / Time.frameCount));
        DebugDisplay.PrintLine("Animation speed: " + _animator.speed);
        _model.rotation = TweenUtils.DecayTowards(
            _model.rotation, 
            GetTargetRot(),
            TWEEN_HALF_LIFE,
            Time.deltaTime
        );

        UpdateBones();
    }

    private void UpdateBones()
    {
        // Manually update the animators.
        // Only update the previous animator if we're in a transition.
         _currentAnimator.Update(Time.deltaTime);

        if (IsInTransition())
            _prevAnimator.Update(Time.deltaTime);

        // Sync the internal animator to the main one.
        // Tween them if we're in a transition
        if (!IsInTransition())
        {
            ApplyTransformHierarchy(_currentBones, _visibleBones);
        }
        else
        {
            float timeInTransition = Time.time - _transitionStartTime;
            float transitionDuration = _transitionEndTime - _transitionStartTime;
            float t = timeInTransition / transitionDuration;
            LerpTransformHierarchy(
                _prevBones,
                _currentBones,
                _visibleBones,
                t
            );
        }
    }

    public void Set(
        string animName,
        float transitionDuration,
        float speed
    )
    {
        AssertAnimNameExists(animName);

        // Don't change anything if we're currently in a forced animation
        if (Time.time < _forcedAnimEndTime)
            return;

        // Don't transition if we're already in that animation.
        // Still update its speed.
        if (_currentAnim == animName)
        {
            _currentAnimator.speed = speed;
            return;
        }

        // Actually do the transition
        StartTransition(
            animName,
            transitionDuration,
            speed
        );
    }

    public void Force(
        string animName,
        float transitionDuration,
        float forcedDuration,
        float speed
    )
    {
        AssertAnimNameExists(animName);

        // Disallow Set() from working for a short time
        _forcedAnimEndTime = Time.time + forcedDuration;

        // Actually do the transition
        StartTransition(
            animName,
            transitionDuration,
            speed
        );
    }

    private void AssertAnimNameExists(string animName)
    {
        if (!_animator.HasState(0, Animator.StringToHash(animName)))
            throw new Exception($"The state {animName} does not exist.");
    }

    private void StartTransition(
        string animName,
        float transitionDuration,
        float speed
    )
    {
        // Start playing the new animation on the other animator, and swap the
        // animators.  The new and previous animations will now be running in
        // parallel.
        if (!IsInTransition())
        {
            var animatorHolder = _currentAnimator;
            _currentAnimator = _prevAnimator;
            _prevAnimator = animatorHolder;

            var bonesHolder = _currentBones;
            _currentBones = _prevBones;
            _prevBones = bonesHolder;
        }

        _currentAnimator.Play(animName, 0, 0);
        _currentAnimator.speed = speed;

        // Record some data about the transition
        _transitionStartTime = Time.time;
        _transitionEndTime = _transitionStartTime + transitionDuration;

        _currentAnim = animName;

        // HACK: Immediately update the bones, to make things more responsive
        UpdateBones();
    }

    private Quaternion GetTargetRot()
    {
        return Quaternion.Euler(
            ForwardTiltAngleDeg,
            -HAngleDeg + 90,
            0
        );
    }

    /// <summary>
    /// Creates a tree of GameObjects whose hierarchy matches the given animator's.
    /// The root of the new hierarchy has an Animator attached to it with the
    /// same parameters as the source Animator.
    /// Returns the Animator at the root of the newly-created hierarchy.
    /// </summary>
    /// <param name="src"></param>
    /// <returns></returns>
    private Animator CloneAnimatorStructure(Animator srcAnim)
    {
        // Clone the structure
        var cloneObj = new GameObject();
        var cloneTf = cloneObj.transform;
        CloneChildren(srcAnim.transform, cloneTf);

        // Add a copy of srcAnim to the clone
        var cloneAnim = cloneObj.AddComponent<Animator>();
        cloneAnim.runtimeAnimatorController = srcAnim.runtimeAnimatorController;
        cloneAnim.updateMode = srcAnim.updateMode;

        // And...return it!
        return cloneAnim;

        void CloneChildren(Transform src, Transform dst)
        {
            // Add clones of each child of src to dst
            for (int i = 0; i < src.childCount; i++)
            {
                var srcChild = src.GetChild(i);
                
                // Make a clone of this child and attach it to dst
                var dstChildObj = new GameObject(srcChild.name);
                var dstChild = dstChildObj.transform;
                
                dstChild.SetParent(dst);
                dstChild.localPosition = srcChild.localPosition;
                dstChild.localRotation = srcChild.localRotation;
                dstChild.localScale = srcChild.localScale;

                // Recursively add clones of the src child's children
                CloneChildren(srcChild, dstChild);
            }
        }
    }

    /// <summary>
    /// Does a depth-first traversal of the given transform hierarchy.
    /// </summary>
    /// <param name="root"></param>
    /// <returns></returns>
    private IEnumerable<Transform> TraverseChildren(Transform root)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            yield return child;

            foreach (var grandchild in TraverseChildren(child))
                yield return grandchild;
        }
    }

    /// <summary>
    /// Copies the local postion, local rotation, and local scale of src to dst.
    /// </summary>
    /// <param name="src"></param>
    /// <param name="dst"></param>
    private void ApplyTransformHierarchy(Transform[] src, Transform[] dst)
    {
        for (int i = 0; i < src.Length; i++)
        {
            dst[i].localPosition = src[i].localPosition;
            dst[i].localScale    = src[i].localScale;
            dst[i].localRotation = src[i].localRotation;
        }
    }

    /// <summary>
    /// Linearly interpolates the local position, local rotation, and local scale
    /// of a and b, and applies the result to dst.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="dst"></param>
    /// <param name="t"></param>
    private void LerpTransformHierarchy(
        Transform[] a,
        Transform[] b,
        Transform[] dst,
        float t
    )
    {
        for (int i = 0; i < a.Length; i++)
        {
            dst[i].localPosition = Vector3.Lerp(a[i].localPosition, b[i].localPosition, t);
            dst[i].localScale    = Vector3.Lerp(a[i].localScale, b[i].localScale, t);
            dst[i].localRotation = Quaternion.Lerp(a[i].localRotation, b[i].localRotation, t);
        }
    }

    private bool IsInTransition()
    {
        return Time.time < _transitionEndTime;
    }
}
