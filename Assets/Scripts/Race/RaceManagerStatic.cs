using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaceManagerStatic : MonoBehaviour, IRaceManager
{
    private static IRaceManager instance = new RaceManager();

    public bool IsRaceInProgress => instance.IsRaceInProgress;
    public float RaceTime => instance.RaceTime;
    
    public void StartRace() => instance.StartRace();
    public void ResetRace() => instance.ResetRace();

    public void FinishRace(string raceId) => instance.FinishRace(raceId);
    public float GetBestTime(string raceId) => instance.GetBestTime(raceId);
}

public class RaceManager : IRaceManager
{
    public bool IsRaceInProgress {get; private set;} = false;

    public float RaceTime
    {
        get => IsRaceInProgress
            ? (Time.fixedTime - _raceStartTime)
            : (_raceEndTime - _raceStartTime);
    }

    private float _raceStartTime = 0;
    private float _raceEndTime = 0;

    public void StartRace()
    {
        IsRaceInProgress = true;
        _raceStartTime = Time.fixedTime;
        _raceEndTime = 0;
    }

    public void ResetRace()
    {
        IsRaceInProgress = false;
        _raceStartTime = 0;
        _raceEndTime = 0;
    }

    public void FinishRace(string raceId)
    {
        IsRaceInProgress = false;
        _raceEndTime = Time.fixedTime;
    }

    public float GetBestTime(string raceId)
    {
        throw new NotImplementedException();
    }
}