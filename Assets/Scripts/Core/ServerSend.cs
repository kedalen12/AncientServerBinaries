using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ServerSend
{
    private static void SendTcpData(int _toClient, Packet _packet)
        {
            _packet.WriteLength();
            Server.Clients[_toClient].TcpInstance.SendData(_packet);
        }

        private static void SendUdpData(int _toClient, Packet _packet)
        {
            _packet.WriteLength();
            Server.Clients[_toClient].UdpInstance.SendData(_packet);
        }
        private static void SendTcpDataToList(Packet packet, List<int> sendToList)
        {
            packet.WriteLength();
            foreach (var x in sendToList.Where(x => Server.Clients.ContainsKey(x)))
            {
                Server.Clients[x].TcpInstance.SendData(packet);
            }
        }
        private static void SendTcpDataToListExcept(Packet packet, List<int> sendToList, int Except)
        {
            packet.WriteLength();
            foreach (var x in sendToList.Where(x => Server.Clients.ContainsKey(x) && x != Except))
            {
                Server.Clients[x].TcpInstance.SendData(packet);
            }
        }
        private static void SendTcpDataToAll(Packet _packet)
        {
            _packet.WriteLength();
            for (var i = 1; i <= Server.MaxPlayers; i++)
            {
                Server.Clients[i].TcpInstance.SendData(_packet);
            }
        }

        public static void PlayerDisconnected(int _playerId)
        {
            using (Packet _packet = new Packet((int)ServerPackets.playerDisconnected))
            {
                _packet.Write(_playerId);
                SendTcpDataToAll(_packet);
            }
        }

        private static void SendTCPDataToAll(int _exceptClient, Packet _packet)
        {
            _packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayers; i++)
            {
                if (i != _exceptClient)
                {
                    Server.Clients[i].TcpInstance.SendData(_packet);
                }
            }
        }
        

        #region Packets
        public static void Welcome(int _toClient, string _msg)
        {
            using (Packet _packet = new Packet((int)ServerPackets.welcome))
            {
                _packet.Write(_msg);
                _packet.Write(_toClient);
                
                SendTcpData(_toClient, _packet);
            }
        }
        public static void MatchFound(int _playerId, int _matchId) {
            using (Packet _packet = new Packet((int) ServerPackets.matchFound))
            {
                _packet.Write(_matchId);
                try {
                SendTcpData(_playerId, _packet);
                } catch {
                    ServerConsoleWriter.WriteLine($"Error sending {_packet} to {_playerId}");
                }
            }
        }
        public static void LoginResult(int _playerId, bool _result, string _error,int _dbID)
        {
            using (Packet _packet = new Packet((int)ServerPackets.handleLoginInfo))
            {
                _packet.Write(_playerId);
                _packet.Write(_result);
                _packet.Write(_error);
                _packet.Write(_dbID);
                SendTcpData(_playerId,_packet);
                // if(_result)
                // Server.clients[_playerId].SendIntoGame(_username);
            }
        }
        /*public static void JordiMatch(int toClient) {
            using (var packet = new Packet((int) ServerPackets.jordiMatch))
            {
                       packet.Write(1);
                        SendTcpData(toClient, packet);
                   }
       }*/

        public static void BeginLobby(int toClient,int matchId, int team,List<InGamePlayerDataHolder> myTeam, List<InGamePlayerDataHolder> enemyTeam, bool isRC = false)
        {
            using (var packet = new Packet((int) ServerPackets.lobbyconnect))
            {
                packet.Write(isRC);
                packet.Write(matchId);
                packet.Write(team);
                packet.Write(myTeam[0].data.Username);
                packet.Write(myTeam[0].data.PlayerId);
                packet.Write(myTeam[1].data.Username);
                packet.Write(myTeam[1].data.PlayerId);
                packet.Write(myTeam[2].data.Username);
                packet.Write(myTeam[2].data.PlayerId);
                packet.Write(enemyTeam[0].data.Username);
                packet.Write(enemyTeam[0].data.PlayerId);
                packet.Write(enemyTeam[1].data.Username);
                packet.Write(enemyTeam[1].data.PlayerId);
                packet.Write(enemyTeam[2].data.Username);
                packet.Write(enemyTeam[2].data.PlayerId);
                SendTcpData(toClient, packet);
            }
        }
        public static void SpawnPlayer(int toClient,int who,int whatPick,Vector3 playerMovement, int team)
        {
            using (var packet = new Packet((int)ServerPackets.spawnPlayer))
            {
                packet.Write(who);
                packet.Write(whatPick);
                packet.Write(playerMovement);
                packet.Write(team);
                SendTcpData(toClient, packet);
            }
        }
        public static void SendInvite(int fromID, string userName, string toUserName,int sendTo)
        {
            using (var packet = new Packet((int)ServerPackets.sendInviteServer))
            {
                packet.Write(fromID); //Who sent it
                packet.Write(userName); //Who sent it Username
                packet.Write(toUserName); //Name of who is this for
                SendTcpData(sendTo,packet);
            }
        }
        public static void SendInviteAnswer(int fromClient, bool answer, string name, int sendTo)
        {
            using (var packet = new Packet((int)ServerPackets.sendInviteAnswer))
            {
                packet.Write(fromClient);
                packet.Write(answer);
                packet.Write(name);
                SendTcpData(sendTo,packet);
            }
        }
        public static void MatchMakingState(int sendTo)
        {
            using (var packet = new Packet((int)ServerPackets.mmOk))
            {
                SendTcpData(sendTo,packet);
            }
        }
        public static void RemoveLfgButton(int sendTo)
        {
            using (var packet = new Packet((int)ServerPackets.removeLFButtons))
            {
                SendTcpData(sendTo,packet);
            }
        }
        #endregion

        public static void StartMatch(int sendTo, int scene)
        {
            if(!Server.Clients.ContainsKey(sendTo)) return;
            using (var packet = new Packet((int) ServerPackets.beginMatch))
            {
                packet.Write(scene);
                SendTcpData(sendTo, packet);
            }
        }

        public static void UpdatePlayerPickLobby(int sendTo, int whoToUpdate, int whichPick)
        {
            using (var packet = new Packet((int) ServerPackets.sendPickUpdate))
            {
               packet.Write(whoToUpdate);  
               packet.Write(whichPick);
               SendTcpData(sendTo,packet);
            }
            Debug.Log($"Sent package {(int) ServerPackets.sendPickUpdate} that equals {(ServerPackets.sendPickUpdate).ToString()}");
        }

        public static void EndLobby(int playerKey, int scene)
        {
            using (var packet = new Packet((int) ServerPackets.endLobby)){
                packet.Write(scene); 
                SendTcpData(playerKey, packet);
            }
        }
        
        public static void PlayerPosition(int playerID,int fromClient, Vector3 position, Quaternion rotation) //UPDATES THE PLAYER POSITION
        {
            using (var packet = new Packet((int) ServerPackets.playerPosition))
            {
                packet.Write(fromClient);
                packet.Write(position);
                packet.Write(rotation);
                SendUdpData(playerID, packet);
            }
        }

        public static void RemoveCanva(int playerId)
        {
            using (var packet = new Packet((int) ServerPackets.removeCanvas) )
            {
                packet.Write(playerId);
                SendTcpData(playerId, packet);
            }
        }

        public static void SendZoneValues(int zone1Value, int zone2Value, int zone3Value, int zone4Value, int zone7Value, int playerId)
        {
            using (var packet = new Packet((int) ServerPackets.sendPlayerConflictZone) )
            {
                packet.Write(zone1Value);
                packet.Write(zone2Value);
                packet.Write(zone3Value);
                packet.Write(zone4Value);
                packet.Write(zone7Value);
                SendUdpData(playerId, packet);
            }
        }
        
        public static void UpdatePlayerHealth(int playerId, float currentHp, int sendTo)
        {
            using (var packet = new Packet((int) ServerPackets.sendPlayerHealth) )
            {
                Debug.Log("Sending update health");
                packet.Write(playerId);
                packet.Write(currentHp);
                SendUdpData(sendTo, packet);
            }
        }

        public static void SetDeathOnPlayer(int playerId, float deathTimer, int deathCount, int sendTo)
        {
            using (var packet = new Packet((int) ServerPackets.playerDied) )
            {
                packet.Write(playerId);
                SendTcpData(sendTo, packet);
            }
        }

        public static void SetMatchEndResult(int teamWhoWon, int who)
        {
            using (var packet = new Packet((int) ServerPackets.setMatchEndResult))
            {
                packet.Write(teamWhoWon);
                SendTcpData(who,packet);
            }
        }

        public static void ForceSceneLoad(int scene, int toClient)
        {
            using (var packet = new Packet((int) ServerPackets.forceSceneLoad))
            {
                packet.Write(scene);
                SendTcpData(toClient,packet);
            }
        }

        public static void UpdateLocalPlayerTutorialStag(int toClient, int currentPlayerStage)
        {
            using (var packet = new Packet((int) ServerPackets.updateTutorialStage))
            {
                packet.Write(currentPlayerStage);
                SendTcpData(toClient,packet);
            }        
        }

        public static void BeginTutorial(int toClient)
        {
            using (var packet = new Packet((int) ServerPackets.beginTutorial))
            {
                SendTcpData(toClient,packet);
            }         
        }

        public static void UpdatePlayerDeathState(int playerId, int sendTo)
        {
            using (var packet = new Packet((int) ServerPackets.respawnPlayer))
            {
                packet.Write(playerId);
                SendTcpData(sendTo,packet);
            }                 
        }

        public static void SendAnimation(int fromclient, int animationToPlay,int sendTo)
        {
            using (var packet = new Packet((int) ServerPackets.updateAnimation))
            {
                packet.Write(fromclient);
                packet.Write(animationToPlay);
                SendUdpData(sendTo,packet);
            }       
        }
        public static void ConfirmPick(int sendTo, int who, int whichPick)
        {
            Debug.Log("Sending ConfirmPick");
            using (var packet = new Packet((int) ServerPackets.confirmPick))
            {
                packet.Write(who);
                packet.Write(whichPick);
                SendTcpData(sendTo, packet);
            }
        }

        public static void PlayerDisconnectedInGame(int sendTo, int whoDc)
        {
            using (var packet = new Packet((int) ServerPackets.removeFromMatch))
            {
                packet.Write(whoDc);
                SendTcpData(sendTo, packet);
            }
        }

        public static void ReconnectPlayer(int who, Vector3 currentPlayerPosition, int sendTo)
        {
            using (var packet = new Packet((int) ServerPackets.reconnectPlayer))
            {
                packet.Write(who);
                packet.Write(currentPlayerPosition);
                SendTcpData(sendTo, packet);
            }
        }

        public static void SendMatchId(int matchId, int sendTo)
        {
            using (var packet = new Packet((int) ServerPackets.sendMatchIdAfterDc))
            {
                packet.Write(matchId);
                SendTcpData(sendTo, packet);
            }        }

        public static void ExitGame(int player)
        {
            using (var packet = new Packet((int) ServerPackets.unLogIn))
            { 
                SendTcpData(player, packet);
            } 
        }

        public static void SendCastorUltimate(Vector3 position, Quaternion rotation, int ownerOf, int whatToSpawn,
            float health, int sendTo)
        {
            using (var packet = new Packet((int) ServerPackets.SpawnCastorUltimate))
            {
                packet.Write(position);
                packet.Write(rotation);
                packet.Write(ownerOf);
                packet.Write(whatToSpawn);
                packet.Write(health);
                SendTcpData(sendTo, packet);
            }
        }

        public static void DamageDealtToObject(int ownerOfTheObject, int whoDealsDamage, float amountOfDamage, int sendTo)
        {
            using (var packet = new Packet((int) ServerPackets.DamageDealtToObject))
            {
                packet.Write(ownerOfTheObject);
                packet.Write(amountOfDamage);
                SendTcpData(sendTo, packet);
            }
        }

        public static void RemoveBullet(int owner, int bulletId, int sendTo)
        {
            using (var packet = new Packet((int) ServerPackets.DestroyBullet))
            {
                packet.Write(owner);
                packet.Write(bulletId);
                SendTcpData(sendTo, packet);
            }
        }
        public static void MoveBullet(int owner,int bulletId, Vector3 pos, Quaternion rot, int sendTo)
        {
            using (var packet = new Packet((int) ServerPackets.MoveBullet))
            {
                packet.Write(owner);
                packet.Write(bulletId);
                packet.Write(pos);
                packet.Write(rot);
                SendUdpData(sendTo, packet);
            }
        }

        public static void RemoveCastorUltimate(int owner, int sendTo)
        {
            using (var packet = new Packet((int) ServerPackets.RemoveTorretaFromGame))
            {
                packet.Write(owner);
                SendTcpData(sendTo, packet);
            }        
        }

        public static void SendZoneCaptured(int team, int sendTo, int bossId)
        {
            //Zone ${zone.name} has been captured by team 1
            using (var packet = new Packet((int) ServerPackets.updateZoneState))
            {
                packet.Write(team);
                packet.Write(bossId);
                SendTcpData(sendTo, packet);
            }              
        }
        
        public static void UpdateBossStatus(string active, int bossId, int dataPlayerId)
        {
            using (var packet = new Packet((int) ServerPackets.ActivateBoss))
            {
                packet.Write(active);
                packet.Write(bossId);
                SendTcpData(dataPlayerId, packet);
            }
        }

        public static void DamageDealtToBoss(int bossId, int whoDeal,float newHealth, int sendTo, bool shouldDie)
        {
            using (var packet = new Packet((int) ServerPackets.DamageDealtToBoss))
            {
                
                packet.Write(bossId);
                packet.Write(whoDeal);
                packet.Write(newHealth);
                packet.Write(shouldDie);
                SendTcpData(sendTo, packet);
            }     
        }

        public static void RespawnBoss(int bossId, int team, int sendTo)
        {
            using (var packet = new Packet((int) ServerPackets.respawnBoss))
            {
                
                packet.Write(bossId);
                packet.Write(team); //Team who the boss will not attack until it is defeated again by the enemy
                Debug.Log($"BOSSDEBUG: {bossId} is being respawned");
                SendTcpData(sendTo, packet);
            }             
        }

        public static void UpdateBossHealth(int bossId, int dataPlayerId)
        {
            using (var packet = new Packet((int) ServerPackets.healBoss))
            {
                packet.Write(bossId);
                SendTcpData(dataPlayerId, packet);
            }
        }

        public static void ActivateOrb(int sendTo)
        {
            using (var packet = new Packet((int) ServerPackets.ActivateOrb))
            {
                SendTcpData(sendTo, packet);
            }        
        }

        public static void RespawnOrb(int sendTo)
        {
            using (var packet = new Packet((int) ServerPackets.RespawnOrb))
            {
                SendTcpData(sendTo, packet);
            }        
        }

        public static void FullEnergy(int sendTo)
        {
            using (var packet = new Packet((int) ServerPackets.RestorePlayer))
            {
                SendTcpData(sendTo, packet);
            }
            
        }

        public static void EndMatch(int team, int sendTo)
        {
            using (var packet = new Packet((int) ServerPackets.MatchMightHaveWinner))
            {
                packet.Write(team);
                SendTcpData(sendTo, packet);
            }
            
        }

        public static void BeginCountDownForMatchEnd(int p0, int dataPlayerId)
        {
            using (var packet = new Packet((int) ServerPackets.BeginCountDownForMatchEnd))
            {
                packet.Write(p0);
                SendTcpData(dataPlayerId, packet);
            }        
        }
}
