using System.Linq;
using Logic.Gameplay.Ships;

namespace Logic.Gameplay.Players
{
    public class Player
    {
        public Faction? Faction;
        public PlayerType Type;
        public Ship[] Fleet;

        public Player(PlayerType type)
        {
            Faction = null;
            Type = type;
            Fleet = new Ship[0];
        }

        public int FleetCost()
        {
            return Fleet.Sum(ship => ship.CalculateCost(ship.Training));
        }
    }
}