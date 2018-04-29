using System;
using UnityEngine;

namespace Logic.Network
{
    [Serializable]
    public class JoinResponse
    {
        public string player_id;
        public GameResponse game;
        
        public static JoinResponse FromJson(string json)
        {
            return JsonUtility.FromJson<JoinResponse>(json);
        }

    }
}