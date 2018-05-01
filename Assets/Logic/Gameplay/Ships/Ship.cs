using System;
using System.Linq;
using Logic.Gameplay.Players;
using Logic.Utilities;
using UnityEngine;

namespace Logic.Gameplay.Ships
{
    public class Ship : MonoBehaviour
    {   
        public ShipSystem[] Systems;
        public string UUID;
        public Player Player;
        public string ShipUuid;
        public bool[] Damage;
        public int Speed;
        public int Training;
        public int Initiative;
        public bool UnderOrders;
        public Orders Order;
        public bool Deployed;
        [Range(2,6)] public int MinimumTraining;
        [Range(2,6)] public int MaximumTraining;

        public int CalculateCost(int training)
        {
            return Systems.Sum(shipSystem => shipSystem.Cost[training - 2]) + Systems.Length * 10;
        }

        private void Start()
        {
            var hardpoints = gameObject.FindChildrenWithName("Hardpoint");
            int systemSearch = 0;
            foreach (var hardpoint in hardpoints)
            {
                while (systemSearch < Systems.Length && !Systems[systemSearch].Displayed)
                {
                    systemSearch++;
                }

                if (systemSearch < Systems.Length && Systems[systemSearch].Displayed)
                {
                    Instantiate(Systems[systemSearch].Model, hardpoint.transform);
                    systemSearch++;
                }
                
            }
        }

        public Ship Initialise(int training, Player player = null, string uuid = null)
        {
            var newShip = Instantiate(this);
            newShip.name = name;
            newShip.gameObject.active = false;
            newShip.Training = training;
            newShip.ShipUuid = uuid ?? Guid.NewGuid().ToString();
            newShip.Player = player;
            return newShip;
        }

        public ShipJson ToSerializable()
        {
            return new ShipJson(UUID, Training, ShipUuid);
        }
    }
}