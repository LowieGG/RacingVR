﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{ 
    public bool IsFinite { get; private set; }
    public float TotalTime { get; private set; }
    public float TimeRemaining { get; private set; }
    public bool IsOver { get; private set; }

    public float CurrentLapTime { get; private set; }
    public float BestLapTime { get; private set; }

    private bool raceStarted;
    private bool firstLap = true;

    public static Action<float> OnAdjustTime;
    public static Action<int, bool, GameMode> OnSetTime;

    private void Awake()
    {
        IsFinite = false;
        TimeRemaining = TotalTime;
        BestLapTime = PlayerPrefs.GetFloat("BestLapTime", -1f);
    }


    void OnEnable()
    {
        OnAdjustTime += AdjustTime;
        OnSetTime += SetTime;
    }

    private void OnDisable()
    {
        OnAdjustTime -= AdjustTime;
        OnSetTime -= SetTime;
    }

    private void AdjustTime(float delta)
    {
        TimeRemaining += delta;
    }

    private void SetTime(int time, bool isFinite, GameMode gameMode)
    {
        TotalTime = time;
        IsFinite = isFinite;
        TimeRemaining = TotalTime;
    }

    void Update()
    {
        if (!raceStarted) return;
        
        CurrentLapTime += Time.deltaTime;
        if (IsFinite && !IsOver)
        {
            TimeRemaining -= Time.deltaTime;
            if (TimeRemaining <= 0)
            {
                TimeRemaining = 0;
                IsOver = true;
            }
        }
    }

    public void CompleteLap()
    {
        Debug.Log($"COMPLETE LAP | Current={CurrentLapTime} Best={BestLapTime}");

        // veiligheid
        if (CurrentLapTime <= 0.1f)
        {
            Debug.LogWarning("Lap ignored (too small)");
            return;
        }

        // best lap check
        if (BestLapTime < 0f || BestLapTime == 0f || CurrentLapTime < BestLapTime)
        {
            BestLapTime = CurrentLapTime;

            PlayerPrefs.SetFloat("BestLapTime", BestLapTime);
            PlayerPrefs.Save();

            Debug.Log("NEW BEST LAP: " + BestLapTime);
        }

        // reset lap
        CurrentLapTime = 0f;
    }

    public void StartRace()
    {
         raceStarted = true;
        CurrentLapTime = 0f;
        
        BestLapTime = 0f;
    }

    public void StopRace() {
        raceStarted = false;
    }
}

