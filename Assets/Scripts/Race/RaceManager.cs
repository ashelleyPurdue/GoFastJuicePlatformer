using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void RaceStartedCallback(string raceId);
public delegate void RaceCanceledCallback(string raceId);
public delegate void RaceFinishedCallback(string raceId, float time);

public enum RaceState
{
    NotStarted,
    InProgress,
    Finished
}

/// <summary>
/// A service that manages races.
/// Only one race may be running at a time.
/// </summary>
public interface IRaceManager
{
    /// <summary>
    /// The ID of the race that was started last.
    /// Will be null if no race has been started yet during this playsession,
    /// or if the last race was canceled.
    /// </summary>
    string CurrentRaceId {get;}

    /// <summary>
    /// The current time in the race that was started last.
    /// Will be 0 if no race has been started yet during this playsession.
    /// 
    /// When a new race is started, this value will be reset to 0.
    /// 
    /// While a race is in progress, this value constantly ticks up on every
    /// FixedUpdate frame.  It will stop increasing when the race is finished
    /// or canceled.
    /// 
    /// When a race is canceled, this value will reset to 0.
    /// 
    /// When a race is finished, this value will "freeze" and remain at what
    /// its current value until a new race is started.
    /// </summary>
    float CurrentRaceTime {get;}

    RaceState CurrentState {get;}
    
    event RaceStartedCallback RaceStarted;
    event RaceCanceledCallback RaceCanceled;
    event RaceFinishedCallback RaceFinished;

    /// <summary>
    /// Starts a new race.  Throws an error if there is already a race in progress.
    /// 
    /// Sets <see cref="CurrentRaceId"/> to the ID of the race that was started.
    /// Sets <see cref="CurrentState"/> to <see cref="RaceState.InProgress"/>.
    /// Resets <see cref="CurrentRaceTime"/> to 0.
    /// Fires the <see cref="RaceStarted"/> event with the ID of the race that
    /// was started.
    /// </summary>
    void StartRace(string raceId);

    /// <summary>
    /// Cancels the race that is currently in progress.  Throws an error if
    /// there is no race in progress.
    /// Sets <see cref="CurrentState"/> to <see cref="RaceState.NotStarted"/>.
    /// Resets <see cref="CurrentRaceTime"/> to 0.
    /// Fires the <see cref="RaceCanceled"/> event.
    /// Throws an error if there is no race in progress.
    /// </summary>
    void CancelRace();

    /// <summary>
    /// Finishes the race that is currently in progress.  Throws an error if
    /// there is no race in progress.
    /// Sets <see cref="CurrentState"/> to <see cref="RaceState.Finished"/>.
    /// Does not change <see cref="CurrentRaceTime"/>.
    /// Fires the <see cref="RaceCanceled"/> event.
    /// </summary>
    void FinishRace();
}

public class RaceManager : ScriptableObject, IRaceManager
{
    public RaceState CurrentState {get; private set;}
    public string CurrentRaceId {get; private set;} = null;

    public float CurrentRaceTime
    {
        get
        {
            switch (CurrentState)
            {
                case RaceState.NotStarted: return 0;
                case RaceState.InProgress: return (Time.time - _lastRaceStartTime);
                case RaceState.Finished: return (_lastRaceEndTime - _lastRaceStartTime);

                default: throw new Exception("This will never happen.");
            }
        }
    }

    public event RaceStartedCallback RaceStarted;
    public event RaceCanceledCallback RaceCanceled;
    public event RaceFinishedCallback RaceFinished;

    private float _lastRaceStartTime = 0;
    private float _lastRaceEndTime = 0;

    public void StartRace(string raceId)
    {
        if (CurrentState == RaceState.InProgress)
        {
            throw new Exception("Cannot start a race while one is already in progress.");
        }

        CurrentRaceId = raceId;
        _lastRaceStartTime = Time.time;
        _lastRaceEndTime = 0;

        RaceStarted?.Invoke(raceId);
    }

    public void CancelRace()
    {
        if (CurrentState != RaceState.InProgress)
        {
            throw new Exception("Cannot cancel a race if none is in progress.");
        }

        CurrentState = RaceState.NotStarted;
        _lastRaceStartTime = 0;
        _lastRaceEndTime = 0;

        var raceId = CurrentRaceId;
        CurrentRaceId = null;

        RaceCanceled?.Invoke(raceId);
    }

    public void FinishRace()
    {
        if (CurrentState != RaceState.InProgress)
        {
            throw new Exception("Cannot finish a race if none is in progress.");
        }
        
        CurrentState = RaceState.Finished;
        _lastRaceEndTime = Time.time;
        RaceFinished?.Invoke(CurrentRaceId, CurrentRaceTime);
    }
}
