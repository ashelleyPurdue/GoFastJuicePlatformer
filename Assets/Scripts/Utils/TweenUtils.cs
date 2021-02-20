using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TweenUtils
{
    public static Quaternion DecayTowards(
        Quaternion current,
        Quaternion target,
        float halfLife,
        float deltaTime
    )
    {
        float angleDelta = Quaternion.Angle(current, target);
        float rotSpeed = (angleDelta / 2) / halfLife;
        return Quaternion.RotateTowards(
            current,
            target,
            rotSpeed * deltaTime
        );
    }

    public static Vector3 DecayTowards(
        Vector3 current,
        Vector3 target,
        float halfLife,
        float deltaTime
    )
    {
        float dist = Vector3.Distance(current, target);
        float speed = (dist / 2) / halfLife;
        return Vector3.MoveTowards(
            current,
            target,
            speed * deltaTime
        );
    }

    public static float DecayTowards(
        float current,
        float target,
        float halfLife,
        float deltaTime
    )
    {
        float diff = Mathf.Abs(current - target);
        float speed = (diff / 2) / halfLife;
        return Mathf.MoveTowards(
            current,
            target,
            speed * deltaTime
        );
    }

    public static float DecayTowardsAngle(
        float current,
        float target,
        float halfLife,
        float deltaTime
    )
    {
        float deltaAngle = Mathf.DeltaAngle(current, target);
        float speed = (deltaAngle / 2) / halfLife;
        return Mathf.MoveTowardsAngle(
            current,
            target,
            speed * deltaTime
        );
    }
}
