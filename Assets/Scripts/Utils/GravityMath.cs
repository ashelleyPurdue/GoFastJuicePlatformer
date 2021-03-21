using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GravityMath
{
    // Using a constant instead of Time.fixedDeltaTime so that this class can
    // be used in a static constructor.  This constant should match the value
    // configured in the Project Settings, or else the computed gravity/vspeed
    // values won't accurately result in the specified jump height or rise/fall
    // times.
    private const float FIXED_TIMESTEP = 0.016f;

    public static GravityValues ComputeGravity(
        float jumpHeight,
        float riseTime,
        float fallTime
    )
    {
        float fps = 1 / FIXED_TIMESTEP;
        float discreteConverter = fps / (fps + 1);

        int riseTimeFrames = Mathf.CeilToInt(riseTime / FIXED_TIMESTEP);
        int fallTimeFrames = Mathf.CeilToInt(fallTime / FIXED_TIMESTEP);

        float jumpVelMetersPerFrame = 2 * jumpHeight / riseTimeFrames;
        float jumpVel = jumpVelMetersPerFrame * fps * discreteConverter;

        float adjustedRiseTime = riseTimeFrames * FIXED_TIMESTEP;
        float adjustedFallTime = fallTimeFrames * FIXED_TIMESTEP;

        float fullRiseGrav = BinarySearch(
            0,
            jumpHeight * 100,
            0.01f,
            100,
            g => Mathf.Abs(jumpHeight - MaxHeight(jumpVel, g))
        );
        float fallGrav = BinarySearch(
            0,
            jumpHeight * 100,
            0.01f,
            100,
            g => Mathf.Abs(adjustedFallTime - RiseFallTime(jumpHeight, g))
        );

        return new GravityValues
        {
            JumpVelocity = jumpVel,
            RiseGravity = fullRiseGrav,
            FallGravity = fallGrav
        };
    }

    public static float BinarySearch(
        float min,
        float max,
        float errorTolerance,
        int maxIterations,
        System.Func<float, float> findError
    )
    {

        for (int i = 0; i < maxIterations; i++)
        {
            float mid = (min + max) / 2;

            float error = findError(mid);
            if (error <= errorTolerance)
            {
                Debug.Log("Within error tolerance");
                return mid;
            }

            // Decide whether to take the bottom path or the top path.
            // We'll go with whichever one decreases the error the most.
            // If neither of them cause the error to go down, then we'll use their
            // midpoints instead.
            
            float maxError = findError((max + mid) / 2);
            float minError = findError((min + mid) / 2);

            float maxErrorDecrease = error - maxError;
            float minErrorDecrease = error - minError;

            if (maxErrorDecrease <= 0 && minErrorDecrease <= 0)
            {
                Debug.Log($"Neither of them make it go down.({minErrorDecrease}, {maxErrorDecrease})  Shrinking the net.");
                min = (min + mid) / 2;
                max = (mid + max) / 2;
                continue;
            }
            else if (maxErrorDecrease > minErrorDecrease)
            {
                Debug.Log("maxError went down the most");
                min = mid;
                continue;
            }
            else if (maxErrorDecrease < minErrorDecrease)
            {
                Debug.Log("minError went down the most");
                max = mid;
                continue;
            }
            else
            {
                Debug.Log("They both have the same error.  Panic!");
                
                // They're both the same, so explore both paths and choose the one
                // with the smallest error
                int remainingIterations = maxIterations - i;
                float bottomResult = BinarySearch(min, mid, errorTolerance, remainingIterations / 2, findError);
                float topResult = BinarySearch(mid, max, errorTolerance, remainingIterations / 2, findError);

                float bottomError = findError(bottomResult);
                float topError = findError(topResult);

                return bottomError < topError
                    ? bottomResult
                    : topResult;
            }
        }
        
        Debug.Log("Gave up after binary searching for too many iterations");
        return (min + max) / 2;
    }

    public static float JumpVelForHeight(
        float jumpHeight,
        float gravityMetersPerSecondSquared
    )
    {
        return BinarySearch(
            0,
            jumpHeight * 100,
            0.01f,
            100,
            FindError
        );

        float FindError(float jumpVel)
        {
            float actualHeight = MaxHeight(jumpVel, gravityMetersPerSecondSquared);
            return Mathf.Abs(actualHeight - jumpHeight);
        }
    }

    public static float MaxHeight(float jumpVel, float gravity)
    {
        float y = 0;
        for (jumpVel = jumpVel; jumpVel > 0; jumpVel -= gravity * FIXED_TIMESTEP)
            y += jumpVel * FIXED_TIMESTEP;

        return y;
    }

    public static float RiseFallTime(float jumpHeight, float gravity)
    {
        float v = 0;
        float t = 0;
        for (float y = jumpHeight; y > 0; y += v * FIXED_TIMESTEP)
        {
            v -= gravity * FIXED_TIMESTEP;
            t += FIXED_TIMESTEP;
        }

        return t;
    }
}

public struct GravityValues
{
    public float JumpVelocity;
    public float RiseGravity;
    public float FallGravity;
}