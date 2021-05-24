using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
// ReSharper disable ForCanBeConvertedToForeach

public class HandleMatchMaking
{
    private static readonly int[] MapsArray = {1, 2, 3, 4, 5};
    public static bool Generating;
    private static List<ClientData> _matchQueue = new List<ClientData>(); //This list stores the PlayerDataHolder's of Clients looking for match
    //public List<Maps> availableMaps = new List<Maps>();
    public static void AddToQueue(ClientData clientData)
    {
        ServerConsoleWriter.WriteLine($"{clientData.Username} has joined the match Queue");
        if (clientData.MatchId != 0)
        {
            //This user was on a match so he should rejoin it :)
        }
        else
        {
            _matchQueue.Add(clientData);
        }
    }

    public static void RemoveFromQueue(ClientData clientData)
    {
        ServerConsoleWriter.WriteLine($"{clientData.Username} has left the match Queue");
        if(_matchQueue.Contains(clientData))
            _matchQueue.Remove(clientData);
    }


    public static void GenerateMatch([NotNull] List <ClientData> holdersList)
    {
        if (holdersList == null) throw new ArgumentNullException(nameof(holdersList));
            Generating = true;
            //Generate TEAMS
            var newMatch = new MatchN();
            var team1 = new List<ClientData>();
            var team2 = new List<ClientData>();
            for (var i = 0; i < holdersList.Count; i++)
            {
                if (team1.Contains(holdersList[i]) || team2.Contains(holdersList[i])) continue;
                if (holdersList[i].InParty)
                {
                    var partyMembers = Parties.GetParty(holdersList[i].PartyID);
                    var partyCount = partyMembers.Count;
                    if (team1.Count + partyCount <= 3)
                    {
                        team1.AddRange(partyMembers);
                    }
                    else
                    {
                        team2.AddRange(partyMembers);
                    }
                }
                else
                {
                    if (team1.Count + 1 <= 3)
                    {
                        team1.Add(holdersList[i]);
                    }
                    else
                    {
                        team2.Add(holdersList[i]);
                    }
                }
            }
            /*
             * Teams are generated so we set them
             */
            newMatch.SetAllPlayers(team1, team2);
            /*SEND THE POP UP TO EVERYONE IN THE MATCH :)
            -> THIS WILL SHOW THE UI IN THE CLIENTS
            -> AFTER 20 SECONDS IT WILL CHECK IF EVERYONE IN THE MATCH HAS ACCEPTED
            */
            newMatch.Lobby.Begin();
            Dictionaries.DataManager.InProgressMatches.Add(newMatch.MatchID, newMatch);
            Generating = false; //Finished Generating Match
    }
    public static List<ClientData> CheckIfMatchMakingIsPossible()
    {
        if (_matchQueue.Count < Constants.MatchSize) return null;
        var playersToAdd = new List<ClientData>();
        // ReSharper disable once InvertIf
        for (var i = 0; i < _matchQueue.Count; i++)
        {
            if(playersToAdd.Contains(_matchQueue[i])) continue;
            if (_matchQueue[i].InParty)
            {
                var partyMembers = Parties.GetParty(_matchQueue[i].PartyID);
                var partyCount = partyMembers.Count;
                if (playersToAdd.Count + partyCount > Constants.MatchSize) continue;
                playersToAdd.AddRange(partyMembers.Select(partyData => Dictionaries.DataManager.GetClientData(partyData.Username)));
            }
            else
            {
                playersToAdd.Add(_matchQueue[i]);
            }
        }
        var newMm = _matchQueue.Except(playersToAdd).ToList();
        _matchQueue = newMm;
        return playersToAdd;
    }

    public static bool IsPlayerInQueue(int playerId)
    {
        var getData = Dictionaries.DataManager.GetClientData(playerId);
        return getData != null && _matchQueue.Contains(getData);
    }
}
