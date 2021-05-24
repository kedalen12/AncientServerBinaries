using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parties
{
    private static readonly SortedDictionary<int, List<ClientData>> CurrentParties = new SortedDictionary<int, List<ClientData>>();
    public static int AddParty(List<ClientData> _members)
    {
        if (CurrentParties.Count == 0)
        {
            CurrentParties.Add(1,_members);
            return 1;
        }
        else
        {
            var x = CurrentParties.Count + 1;
            CurrentParties.Add(x,_members);
            return x;
        }
    }
    public static void RemoveParty(int _partyID)
    {
        CurrentParties.Remove(_partyID);
    }
    public static List<ClientData> GetParty(int _partyID)
    {
        return CurrentParties[_partyID];
    }
    public static void AddToExistingParty(int _partyLeaderPartyID, ClientData _partyMember)
    {
        List<ClientData> x = GetParty(_partyLeaderPartyID);
        x.Add(_partyMember);
        CurrentParties[_partyLeaderPartyID] = x;

    }
    public static void RemoveFromExistingParty(int _partyLeaderPartyID, ClientData _partyMember)
    {
        List<ClientData> x = GetParty(_partyLeaderPartyID);
        x.Remove(_partyMember);
        CurrentParties[_partyLeaderPartyID] = x;

    }
}
