using System;
using UnityEngine;

namespace PigeonSandbox.Editor
{
    // Exercise Unity's actual save serializer without reading or writing the player's town.
    public static class FriendshipChecks
    {
        public static void Verify()
        {
            var town=new TownSimulation();
            var save=town.Capture();
            save.Birds[1].Name="しずくちゃん";
            save.Friendships.Add(new BirdFriendship{FirstId=save.Birds[0].Id,SecondId=save.Birds[1].Id,SharedSeconds=14});
            save.Wishes[0].Complete=true;
            var decoded=JsonUtility.FromJson<TownSave>(JsonUtility.ToJson(save));
            if(!town.Restore(decoded)||town.BestFriendOf(town.Birds[0])?.Name!="しずくちゃん")
                throw new Exception("Friendship JSON roundtrip failed");
            if(!town.Wishes[0].Complete)throw new Exception("Wish JSON roundtrip failed");
            var legacy=new TownSimulation().Capture();
            string json=JsonUtility.ToJson(legacy).Replace("\"Friendships\":[],","");
            if(json.Contains("Friendships")||!town.Restore(JsonUtility.FromJson<TownSave>(json))||town.BestFriendOf(town.Birds[0])!=null)
                throw new Exception("Legacy JSON friendship migration failed");
            string oldJson=System.Text.RegularExpressions.Regex.Replace(JsonUtility.ToJson(legacy),"\"Wishes\":\\[[^\\]]*\\],","");
            if(oldJson.Contains("Wishes")||!town.Restore(JsonUtility.FromJson<TownSave>(oldJson))||town.Wishes.Count!=3)
                throw new Exception("Legacy JSON wish migration failed");
            Debug.Log("PIGEON FRIENDSHIP AND WISH JSON VERIFICATION PASSED");
        }
    }
}
