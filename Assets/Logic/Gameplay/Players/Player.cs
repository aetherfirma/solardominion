using System;
using System.Linq;
using Logic.Gameplay.Ships;
using Logic.Utilities;
using UnityEngine;

namespace Logic.Gameplay.Players
{
    public class Player
    {
        public Faction? Faction;
        public PlayerType Type;
        public Ship[] Fleet;
        public string Uuid;

        public Player(string uuid, PlayerType type)
        {
            Faction = null;
            Type = type;
            Uuid = uuid;
            Fleet = new Ship[0];
        }

        public int FleetCost()
        {
            return Fleet.Sum(ship => ship.CalculateCost(ship.Training));
        }

        public string FleetJson()
        {
            return StringSerializationAPI.Serialize<FleetJson>(new FleetJson(Faction, Fleet.Select(ship => ship.ToSerializable()).ToArray()));
        }

        public void UpdateFleet(FleetJson fleet, Ship[] ships)
        {
            Faction = fleet.Faction;
            Fleet = fleet.Ships.Select(shipRep =>
            {
                foreach (var ship in ships)
                {
                    if (ship.UUID == shipRep.Uuid) return ship.Initialise(shipRep.Training, this, shipRep.ShipUuid);
                }

                throw new ArgumentException();
            }).ToArray();
        }
    }

    [Serializable]
    public class FleetJson
    {
        public ShipJson[] Ships;
        public Faction? Faction;

        public FleetJson(Faction? faction, ShipJson[] ships)
        {
            Ships = ships;
            Faction = faction;
        }
    }
}