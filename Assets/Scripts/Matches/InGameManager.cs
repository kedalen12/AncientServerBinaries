using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core;
using UnityEngine;
#pragma warning disable 4014

public class InGameManager
{
    
    public System.Action updateAction;
    public int Team1Death = 0;
    public int Team1Kills = 0;
    public int Team1Tower = 0;    
    public int Team2Death = 0;
    public int Team2Kills = 0;
    public int Team2Towers = 0;
    private Enums.MapType _myType;
    private readonly MatchN _myMatch;
    private List<Zone> _zones = new List<Zone>();

    public enum BossTarget
    {
        t1,
        t2,
        both

    }
    public class BossServerSide
    {
        private BossTarget Target = BossTarget.both;

        public const float MAXHealth = 2500;
        public float CurrentHealth = MAXHealth;
        public int lastHit;

        public int myId;
        private int amountOfPlayers;

        public BossServerSide(int id)
        {
            myId = id;
        }

        public bool isActive;
        public bool TakeDamage(float amount, int who)
        {
            lastHit = who;
            CurrentHealth -= amount;
            return CurrentHealth <= 0;
        }
        public string GetTarget()
        {
            return Target.ToString();
        }

        public void ActivateBoss()
        {
            if (!isActive)
            {
                isActive = true;
                amountOfPlayers++;
            }
            else
            {
                amountOfPlayers++;
            }
        }
        
        public void SetNewTarget(BossTarget target)
        {
            Target = target;
        }

        public async Task RespawnBoss(Action<int, int> invokeAfter)
        {
            await Task.Delay(19700);
            invokeAfter?.Invoke(myId, lastHit);
        }

        public bool CanHeal(int i)
        {
            amountOfPlayers--;
            if (amountOfPlayers != 0) return false;
            isActive = false;
            CurrentHealth = i;
            return true;

        }
    }
    public InGameManager(Enums.MapType myType, MatchN myMatch)
    {
        _myType = myType;
        _myMatch = myMatch;
        if (myType != Enums.MapType.AztecMap) return;
        var posT1 = 0;
        var posT2 = 0;
        foreach (var x in myMatch.AllPlayers.Values)
        {
            if (x.Team == 1)
            {
                CurrentPlayerPositions.Add(x.data.PlayerId, positions[posT1]);
                posT1++;
            }
            else
            {
                CurrentPlayerPositions.Add(x.data.PlayerId, positions[posT2 + 3]);
                posT2++;
                
            }
        }
        Task.Run(CallSpawnPlayers);
    }

    private Vector3[] positions = new[]
    {
        new Vector3(-70, -6, 6.3f),
        new Vector3(-70, -6, 9.3f),
        new Vector3 (-70, -6, 12.3f),
        new Vector3 (70, -6, 6.3f),
        new Vector3 (70, -6, 9.3f),
        new Vector3 (70, -6, 12.3f)
    };
    public Dictionary<int, Vector3> CurrentPlayerPositions = new Dictionary<int, Vector3>();
    public Dictionary<int, BossServerSide> Bosses = new Dictionary<int, BossServerSide>()
    {
        {0, new BossServerSide(0)},
        {1, new BossServerSide(1)},
        {2, new BossServerSide(2)},
        {3, new BossServerSide(3)}
    };
    private async Task CallSpawnPlayers()
    {
        await Task.Delay(10000);
        foreach (var id in _myMatch.AllPlayers.Values)
        {
            foreach (var oid in _myMatch.AllPlayers.Values)
            {
                if(oid.data.PlayerId == id.data.PlayerId) continue;
                ServerSend.SpawnPlayer(id.data.PlayerId, oid.data.PlayerId, oid.Pick,
                    CurrentPlayerPositions[oid.data.PlayerId], oid.Team);
            }
            ServerSend.SpawnPlayer(id.data.PlayerId, id.data.PlayerId, id.Pick,
                CurrentPlayerPositions[id.data.PlayerId], id.Team);
        }
    }

