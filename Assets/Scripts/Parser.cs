using System;
using System.Collections.Generic;
using System.Linq;
public abstract class Parser
{
    public static List<Map> ParseIntToMap(IEnumerable<int> mapOrder) { return mapOrder.Select(intMap => Dictionaries.DataManager.Maps[intMap]).ToList(); }
    
    public static List<Map> ParseMapToInt(IEnumerable<int> mapOrder) { return mapOrder.Select(intMap => Dictionaries.DataManager.Maps[intMap]).ToList(); }

    public static List<int> ParseHolderToInt(IEnumerable<ClientData> holders)
    {
        return holders.Select(id => id.PlayerId).ToList();
    }
    public static void PopulateMapDictionary()
    {
        Dictionaries.DataManager.Maps.Add(1,new Map(1,"Maya Ruins"));
        /*Dictionaries.Maps.Add(2,new Map(2,"Ship Adventures"));
        Dictionaries.Maps.Add(3,new Map(3,"Random Map 3"));
        Dictionaries.Maps.Add(4,new Map(4,"Random Map 4"));
        Dictionaries.Maps.Add(5,new Map(5,"Random Map 5"));*/
    }

}

