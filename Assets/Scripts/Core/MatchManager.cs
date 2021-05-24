using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ReSharper disable once CheckNamespace
public class MatchManager : MonoBehaviour //This Script Is Attached to an empty object within THE MAP 1 of this is generated for each map spawn
{
    public static MatchManager Instance;
    /// <summary>
    /// Slow update is called once every 3f
    /// </summary>
    public event Action OnSlowUpdate;
    public event Action OnFastUpdate;
    private void Awake()
    {
        Application.quitting += DcEveryone;
        Instance = this;
        StartCoroutine(ExecuteSlowUpdate());
        StartCoroutine(ExecuteFastUpdate());
    }

    private IEnumerator ExecuteFastUpdate()
    {
        while (true)
        {
            yield return new WaitForSeconds(.5f);
            OnFastUpdate?.Invoke();
        }
    }

    private void DcEveryone()
    {
        foreach (var player in Server.Clients.Keys)
        {
            ServerSend.ExitGame(player);
        }
    }

    private IEnumerator ExecuteSlowUpdate()
    {
        while (true)
        {
            yield return new WaitForSeconds(3f);
            OnSlowUpdate?.Invoke();
        }
    }
    
}