    #region Standard For AllMaps
    public void MovePlayer(int playerId, Vector3 newPos, Quaternion rotation)
    {
        if (newPos.y <= -9.3f) //Kill the player if he is too far underground
        {
            TakeDamage(playerId, 9999);
        }

        _myMatch.AllPlayers[playerId].currentPosition = newPos;
        foreach (var nextId in _myMatch.AllPlayers.Values.Select(player => player.data.PlayerId).Where(nextId => nextId != playerId))
        {
            ServerSend.PlayerPosition(nextId, playerId, newPos, rotation);
        }
    }
    public void TakeDamage(int playerId,float value)
    {
        Debug.Log($"Second step damage process {playerId}, {value}");
        var player = _myMatch.AllPlayers[playerId];
        TakeDamage(player, value);
    }
    private void UpdateHealth(int playerId, float currentHealth)
    {
        foreach (var player in _myMatch.AllPlayers.Values)
        {
            ServerSend.UpdatePlayerHealth(playerId, currentHealth, player.data.PlayerId);
        }
    }
    private void Kill(int playerId)
    {
        
        foreach (var player in _myMatch.AllPlayers.Values)
        {
            ServerSend.SetDeathOnPlayer(playerId, 5, 1, player.data.PlayerId);
        }
    }
    #endregion
    #region AztecMap
    public void SetZone(int playerId, int zone)
    {
        var player = _myMatch.AllPlayers[playerId];
        player.CurrentZone = zone;
    }

    public void SetZonesEffects()
    {
        foreach (var playerInZone6 in _myMatch.AllPlayers.Values.Where(w => w.CurrentZone == 6))
        {
            switch (playerInZone6.Team)
            {
                case 1: 
                    TakeDamage(playerInZone6, playerInZone6.maxHealth * .15f);
                    break;
                case 2:
                    RestoreHealth(playerInZone6, playerInZone6.maxHealth * .15f);
                    break;
            }
        }
        foreach (var playerInZone5 in _myMatch.AllPlayers.Values.Where(w => w.CurrentZone == 5))
        {

            switch (playerInZone5.Team)
            {
                case 1: 
                    RestoreHealth(playerInZone5, playerInZone5.maxHealth * .15f);
                    break;
                case 2:
                    TakeDamage(playerInZone5, playerInZone5.maxHealth * .15f);
                    break;
            }
        }
    }

    private void RestoreHealth(InGamePlayerDataHolder player, float value)
    {
        if((int)player.health == (int)player.maxHealth) return;
        player.health += value;
        if (player.health > player.maxHealth)
        {
            player.health = player.maxHealth;
        }
        UpdateHealth(player.data.PlayerId, player.health);
    }

    private void TakeDamage(InGamePlayerDataHolder player, float value)
    {
        if(player.isDead) return;
        var toApply = value * player.defenceMp;
        player.health -= toApply;
        if (player.health <= 0)
        {

            if (!player.isDead)
            {
#pragma warning disable 4014
                RespawnPlayer(player);
#pragma warning restore 4014
                Kill(player.data.PlayerId);
                player.isDead = true;
            }
            player.health = 0;
        }
        UpdateHealth(player.data.PlayerId, player.health);
    }
    
    public void OnZoneConquered()
    {
        if (Bosses.Count(t => t.Value.GetTarget() == "t1") >= 3)
        {
            isDown = false;
            foreach (var playerData in _myMatch.AllPlayers.Values)
            {
                ServerSend.BeginCountDownForMatchEnd(1,playerData.data.PlayerId);
            }

            MatchManager.Instance.OnFastUpdate += CheckForWinnEnd;
            AddDelay(CheckForWinner, 60);
        } else if (Bosses.Count(t => t.Value.GetTarget() == "t2") >= 3)
        { 
            isDown = false;
            foreach (var playerData in _myMatch.AllPlayers.Values)
            {
                ServerSend.BeginCountDownForMatchEnd(2,playerData.data.PlayerId);
            }     
            MatchManager.Instance.OnFastUpdate += CheckForWinnEnd;
            AddDelay(CheckForWinner, 60);
        }
    }
    
