﻿using System.Collections;
using KartGame.Track;
using UnityEngine;

public class ObjectiveCompleteLaps : Objective
{
    
    [Tooltip("How many laps should the player complete before the game is over?")]
    public int lapsToComplete;

    [Header("Notification")]
    [Tooltip("Start sending notification about remaining laps when this amount of laps is left")]
    public int notificationLapsRemainingThreshold = 1;


    
    public int currentLap { get; private set; }

    void Awake()
    {
        currentLap = 0;
        
        // set a title and description specific for this type of objective, if it hasn't one
        if (string.IsNullOrEmpty(title))
            title = $"Complete {lapsToComplete} {targetName}s";
        
    }

    IEnumerator Start()
    {
        TimeManager.OnSetTime(totalTimeInSecs, isTimed, gameMode);
        TimeDisplay.OnSetLaps(lapsToComplete);
        yield return new WaitForEndOfFrame();
        Register();
    }

    protected override void ReachCheckpoint(int remaining)
    {
        if (isCompleted)
            return;

        currentLap++;

        // Oneindig rijden: nooit CompleteObjective aanroepen.
        // Toon gewoon het huidige lapnummer als info.
        UpdateObjective(string.Empty, GetUpdatedCounterAmount(), string.Empty);
    }

    public override string GetUpdatedCounterAmount()
    {
        return "Lap " + currentLap;
    }
  
   
  
  

}
