using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GravityMath
{
    public static GravityValues ComputeGravity(JumpParameters jump)
    {
        float fps = 1 / Time.fixedDeltaTime;
        float discreteConverter = fps / (fps + 1);

        int fullJumpRiseFrames = Mathf.CeilToInt(jump.FullJumpRiseTime / Time.fixedDeltaTime);
        int fullJumpFallFrames = Mathf.CeilToInt(jump.FullJumpFallTime / Time.fixedDeltaTime);

        float jumpVelMetersPerFrame = 2 * jump.FullJumpHeight / fullJumpRiseFrames;
        float fullRiseGravMetersPerFrameSquared = jumpVelMetersPerFrame / fullJumpRiseFrames;
        float shortRiseGravMetersPerFrameSquared = (jumpVelMetersPerFrame * jumpVelMetersPerFrame / (2 * jump.ShortJumpHeight));
        float fallGravMetersPerFrameSquared =  (2 * jump.FullJumpHeight / (fullJumpFallFrames * fullJumpFallFrames));

        float jumpVel = jumpVelMetersPerFrame * fps * discreteConverter;
        float fullRiseGrav = fullRiseGravMetersPerFrameSquared * fps * fps;
        float shortRiseGrav = shortRiseGravMetersPerFrameSquared * fps * fps * discreteConverter;
        float fallGrav = fallGravMetersPerFrameSquared * fps * fps * discreteConverter;

        return new GravityValues
        {
            JumpVelocity = jumpVel,
            FullJumpRiseGravity = fullRiseGrav,
            ShortJumpRiseGravity = shortRiseGrav,
            FallGravity = fallGrav
        };
    }
}

public struct JumpParameters
{
    public float FullJumpHeight;
    public float FullJumpRiseTime;
    public float FullJumpFallTime;

    public float ShortJumpHeight;
}

public struct GravityValues
{
    public float JumpVelocity;
    public float FullJumpRiseGravity;
    public float ShortJumpRiseGravity;
    public float FallGravity;
}