    /// <summary>
    /// Adds a delay to a method
    /// </summary>
    /// <param name="action">Action to execute in X seconds</param>
    /// <param name="delay">Delay given in seconds</param>
    public async Task AddDelay(Action action, int delay)
    {
        await Task.Delay(delay * 1000);
        action.Invoke();
    }

    private bool isDown = false;
    private void CheckForWinnEnd()
    {

            if (Bosses.Count(t => t.Value.GetTarget() == "t1") >= 4)
            {
                foreach (var playerData in _myMatch.AllPlayers.Values)
                {
                    ServerSend.EndMatch(1,playerData.data.PlayerId);
                }
                MatchManager.Instance.OnFastUpdate -= CheckForWinnEnd;
                isDown = true;
            } else if (Bosses.Count(t => t.Value.GetTarget() == "t2") >= 4)
            { 
                foreach (var playerData in _myMatch.AllPlayers.Values)
                {
                    ServerSend.EndMatch(2,playerData.data.PlayerId);
                }    
                MatchManager.Instance.OnFastUpdate -= CheckForWinnEnd;
                isDown = true;
            }
            else if (Bosses.Count(t => t.Value.GetTarget() == "t1") == 2 &&
                     Bosses.Count(t => t.Value.GetTarget() == "t2") == 2)
            {
                foreach (var playerData in _myMatch.AllPlayers.Values)
                {
                    ServerSend.EndMatch(0, playerData.data.PlayerId);
                }

                MatchManager.Instance.OnFastUpdate -= CheckForWinnEnd;
                isDown = true;
            }
    }
    private void CheckForWinner()
    {
        /*
         *  Here we will check if 3 or more zones have t1 as target or t2
         */

        if (!isDown)
        {
            if (Bosses.Count(t => t.Value.GetTarget() == "t1") >= 3)
            {
                foreach (var playerData in _myMatch.AllPlayers.Values)
                {
                    ServerSend.EndMatch(1, playerData.data.PlayerId);
                }
            }
            else if (Bosses.Count(t => t.Value.GetTarget() == "t2") >= 3)
            {
                foreach (var playerData in _myMatch.AllPlayers.Values)
                {
                    ServerSend.EndMatch(2, playerData.data.PlayerId);
                }
            }
            else
            {
                foreach (var playerData in _myMatch.AllPlayers.Values)
                {
                    ServerSend.EndMatch(0, playerData.data.PlayerId);
                }
            }
        }
    }
    public void Dispose()
    {
        MatchManager.Instance.OnFastUpdate -= SetZonesEffects;
        _myMatch.InGameManager = null;
        GC.Collect();  
        GC.WaitForPendingFinalizers();
    }
    async Task RespawnPlayer(InGamePlayerDataHolder player)
    {
        await Task.Delay(5000);
        player.isDead = false;
        SendRestore(player.data.PlayerId);
    }
    private void SendRestore(int playerId)
    {
        _myMatch.AllPlayers[playerId].health = _myMatch.AllPlayers[playerId].maxHealth;
        foreach (var player in _myMatch.AllPlayers.Values)
        {
            ServerSend.UpdatePlayerDeathState(playerId, player.data.PlayerId);
        }
            
    }
    #endregion

    public void RemovePlayerFromGame(int playerId)
    {
        _myMatch.disconnections.Add(playerId);
        if (_myMatch.disconnections.Count >= 6)
        {
            _myMatch.Dispose();   
        }
        else
        {
            foreach (var player in _myMatch.AllPlayers.Values)
            {
                if (player.data.PlayerId == playerId) continue;
                ServerSend.PlayerDisconnectedInGame(player.data.PlayerId, playerId);
            }
        }
    }

