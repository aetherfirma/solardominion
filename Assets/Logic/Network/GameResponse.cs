using System;
using UnityEngine;

namespace Logic.Network
{
    [Serializable]
    public class GameResponse
    {
        public string id, scenario, seed;
        public int no_players;
        public string[] players;
        public float started, modified;

        public static GameResponse FromJson(string json)
        {
            return JsonUtility.FromJson<GameResponse>(json);
        }
    }
}