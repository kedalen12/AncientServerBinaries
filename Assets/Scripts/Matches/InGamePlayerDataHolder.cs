using UnityEngine;

public class InGamePlayerDataHolder
{
    public ClientData data;
    public int Pick  = -1;
    public bool connectionState, pickState;
    public int Team;
    public float defenceMp = 1;
    public InGamePlayerDataHolder(ClientData holder, int team)
    {
        data = holder;
        Team = team;
    }

    public void ResetDefence()
    {
        defenceMp = 1;
    }

    public void AddDefence()
    {
        defenceMp = .5f;
    }
    public UsablePlayerObject UsableObject { get; set; }

    public void SetPickStats(int pick)
    {
        switch (pick)
        {
            case 0:
                maxHealth = 800f;
                health = 800f;
                break;
            case 1:
                maxHealth = 1200f;
                health = 1200f;
                break;
            case 2:
                maxHealth = 1000f;
                health = 1000f;
                break;
        }
    }

    #region InGameVarsStandard
    public bool isDead;
    public float health;
    public float maxHealth;
    public Vector3 currentPosition;
    public UsablePlayerObject CurrentObject;
    #endregion

    #region AztecMapVars
    public int CurrentZone = -1;
    
    
    #endregion
}