    public void SpawnPlayerAfterDc(int playerID, InGamePlayerDataHolder allPlayer)
    {
        switch (allPlayer.Team)
        {
            case 1:
                ServerSend.BeginLobby(playerID, _myMatch.MatchID, 1,_myMatch.Team1.Values.ToList(), _myMatch.Team2.Values.ToList(), true);
                break;
            case 2:
                ServerSend.BeginLobby(playerID, _myMatch.MatchID, 2,_myMatch.Team2.Values.ToList(), _myMatch.Team1.Values.ToList(), true);
                break;
        }


    }

    public void SendSpawns(int fromclient)
    {
        foreach (var player in _myMatch.AllPlayers.Values)
        {
            var id = player.data.PlayerId;
            if(_myMatch.disconnections.Contains(id)) continue;
            ServerSend.SpawnPlayer(fromclient, id,player.Pick ,player.currentPosition, player.Team);
        }
        foreach (var inGamePlayer in _myMatch.AllPlayers.Values)
        {
            if(inGamePlayer.data.PlayerId == fromclient) continue;
            ServerSend.ReconnectPlayer(fromclient, CurrentPlayerPositions[fromclient], inGamePlayer.data.PlayerId);
        }
        ServerSend.RemoveCanva(fromclient);
    }

    public void DealDamageToObject(int whosObject, int whoDealsDamage, float amountOfDamage)
    {
        foreach (var plx in _myMatch.AllPlayers.Values)
        {
            ServerSend.DamageDealtToObject(whosObject, whoDealsDamage, amountOfDamage, plx.data.PlayerId);
        } 
    }

    private UsablePlayerObject GetObjectFromPlayer(int whosObject)
    {
        return _myMatch.AllPlayers[whosObject].CurrentObject;
    }

    public void SpawnUltimate(Quaternion rotation, Vector3 position, int whatTo, int owner, float health)

    {
        _myMatch.AllPlayers[owner].CurrentObject = new UsablePlayerObject(position, rotation, health);
        foreach (var playEr in _myMatch.AllPlayers.Values)
        {
            if(owner == playEr.data.PlayerId) continue;
            ServerSend.SendCastorUltimate(position, rotation, owner, whatTo, health, playEr.data.PlayerId);
        }
    }

    public void OnRespawnRequest(int bossId, int lastHit)
    {
       var team = _myMatch.AllPlayers[lastHit].Team;
       Bosses[bossId].CurrentHealth = 2500;
        foreach (var playEr in _myMatch.AllPlayers.Values)
        {
            ServerSend.RespawnBoss(bossId, team, sendTo: playEr.data.PlayerId);
        }
        Debug.Log($"BOSS DEBUG: Boss {bossId} has now been restored");
    }
    public void DealDamageToBoss(int bossId, float damageAmount, int whoDeals)
    {
        var boss  = Bosses[bossId];
        var shouldDie = boss.TakeDamage(damageAmount, whoDeals);
        if (shouldDie)
        {
#pragma warning disable 4014
            Debug.Log($"BOSS DEBUG: Boss {bossId} has died and will respawn in 19700ms");
            boss.RespawnBoss(OnRespawnRequest);
            BossDeath(bossId, whoDeals);
            if (bossId == 0)
            {
                if (_myMatch.AllPlayers[whoDeals].Team == 1)
                {
                    foreach (var inGamePlayerDataHolder in _myMatch.Team1.Values)
                    {
                        inGamePlayerDataHolder.AddDefence();
                    }

                    foreach (var inGamePlayerDataHolder in _myMatch.Team2.Values)
                    {
                        inGamePlayerDataHolder.ResetDefence();
                    }
                }
                else
                {
                    foreach (var inGamePlayerDataHolder in _myMatch.Team2.Values)
                    {
                        inGamePlayerDataHolder.AddDefence();
                    }

                    foreach (var inGamePlayerDataHolder in _myMatch.Team1.Values)
                    {
                        inGamePlayerDataHolder.ResetDefence();
                    }
                }
            }
#pragma warning restore 4014
        }
        foreach (var playEr in _myMatch.AllPlayers.Values)
        {
            ServerSend.DamageDealtToBoss(bossId, whoDeals,boss.CurrentHealth, sendTo: playEr.data.PlayerId, shouldDie);
        }
    }

