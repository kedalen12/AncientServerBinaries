using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Dictionaries
{
    public static Dictionaries DataManager;

    public static void InitializeDictionaries()
    {
        DataManager = new Dictionaries();
    }

    public List<ClientData> ConnectedUsers = new List<ClientData>();
    public Dictionary<int, int> PartiesDictionary = new Dictionary<int, int>();
    public Dictionary<int, MatchN> InProgressMatches = new Dictionary<int, MatchN>();
    public Dictionary<int,Map> Maps = new Dictionary<int,Map>();

    public ClientData GetClientData(int id)
    {
        return ConnectedUsers.FirstOrDefault(d => d.PlayerId == id);
    }
    public ClientData GetClientData(string user) 
    {
        return ConnectedUsers.FirstOrDefault(d => d.Username == user);
    }
}