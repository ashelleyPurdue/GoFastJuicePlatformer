using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathBarrier : MonoBehaviour
{
    void OnPlayerMotorCollisionStay(PlayerMotor motor)
    {
        var playerController = motor.GetComponent<PlayerController>();
        if (playerController != null)
            playerController.Kill();
    }
}
