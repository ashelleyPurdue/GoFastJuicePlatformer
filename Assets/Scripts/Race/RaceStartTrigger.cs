using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(IRaceManager))]
public class RaceStartTrigger : MonoBehaviour
{
    public RaceEndTrigger goal;
    public Transform model;

    private IRaceManager _raceManager;

    void Awake()
    {
        _raceManager = GetComponent<IRaceManager>();
    }

    void Update()
    {
        // Hide the model if the race is in progress
        model.localScale = _raceManager.IsRaceInProgress
            ? Vector3.zero
            : Vector3.one;

        // TODO: Display this in some kind of GUI instead of debug-displaying
        // it.
        if (_raceManager.IsRaceInProgress)
            DebugDisplay.PrintLine("Race time: " + _raceManager.RaceTime);
    }

    void FixedUpdate()
    {
        GetComponent<Collider>().enabled = !_raceManager.IsRaceInProgress;
    }

    void OnPlayerMotorCollisionStay(PlayerMotor player)
    {
        _raceManager.StartRace();
    }
}
