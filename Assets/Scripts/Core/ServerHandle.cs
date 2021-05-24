using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static Dictionaries;



public class ServerHandle
{
    public static void WelcomeReceived(int fromClient, Packet packet)
    {
        var clientIdCheck = packet.ReadInt();
/*        ServerConsoleWriter.WriteLine(
            $"Connection with {Server.Clients[fromClient].TcpInstance.Socket.Client.RemoteEndPoint} has been successfully established and is now indexed as : {fromClient}.");*/
    }
    public static void PlayerMovement(int fromClient, Packet packet)
    {
        var matchId = packet.ReadInt();
        var position = packet.ReadVector3();
        var rotation = packet.ReadQuaternion();
        DataManager.InProgressMatches[matchId].InGameManager.MovePlayer(fromClient, position, rotation);
    }
    public static void LoginInformation(int fromClient, Packet packet)
    {
        var id = packet.ReadInt();
        var username = packet.ReadString();
        var password = packet.ReadString();
        Server.Clients[fromClient].RequestLogin(username, password);
    }
    public static void SendInvitationServer(int fromClient, Packet packet)
    {
        var id = packet.ReadInt();
        var username = packet.ReadString(); //Who SENT THE INVITATION
        var toUserName = packet.ReadString(); // Who is THE INVITE FOR
        var sendTo = DataManager.GetClientData(toUserName);
        if(sendTo == null) return;
        ServerSend.SendInvite(fromClient, username, toUserName, sendTo.PlayerId);
    }
    public static void SendInviteAnswer(int fromClient, Packet packet)
    {
        var answer = packet.ReadBool();
        var sendTo = packet.ReadString();
        var whoSent = packet.ReadString();
        var send = DataManager.GetClientData(sendTo);
        if(send == null) return;
        ServerSend.SendInviteAnswer(fromClient, answer, whoSent, send.PlayerId);
        if (!answer) return;
        var member = DataManager.GetClientData(fromClient);
        if(member == null) return;
        var partyMembers = new List<ClientData>
        {
            member,
            send,
        };
        if (partyMembers[0].InParty)
        {
            //Player is already in party
            Parties.AddToExistingParty(partyMembers[0].PartyID, partyMembers[0]);
            ServerSend.RemoveLfgButton(partyMembers[0].PlayerId);
            send.PartyID = send.PartyID;
            send.InParty = true;
        }
        else
        {
            var partyId = Parties.AddParty(partyMembers);
            send.InParty = true;
            member.InParty = true;
            send.PartyID = partyId;
            member.PartyID = partyId;
            ServerSend.RemoveLfgButton(member.PlayerId);
        }
    }
    public static void DealDamageToPlayer(int fromClient, Packet packet)
    {
        var matchId = packet.ReadInt();
        var amount = packet.ReadFloat();
        var toWhom = packet.ReadInt();
        DataManager.InProgressMatches[matchId].InGameManager.TakeDamage(toWhom, amount);
    }
    public static void OnPlayerAddToMatchMaking(int fromClient, Packet packet)
    {
        if(HandleMatchMaking.IsPlayerInQueue(fromClient)) return;
        var mapId = packet.ReadInt();
        switch (mapId)
        {
            case 0: //Tutorial
                Debug.Log("Tutorial Request");
                ServerSend.BeginTutorial(fromClient);
                break;
            case 1:
            case 2:
                var isClientInParty = packet.ReadBool();
                var getData = DataManager.GetClientData(fromClient);
                if(getData == null) return;
                if (isClientInParty) //Player In Party
                {
                    var partyID = getData.PartyID;
                    var partyMembers = Parties.GetParty(partyID);
                    foreach (var member in partyMembers)
                    {
                        HandleMatchMaking.AddToQueue(member);
                        ServerSend.MatchMakingState(member.PlayerId);
                    }
                }
                else //Player Is NOT IN PARTY
                {
                    HandleMatchMaking.AddToQueue(getData);
                    ServerSend.MatchMakingState(fromClient);
                }
                break;
            default:
                Debug.LogError($"Add To Match Making Method was called by {fromClient} with an invalid MAP ID {mapId} \n His request has been ignored.");
                break;
        }
   
    }
    public static void MatchAnswer(int fromClient, Packet packet)
    {
       Debug.LogWarning($"Deprecated Method SET MATCH ANSWER sent from -> {fromClient} client");
    }

