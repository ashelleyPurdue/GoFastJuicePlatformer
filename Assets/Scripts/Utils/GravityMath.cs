using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GravityMath
{
    public static GravityValues ComputeGravity(
        float jumpHeight,
        float riseTime,
        float fallTime
    )
    {
        float fps = 1 / Time.fixedDeltaTime;
        float discreteConverter = fps / (fps + 1);

        int riseTimeFrames = Mathf.CeilToInt(riseTime / Time.fixedDeltaTime);
        int fallTimeFrames = Mathf.CeilToInt(fallTime / Time.fixedDeltaTime);

        float jumpVelMetersPerFrame = 2 * jumpHeight / riseTimeFrames;
        float fullRiseGravMetersPerFrameSquared = jumpVelMetersPerFrame / riseTimeFrames;
        float fallGravMetersPerFrameSquared =  (2 * jumpHeight / (fallTimeFrames * fallTimeFrames));

        float jumpVel = jumpVelMetersPerFrame * fps * discreteConverter;
        float fullRiseGrav = fullRiseGravMetersPerFrameSquared * fps * fps;
        float fallGrav = fallGravMetersPerFrameSquared * fps * fps * discreteConverter;

        return new GravityValues
        {
            JumpVelocity = jumpVel,
            RiseGravity = fullRiseGrav,
            FallGravity = fallGrav
        };
    }
}

public struct GravityValues
{
    public float JumpVelocity;
    public float RiseGravity;
    public float FallGravity;
}