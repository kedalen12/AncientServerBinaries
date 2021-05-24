using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ClientCurrentState
{
    STATE_LOBBY,
    STATE_INMATCH,
    STATE_IDLE,
    STATE_DOWN,
    STATE_PENDING
}
    public class ClientData
    {
        public string Username;
        public bool InParty;
        public int PartyID;
        public int PlayerId;
        public Client currentOwner;
        public int MatchId;
        public ClientCurrentState currentState;
        
        public ClientData(int clientId)
        {
            PlayerId = clientId;
            currentState = ClientCurrentState.STATE_PENDING;
        }

        public void SetNewUser(string user)
        {
            Username = user;
            currentState = ClientCurrentState.STATE_IDLE;
        }
    }

