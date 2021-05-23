using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A service that manages races.
/// Only one race may be running at a time.
/// </summary>
public interface IRaceManager
{
    bool IsRaceInProgress {get;}

    /// <summary>
    /// Starts at 0.
    /// Increments every FixedUpdate frame while a race is in progress.
    /// </summary>
    /// <value></value>
    float RaceTime {get;}

    /// <summary>
    /// Starts a new race and resets <see cref="RaceTime"/> to 0.
    /// If another race is already in progress, that race will be canceled
    /// and the new one will take its place.
    /// </summary>
    void StartRace();

    /// <summary>
    /// Stops the current race(if one is in progress) and resets <see cref="RaceTime"/>
    /// to 0.
    /// </summary>
    void ResetRace();

    /// <summary>
    /// Stops the current race.  Throws an error if there is no race in progress.
    /// <see cref="RaceTime"/> will remain at its current value until either
    /// <see cref="StartRace"/> or <see cref="ResetRace"/> is called.
    /// 
    /// Updates the leaderboard for the race with the specified ID, if this time
    /// is better than the current best.
    /// </summary>
    void FinishRace(string raceId);

    /// <summary>
    /// Gets the current best time for the race with the given ID.
    /// </summary>
    float GetBestTime(string raceId);
}