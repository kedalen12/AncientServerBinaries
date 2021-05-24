using System;
using System.Linq;
using System.Threading.Tasks;
using Core;
using UnityEngine;

public class MatchLobby
{
    private readonly MatchN _match;
/*
    public int _scene = -1;
*/
    public MatchLobby(MatchN match)
    {
        _match = match;
    }
    public  void Begin() /*This is called once and tells the client to load the lobby scene*/
    {
        foreach (var player in _match.AllPlayers.Values)
        {
       
            player.data.currentState = ClientCurrentState.STATE_LOBBY;
            switch (player.Team)
            {
                case 1:
                    ServerSend.BeginLobby(player.data.PlayerId, _match.MatchID, 1,_match.Team1.Values.ToList(), _match.Team2.Values.ToList());
                    break;
                case 2:
                    ServerSend.BeginLobby(player.data.PlayerId, _match.MatchID, 2,_match.Team2.Values.ToList(),_match.Team1.Values.ToList());
                    break;
            }
        }
        Task.Run(EndLobby);
    }

    private void OnLobbyEnd()
    {
        var isNull = _match is null;
        if (isNull)
        {
            Dispose();
            EndLobby().Dispose();
        }
    }
    private async Task EndLobby()
    {
        await Task.Delay(30000);
        if (_match == null)
        {
            Dispose();
            EndLobby().Dispose();
        }
        if (_match != null)
        {
            var allPlayers = _match.AllPlayers.Values;
            var whoHasPicked = allPlayers.Count(player => player.pickState);
            if (whoHasPicked == 6)
            {
                _match.InGameManager = new InGameManager(Enums.MapType.AztecMap, _match);
                foreach (var player in allPlayers)
                {
                    ServerSend.EndLobby(player.data.PlayerId, 3);
                    player.data.currentState = ClientCurrentState.STATE_INMATCH;
                }
            }
            else
            {
                if (allPlayers.Count(player => player.Pick != -1) == 6 - whoHasPicked)
                {
                    foreach (var player in allPlayers)
                    {
                        player.pickState = true;
                    }
                    _match.InGameManager = new InGameManager(Enums.MapType.AztecMap, _match);
                    foreach (var player in allPlayers)
                    {
                        ServerSend.EndLobby(player.data.PlayerId, 3);
                        player.data.currentState = ClientCurrentState.STATE_IDLE;
                    }
                }
                else
                {
                    foreach (var player in allPlayers)
                    {
                        ServerSend.EndLobby(player.data.PlayerId, 1);
                        player.data.currentState = ClientCurrentState.STATE_IDLE;
                    }
                }
            }
        }
    }

    private void Dispose()
    {
        GC.Collect();  
        GC.WaitForPendingFinalizers();
    }
    public void UpdatePick(int who,int team ,int whatPick) /*This is called when a player presses a pick*/
    {
        if (whatPick == -1)
        {
            _match.AllPlayers[who].Pick = whatPick;
            foreach (var player in  _match.AllPlayers.Values)
            {
                ServerSend.UpdatePlayerPickLobby(player.data.PlayerId  ,who, whatPick);
            }
        }
        else if ( _match.AllPlayers.Values.Any(play => play.Pick == whatPick && play.Team == team))
        {
        }
        else
        {
            _match.AllPlayers[who].Pick = whatPick;
            _match.AllPlayers[who].SetPickStats(whatPick);
            _match.AllPlayers[who].pickState = true;
            foreach (var player in  _match.AllPlayers.Values)
            {
                ServerSend.UpdatePlayerPickLobby(player.data.PlayerId  ,who, whatPick);
            }
        }
    }
    public void ConfirmPick(int who, int team) /*This is called when a player attempts to confirm a pick*/
    {
        switch (team)
        {
            case 1:
                var whatPlayerT1 = _match.Team1[who];
                if (whatPlayerT1.pickState)
                {
                    return;
                }
                var whatPickT1 = whatPlayerT1.Pick;
                if (_match.Team1.Values.Where(player => player.data.PlayerId != who).Any(player => player.Pick == whatPickT1))
                {
                    Debug.Log("Found Pick");
                    return;
                }
                foreach (var teamMate in _match.Team1)
                {
                    ServerSend.ConfirmPick(sendTo: teamMate.Key ,who, whichPick: whatPickT1);
                }
                _match.Team1[who].pickState = true;
                _match.Team1[who].SetPickStats(whatPickT1);
                return;
            case 2:
                var whatPlayerT2 = _match.Team2[who];
                if (whatPlayerT2.pickState)
                {
                    return;
                }
                var whatPickT2 = whatPlayerT2.Pick;
                if (_match.Team2.Values.Where(player => player.data.PlayerId != who).Any(player => player.Pick == whatPickT2))
                {
                    return;
                }
                foreach (var teamMate in _match.Team2)
                {
                    ServerSend.ConfirmPick(sendTo: teamMate.Key ,who, whichPick: whatPickT2);
                }
                _match.Team2[who].pickState = true;
                _match.Team2[who].SetPickStats(whatPickT2);
                return;
            default:
                return;
        }
    }
    public void ForceEndLobby(Enums.LobbyEndReason reason, int id = -1)
    {
        switch (reason)
        {
            case Enums.LobbyEndReason.PlayerDidNotSelect:
                break;
            case Enums.LobbyEndReason.PlayerDisconnected:
                foreach (var player in _match.AllPlayers.Values.Where(player => player.data.PlayerId != id && player.data != null))
                {
                    ServerSend.EndLobby(player.data.PlayerId, 1);
                    player.data.currentState = ClientCurrentState.STATE_IDLE;
                }
                break;
            case Enums.LobbyEndReason.ServerEnded:
                break;
        }
        
        Dictionaries.DataManager.InProgressMatches.Remove(_match.MatchID);
        _match.Dispose();

    }
}