    public static void OnPlayerRemoveFromMatchMaking(int fromClient, Packet packet)
    {
        var isClientInParty = packet.ReadBool();
        var getData = DataManager.GetClientData(fromClient);
        if(getData == null) return;
        if (isClientInParty) //Player In Party
        {
            var partyID = getData.PartyID;
            var partyMembers = Parties.GetParty(partyID);
            foreach (var member in partyMembers)
            {
                HandleMatchMaking.RemoveFromQueue(member);
                ServerSend.MatchMakingState(member.PlayerId);
            }
        }
        else //Player Is NOT IN PARTY
        {
            HandleMatchMaking.RemoveFromQueue(getData);
            ServerSend.MatchMakingState(fromClient);
        }
    }

    public static void OnPlayerPickUpdate(int fromclient, Packet packet)
    {
        
        var matchId = packet.ReadInt();
        var team = packet.ReadInt();
        var whatPick = packet.ReadInt();
        Debug.Log("Received Package");
        DataManager.InProgressMatches[matchId].Lobby.UpdatePick(fromclient,team,whatPick);
    }

    public static void OnPlayerSceneIsLoaded(int fromclient, Packet packet)
    {
        var matchId = packet.ReadInt();
        var team = packet.ReadInt();
        DataManager.InProgressMatches[matchId].SetSceneLoaded(fromclient, team);
    }
    


    public static void HandleGamePlayConflictZone(int fromclient, Packet packet)
    {
        var matchId = packet.ReadInt();
        var zone = packet.ReadInt();
        DataManager.InProgressMatches[matchId].InGameManager.SetZone(fromclient, zone);
    }

    public static void OnPlayerAnimationUpdate(int fromclient, Packet packet)
    {
        var matchId = packet.ReadInt();
        var animationToPlay = packet.ReadInt();
        foreach (var player in DataManager.InProgressMatches[matchId].AllPlayers.Values.Where(player => player.data.PlayerId != fromclient))
        {
            ServerSend.SendAnimation(fromclient, animationToPlay,player.data.PlayerId);
        }
    }


    public static void OnPlayerPickConfirmation(int fromclient, Packet packet)
    {
        var matchID = packet.ReadInt();
        var team = packet.ReadInt();
        DataManager.InProgressMatches[matchID].Lobby.ConfirmPick(fromclient, team);
    }

    public static void OnPlayerEnterNewZone(int fromclient, Packet packet)
    {
        var matchId = packet.ReadInt();
        var zone = packet.ReadInt();
        DataManager.InProgressMatches[matchId].InGameManager.SetZone(fromclient,zone);
    }

    public static void OnSpawnRequest(int fromclient, Packet packet)
    {
        var matchID = packet.ReadInt();
        DataManager.InProgressMatches[matchID].InGameManager.SendSpawns(fromclient);
    }

    public static void OnDamageDeal(int fromclient, Packet packet)
    {
        Debug.Log("Begin damage process");
        var matchId = packet.ReadInt();
        var dealTo = packet.ReadInt();
        var dmg = packet.ReadFloat();
        DataManager.InProgressMatches[matchId].InGameManager.TakeDamage(dealTo, dmg);
    }

    public static void OnDamageDealToObject(int fromClient, Packet packet)
    {
        var matchId = packet.ReadInt();
        var ownerOfObject = packet.ReadInt();
        var amount = packet.ReadFloat();
        DataManager.InProgressMatches[matchId].InGameManager.DealDamageToObject(whosObject: ownerOfObject,
            whoDealsDamage: fromClient, amountOfDamage: amount);
    }

