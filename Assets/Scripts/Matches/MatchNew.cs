using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class MatchN
{
    public InGameManager InGameManager;
    public Dictionary<int,InGamePlayerDataHolder> AllPlayers = new Dictionary<int,InGamePlayerDataHolder>(); 
    public Dictionary<int,InGamePlayerDataHolder> Team1 = new Dictionary<int,InGamePlayerDataHolder>(); 
    public Dictionary<int,InGamePlayerDataHolder> Team2 = new Dictionary<int,InGamePlayerDataHolder>(); 
    public MatchLobby Lobby;
    public List<int> disconnections = new List<int>();
    CancellationTokenSource source = new CancellationTokenSource();
    CancellationToken token;
    public int MatchID { get;private set; }
    public MatchN()
    {
        MatchID = GenerateMatchHash();
        Lobby = new MatchLobby(this);
        Server.OnPlayerConnection += OnPlayerConnect;
    }

    public bool OnPlayerConnect(int playerID, ClientData holder)
    {
        if (!AllPlayers.ContainsKey(playerID)) return false;
        disconnections.Remove(playerID);
        AllPlayers[playerID].data = holder;
        InGameManager.SpawnPlayerAfterDc(playerID, AllPlayers[playerID]);
        holder.MatchId = MatchID;
        return true;
    }
    private int GenerateMatchHash()
    {
        return GetHashCode() + GetHashCode();
    }

    
    public void Dispose()
    {
        foreach (var player in AllPlayers.Values)
        {
            player.data.MatchId = MatchID;        
            player.data.currentState = ClientCurrentState.STATE_IDLE;
        }
        InGameManager?.Dispose();
        AllPlayers = new Dictionary<int, InGamePlayerDataHolder>();
        /*Team1 = new Dictionary<int, InGamePlayerDataHolder>();
        Team2 = new Dictionary<int, InGamePlayerDataHolder>();*/
        Dictionaries.DataManager.InProgressMatches.Remove(MatchID);
        GC.Collect();  
        GC.WaitForPendingFinalizers();
        
    }

    public void GenerateTeamDictionary()
    {
        var t1 = AllPlayers.Values.Where(p => p.Team == 1).ToList();
        var t2 = AllPlayers.Values.Where(p => p.Team == 2).ToList();


        foreach (var player in t1)
        {
            Team1.Add(player.data.PlayerId, player);
        }
        foreach (var player in t2)
        {
            Team2.Add(player.data.PlayerId, player);
        }
        
    }

    public void SetSceneLoaded(int fromclient, int team)
    {

        AllPlayers[fromclient].connectionState = true;
       /* switch (team)
        {
            case 1:
                Team1[fromclient].connectionState = true;
                break;
            case 2:
                Team2[fromclient].connectionState = true;
                break;
        }*/
        if (AllPlayers.Values.Count(pla => pla.connectionState) < 6) return;
        //Everyone is ready so we send 
        InGameManager.AddDelay(SpawnAll, 10);
    }

    private void SpawnAll()
    {
        foreach (var player in AllPlayers.Values)
        {
            ServerSend.RemoveCanva(player.data.PlayerId);
            MatchManager.Instance.OnFastUpdate += InGameManager.SetZonesEffects;

        }    
    }


    public void SetAllPlayers(List<ClientData> team1, List<ClientData> team2)
    {
        foreach (var player in team1)
        {
            AllPlayers.Add(player.PlayerId,new InGamePlayerDataHolder(player, 1));
        }
        foreach (var player in team2)
        {
            AllPlayers.Add(player.PlayerId,new InGamePlayerDataHolder(player, 2));
        }
        GenerateTeamDictionary();
    }
}

public class UsablePlayerObject
{
    public int id;
    public Vector3 position;
    public Quaternion rotation;
    public float Health;

    public UsablePlayerObject(Vector3 vector3, Quaternion quaternion, float health)
    {
        position = vector3;
        rotation = quaternion;
        Health = health;
    }
}

public class Zone
{
    public int ZoneId;
    public bool canBeCaptured = false;
    public int CurrentValue = 0;
    public int CurrentTeam = 0;
    public int isBeneficialToTeam = 0;
    public int team1PlayerAmount = 0;
    public int team2PlayerAmount = 0;
    public bool isVirgin = true;

    public Zone(int zoneId, bool canBeCaptured, int isBeneficialToTeam = 0)
    {
        ZoneId = zoneId;
        this.canBeCaptured = canBeCaptured;
        this.isBeneficialToTeam = isBeneficialToTeam;
    }

    public void RemovePlayer(int team)
    {
        if (team == 1)
        {
            team1PlayerAmount--;
        }
        else
        {
            team2PlayerAmount--;
        }
    }
    public void AddPlayer(int team)
    {
        if (team == 1)
        {
            team1PlayerAmount++;
        }
        else
        {
            team2PlayerAmount++;
        }
    }
}