using System;
using System.Collections.Generic;
using Logic.Gameplay.Players;
using Logic.Gameplay.Ships;
using Logic.Utilities;

namespace Logic.Network
{
    [Serializable]
    public class GameListResponse
    {
        public string[] games;

        public static GameListResponse FromJson(string json)
        {
            return StringSerializationAPI.Deserialize<GameListResponse>(json);
        }
    }

    [Serializable]
    public class WaitingGame
    {
        public string id, scenario;
        public string[] players;
        public float started;
        public int points_limit;

        public static WaitingGame FromJson(string json)
        {
            return StringSerializationAPI.Deserialize<WaitingGame>(json);
        }
    }

    [Serializable]
    public class WaitingResponse
    {
        public WaitingGame[] games;

        public static WaitingResponse FromJson(string json)
        {
            return StringSerializationAPI.Deserialize<WaitingResponse>(json);
        }
    }
    
    [Serializable]
    public class GameResponse
    {
        public string id, scenario, seed;
        public int no_players, points_limit;
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
        public string target_player;
        public string target;
        public float[] location;
        public float rotation;
        public int speed;
        public Order? order;
        public Dictionary<int, int> system_status;
        public int defence_modifier;
        public int shots;
        public int damage;
        public string weapon;
        public int thrust;
    }

    public enum TurnType
    {
        Deploy,
        MovementPhase,
        ActionChallenge,
        ActionPhase,
        ActionResponse,
        ActionEnd,
        CommandPhase
    }
}