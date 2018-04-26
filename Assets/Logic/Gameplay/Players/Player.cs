using System.Linq;
using Logic.Gameplay.Ships;

namespace Logic.Gameplay.Players
{
    public class Player
    {
        public Faction Faction;
        public PlayerType Type;
        public Ship[] Fleet;

        public Player(Faction faction, PlayerType type)
        {
            Faction = faction;
            Type = type;
            Fleet = new Ship[0];
        }

        public int FleetCost()
        {
            return Fleet.Sum(ship => ship.CalculateCost(ship.Training));
        }
    }
}