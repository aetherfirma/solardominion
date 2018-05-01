using System;
using System.Collections.Generic;
using Logic.Gameplay.Players;
using Logic.Utilities;

namespace Logic.Network
{
    [Serializable]
    public class GameResponse
    {
        public string id, scenario, seed;
        public int no_players;
        public string[] players;
        public float started, modified;
        public Dictionary<string, FleetJson> rosters;
        public List<Turn> turns;

        public static GameResponse FromJson(string json)
        {
            return StringSerializationAPI.Deserialize<GameResponse>(json);
        }
    }

    [Serializable]
    public class Turn
    {
        public string player;
        public TurnType action;
        public string ship;
        public string target;
        public float[] location;
        public float rotation;
        public int speed;
    }

    public enum TurnType
    {
        Deploy,
        Move,
        Act
    }
}