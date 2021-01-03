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

    public static float JumpVelForHeight(
        float jumpHeight,
        float gravityMetersPerSecondSquared
    )
    {
        float fps = 1 / Time.fixedDeltaTime;
        float discreteConverter = fps / (fps + 1);

        float gravityMetersPerFrameSquared = gravityMetersPerSecondSquared / (fps * fps * discreteConverter);

        // Simulate falling from that height.  If we assume that the jump arc is
        // symmetrical, the velocity we hit the ground at should equal the
        // velocity we left the ground at
        float jumpVelMetersPerFrame = 0;
        for (float y = jumpHeight; y > 0; y -= jumpVelMetersPerFrame * Time.fixedDeltaTime)
        {
            jumpVelMetersPerFrame += gravityMetersPerFrameSquared * Time.fixedDeltaTime;
        }

        // Convert it back into meters per second
        float jumpVel = jumpVelMetersPerFrame * fps * discreteConverter;
        return jumpVel;
    }
}

public struct GravityValues
{
    public float JumpVelocity;
    public float RiseGravity;
    public float FallGravity;
}