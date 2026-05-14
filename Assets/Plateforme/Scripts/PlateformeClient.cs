using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using WanzyeeStudio;
/// <summary>
/// Classe qui permet de parler avec la page Web et la Plateforme
/// version 1.0.1
/// </summary>
public class PlateformeClient : BaseSingleton<PlateformeClient>
{
    public delegate void SoundChangeDelegate(bool state);
    public static SoundChangeDelegate SoundChange;

    public delegate void InitPlateformeAppDelegate();
    public static InitPlateformeAppDelegate InitPlateformeApp;

    [DllImport("__Internal")]
    public static extern void InitApp();

    [DllImport("__Internal")]
    public static extern void InitGame();

    [DllImport("__Internal")]
    public static extern void SaveHighscore(int score);

    [DllImport("__Internal")]
    public static extern void GetHighscores();

    [DllImport("__Internal")]
    public static extern void Close();

    [DllImport("__Internal")]
    public static extern void Restart();

    [DllImport("__Internal")]
    public static extern void FullscreenSwitch(bool fullscreenState = true);

    public static bool SoundMuteState = false;

    protected override void Awake()
    {
        base.Awake();
        autoDestroy = true;
        Debug.Log("PlateformeClientAwake");
    }

    /// <summary>
    /// On scene change we want to keep all saved states active as they were in last scene
    /// </summary>
    public void ExecuteSavedStates()
    {
        SoundChangeCall(Convert.ToInt32(SoundMuteState));
    }


    /// <summary>
    /// Called by the HTML template page in regards of muting state on the plateform
    /// </summary>
    /// <param name="state">muting state</param>
    public void SoundChangeCall(int state)
    {
        SoundMuteState = Convert.ToBoolean(state);
        SoundChange?.Invoke(SoundMuteState);
    }

    /// <summary>
    /// Called by the HTML template page when the game is loaded by the plateform
    /// </summary>
    public void InitPlateformeAppCall()
    {
        InitPlateformeApp?.Invoke();
    }
}
