using Logic.Gameplay.Ships;
using UnityEngine;

namespace Logic.Utilities
{
    public class DebugShip : MonoBehaviour
    {
        public RectTransform Canvas;
        public ShipCard ShipCard;
        
        private void Start()
        {
            var ship = GetComponent<Ship>();
            ship.SetupStatusArrays();
            var card = Instantiate(ShipCard, Canvas);
            card.Ship = ship;
        }
    }
}