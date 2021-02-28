using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crate : MonoBehaviour
{
    private bool _brokenNow = false;
    private bool _brokenLastCheckpoint = false;

    public void Awake()
    {
        // Subscribe to events
        CheckpointManager.CheckpointActivated += OnCheckpointReached;
        CheckpointManager.Respawned += OnRespawned;
    }

    private void OnCheckpointReached(CheckpointActivatedInfo obj)
    {
        _brokenLastCheckpoint = _brokenNow;
    }

    private void OnRespawned()
    {
        _brokenNow = _brokenLastCheckpoint;
        gameObject.SetActive(!_brokenNow);
    }

    void OnDamaged()
    {
        _brokenNow = true;
        gameObject.SetActive(!_brokenNow);
    }
}
