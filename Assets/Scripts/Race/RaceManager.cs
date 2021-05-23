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
    /// Will be null if no race has been started yet during this playsession
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
    /// Sets <see cref="CurrentState"/> to <see cref="RaceState.InProgress"/>.
    /// Resets <see cref="CurrentRaceTime"/> to 0.
    /// Fires the <see cref="RaceStarted"/> event.
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
    public string CurrentRaceId {get; private set;}

    public float CurrentRaceTime => throw new System.NotImplementedException();

    public RaceState CurrentState => throw new System.NotImplementedException();

    public event RaceStartedCallback RaceStarted;
    public event RaceCanceledCallback RaceCanceled;
    public event RaceFinishedCallback RaceFinished;

    public void CancelRace()
    {
        throw new System.NotImplementedException();
    }

    public void FinishRace()
    {
        throw new System.NotImplementedException();
    }

    public void StartRace(string raceId)
    {
        throw new System.NotImplementedException();
    }
}
