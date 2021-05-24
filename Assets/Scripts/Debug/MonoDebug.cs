using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MonoDebug : MonoBehaviour
{
    public Text playerTexts;
    public static MonoDebug Instance;
    public List<string> toDraw = new List<string>();

    private void Awake()
    {
        Instance = this;
    }

    public void WriteNewUser(string user)
    {
        playerTexts.text = "";
        foreach (var sx in toDraw)
        {
            playerTexts.text += $"{sx}\n";
        }
        if (!toDraw.Contains(user))
        {
            toDraw.Add(user);
            playerTexts.text += user + "\n";
        }
    }
    public void DisconnectUser(string user)
    {
        Debug.Log($"Attempting to remove {user}");
        toDraw.Remove(user);
        playerTexts.text = "";
        foreach (var sx in toDraw)
        {
            playerTexts.text += $"{sx}\n";
        }
       
    }
    
}