    public static void SpawnCastorUltiamte(int fromclient, Packet packet)
    {
        var matchId = packet.ReadInt();
        if(!isMatchPresent(matchId)) return;
        var match = DataManager.InProgressMatches[matchId];
        var position = packet.ReadVector3();
        var rotation = packet.ReadQuaternion();
        var whatTo = packet.ReadInt();
        var hp = packet.ReadFloat();
        match.InGameManager.SpawnUltimate(rotation, position, whatTo, fromclient, hp);
    }

    public static void RemoveTorretaFromGame(int fromclient, Packet packet)
    {
        var matchId = packet.ReadInt();
        if(!isMatchPresent(matchId)) return;
        var match = DataManager.InProgressMatches[matchId];
        match.InGameManager.RemoveUltimate(fromclient);
    }

    public static void MoveBullet(int fromclient, Packet packet)
    {
        var matchId = packet.ReadInt();
        if(!isMatchPresent(matchId)) return;
        var match = DataManager.InProgressMatches[matchId];
        var bulletId = packet.ReadInt();
        var pos = packet.ReadVector3();
        var rot = packet.ReadQuaternion();
        match.InGameManager.MoveBullet(fromclient, bulletId, pos, rot);
    }

    public static void DestroyBullet(int fromclient, Packet packet)
    {
        var matchId = packet.ReadInt();
        if(!isMatchPresent(matchId)) return;
        var match = DataManager.InProgressMatches[matchId];    
        var bulletId = packet.ReadInt();
        match.InGameManager.RemoveBullet(fromclient, bulletId);

    }

    public static void DealDamageToObject(int fromClient, Packet packet)
    {
        var matchId = packet.ReadInt();
        if(!isMatchPresent(matchId)) return;
        var match = DataManager.InProgressMatches[matchId];
        var owner = packet.ReadInt();
        var amount = packet.ReadFloat();
        match.InGameManager.DealDamageToObject(owner, fromClient, amount);
    }

    public static void ActivateBoss(int fromclient, Packet packet)
    {
        var matchId = packet.ReadInt();
        if(!isMatchPresent(matchId)) return;
        var match = DataManager.InProgressMatches[matchId];
        var bossId = packet.ReadInt();
        match.InGameManager.ActivateBoss(bossId);
    }

    public static void DealDamageToBoss(int fromclient, Packet packet)
    {
        var matchId = packet.ReadInt();
        if(!isMatchPresent(matchId)) return;
        var bossId = packet.ReadInt();
        var amount = packet.ReadFloat();
        DataManager.InProgressMatches[matchId].InGameManager.DealDamageToBoss(bossId, amount, fromclient);
        
    }

    private static bool isMatchPresent(int matchId)
    {
        return DataManager.InProgressMatches.ContainsKey(matchId);
    }

    public static void HealBossToFull(int fromclient, Packet packet)
    {
        var matchId = packet.ReadInt();
        if(!isMatchPresent(matchId)) return;
        var bossId = packet.ReadInt();
        DataManager.InProgressMatches[matchId].InGameManager.RestoreBossToFull(bossId);
    }

    public static void AttemptOrbActivation(int fromclient, Packet packet)
    {
        var matchId = packet.ReadInt();
        if(!isMatchPresent(matchId)) return;
        Debug.Log($"Attempting orb activation from {fromclient}");
        DataManager.InProgressMatches[matchId].InGameManager.AttemptOrbActivation();
    }

    public static void HealPlayer(int fromclient, Packet packet)
    {
        var matchId = packet.ReadInt();
        if(!isMatchPresent(matchId)) return;
        DataManager.InProgressMatches[matchId].InGameManager.HealPlayerToFull(fromclient);
    }
}
