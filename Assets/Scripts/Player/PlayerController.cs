using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PlayerStateMachine))]
[RequireComponent(typeof(PlayerMotor))]
public class PlayerController : MonoBehaviour
{
    private PlayerStateMachine _stateMachine;
    private PlayerMotor _motor;

    private Vector3 _respawnPosition;

    void Awake()
    {
        // Subscribe to events
        CheckpointManager.CheckpointActivated += OnCheckpointReached;
        CheckpointManager.Respawned += OnRespawned;

        // Get components
        _stateMachine = GetComponent<PlayerStateMachine>();
        _motor = GetComponent<PlayerMotor>();

        _respawnPosition = transform.position;
    }

    public void Kill()
    {
        // TODO: Play a dying animation, and then respawn AFTER it finishes.
        CheckpointManager.Respawn();
    }

    private void OnCheckpointReached(CheckpointActivatedInfo info)
    {
        _respawnPosition = info.PlayerRespawnPos;
    }

    private void OnRespawned()
    {
        _motor.SetPosition(_respawnPosition);
        _stateMachine.ResetState();
        // TODO: Play a respawning animation
    }
}
