using System;
using Logic.Utilities;

namespace Logic.Network
{
    [Serializable]
    public class JoinResponse
    {
        public string player_id;
        public GameResponse game;
        
        public static JoinResponse FromJson(string json)
        {
            return StringSerializationAPI.Deserialize<JoinResponse>(json);
        }

    }
}