using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CheckpointManager
{
    /// <summary>
    /// Fired when the player reaches a checkpoint.
    /// Lets subscribing objects know that they should save their state
    /// so it can be reloaded after death
    /// </summary>
    public static event Action<CheckpointActivatedInfo> CheckpointActivated;

    /// <summary>
    /// Fired when the player has finished dieing and the screen has faded to
    /// black.  Subscribers should revert back to their previously-saved state,
    /// or their default state if no checkpoint has been reached.
    /// </summary>
    public static event Action Respawned;

    static CheckpointManager()
    {

        SceneManager.sceneUnloaded += (Scene scene) =>
        {
            ClearSubscribers();
        };
    }

    public static void ActivateCheckpoint(CheckpointActivatedInfo info)
    {
        CheckpointActivated?.Invoke(info);
    }

    public static void Respawn()
    {
        Respawned?.Invoke();
    }

    /// <summary>
    /// Call this before changing scenes, to ensure unloaded objects get
    /// garbage collected.
    /// </summary>
    private static void ClearSubscribers()
    {
        CheckpointActivated = null;
        Respawned = null;
    }
}

public struct CheckpointActivatedInfo
{
    public Vector3 PlayerRespawnPos;
}