    public void BossDeath(int bossId, int lastHitId)
    {
        if (_myMatch.Team1.ContainsKey(lastHitId))
        {
            Bosses[bossId].SetNewTarget(BossTarget.t2);
            NotifyZoneCapture(1, bossId);
        }
        else
        {
            Bosses[bossId].SetNewTarget(BossTarget.t1);
            NotifyZoneCapture(2, bossId);
        }
        OnZoneConquered();
    }

    private void NotifyZoneCapture(int team, int bossId)
    {
        foreach (var playEr in _myMatch.AllPlayers.Values)
        {
            ServerSend.SendZoneCaptured(team: team, sendTo: playEr.data.PlayerId, bossId);
        }
    }

    public void RemoveUltimate(int owner)
    {
        _myMatch.AllPlayers[owner].CurrentObject = null;
        foreach (var playEr in _myMatch.AllPlayers.Values)
        {
            if(owner == playEr.data.PlayerId) continue;
            ServerSend.RemoveCastorUltimate(owner, playEr.data.PlayerId);
        }
    }

    public void MoveBullet(int owner, int bulletId, Vector3 pos, Quaternion rot)
    {
        foreach (var playEr in _myMatch.AllPlayers.Values)
        {
            if(owner == playEr.data.PlayerId) continue;
            ServerSend.MoveBullet(owner, bulletId, pos, rot, playEr.data.PlayerId);
        }    
    }

    public void RemoveBullet(int owner, int bulletId)
    {
        foreach (var playEr in _myMatch.AllPlayers.Values)
        {
            if(owner == playEr.data.PlayerId) continue;
            ServerSend.RemoveBullet(owner, bulletId, playEr.data.PlayerId);
        }        
    }

    public void ActivateBoss(int bossId)
    {
        foreach (var player in _myMatch.AllPlayers.Values)
        {
            ServerSend.UpdateBossStatus("active", bossId: bossId,player.data.PlayerId);
        }
    }

    public void RestoreBossToFull(int bossId)
    {
        var boss = Bosses[bossId];
        if (!(boss.CurrentHealth < 2500)) return;
        if (!boss.CanHeal(2500)) return;
        foreach (var player in _myMatch.AllPlayers.Values)
        {
            ServerSend.UpdateBossHealth(bossId,player.data.PlayerId);
        }


    }

    private bool canActivateOrb = true;
    private bool orbAwaitingRespawn;
    public void AttemptOrbActivation()
    {
        if(!canActivateOrb) return;
        if(orbAwaitingRespawn) return;
        orbAwaitingRespawn = true;
        canActivateOrb = false;
        foreach (var inGamePlayerDataHolder in _myMatch.AllPlayers.Values)
        {
            ServerSend.ActivateOrb(inGamePlayerDataHolder.data.PlayerId);
        }
        Task.Run(RespawnOrb);
    }

    private void OnOrbRespawn()
    {
        canActivateOrb = true;
        orbAwaitingRespawn = false;
        foreach (var inGamePlayerDataHolder in _myMatch.AllPlayers.Values)
        {
            ServerSend.RespawnOrb(inGamePlayerDataHolder.data.PlayerId);
        }
    }
    private async Task RespawnOrb()
    {
        await Task.Delay(5000);
        OnOrbRespawn();
    }

    public void HealPlayerToFull(int fromclient)
    {
        _myMatch.AllPlayers[fromclient].health = _myMatch.AllPlayers[fromclient].maxHealth;
        ServerSend.FullEnergy(fromclient);
        foreach (var player in _myMatch.AllPlayers.Values)
        {
            ServerSend.UpdatePlayerHealth(fromclient, _myMatch.AllPlayers[fromclient].health, player.data.PlayerId);
        }    
    }
}