using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class InputUtils
{
    /// <summary>
    /// Converts left stick input into world space, based on the current
    /// camera angle.
    /// </summary>
    /// <param name="leftStick"></param>
    /// <returns></returns>
    public static Vector3 LeftStickToWorldSpace(Vector2 leftStick)
    {
        var rawInput = new Vector3(
            leftStick.x,
            0,
            leftStick.y
        );
        
        // Cap the magnitude at 1.0, because some people like to play with
        // keyboards.
        if (rawInput.magnitude > 1)
            rawInput.Normalize();

        // Rotate it into camera space
        var adjustedInput = 
            Camera.main.transform.forward.Flattened().normalized * rawInput.z +
            Camera.main.transform.right.Flattened().normalized * rawInput.x;
        
        return adjustedInput;
    }
}
