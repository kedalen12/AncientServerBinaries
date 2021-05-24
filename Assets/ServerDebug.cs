using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ServerDebug : MonoBehaviour
{
    public static ServerDebug Instance;
    private void Awake()
    {
        Instance = this;
    }
    
    public void GOOOO(int _fromClient)
    {
        StartCoroutine(goInisdeMe(_fromClient));
    }

    public IEnumerator goInisdeMe(int _fromClient)
    {
        yield return new WaitForSecondsRealtime(3f);
        //ServerSend.StartMatch(_fromClient, 2);
    }
}

