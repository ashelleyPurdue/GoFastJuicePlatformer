using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerController : MonoBehaviour
{
    private PlayerMovement _movement;

    private Vector3 _respawnPosition;

    void Awake()
    {
        // Subscribe to events
        CheckpointManager.CheckpointActivated += OnCheckpointReached;
        CheckpointManager.Respawned += OnRespawned;

        // Get components
        _movement = GetComponent<PlayerMovement>();

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
        transform.position = _respawnPosition;
        _movement.ResetState();
        // TODO: Play a respawning animation
    }
}
