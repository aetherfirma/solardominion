using System;
using System.Linq;
using Logic.Gameplay.Players;
using Logic.Maths;
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
        public bool Fleeing, Fled;
        public Orders Order;
        public bool Deployed;
        [Range(2,6)] public int MinimumTraining;
        [Range(2,6)] public int MaximumTraining;

        public bool Alive
        {
            get { return !(Fled || Damage.All(d => d)); }
        }

        public void SetInitiative(WellRng rng)
        {
            Initiative = Training + rng.D6();
        }

        public int CalculateCost(int training)
        {
            return Systems.Sum(shipSystem => shipSystem.Cost[training - 2]) + Systems.Length * 10;
        }

        private void Start()
        {
            var hardpoints = gameObject.FindChildrenWithName("Hardpoint");
            var systemSearch = 0;
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

        public ShipSize Class
        {
            get
            {
                if (Systems.Length <= 2) return ShipSize.StrikeCraft;
                if (Systems.Length <= 5) return ShipSize.Corvette;
                if (Systems.Length <= 8) return ShipSize.Frigate;
                if (Systems.Length <= 12) return ShipSize.Destroyer;
                return ShipSize.Cruiser;
            }
        }

        public string Describe()
        {
            var prefix = "SS";
            switch (Player.Faction)
            {
                case Faction.UNM:
                    prefix = "UNN";
                    break;
                case Faction.IP3:
                    prefix = "PDV";
                    break;
            }

            return string.Format(
                "{0:} {1:} - {2:}\nTraining {3:}\nInitative {4:}",
                prefix, "Ship Name Here", Class, Training, Initiative
            );
        }

        private void SetupDamageArray()
        {
            Damage = new bool[Systems.Length];
        }

        public Ship Initialise(int training, Player player = null, string uuid = null)
        {
            var newShip = Instantiate(this);
            newShip.name = name;
            newShip.gameObject.active = false;
            newShip.Training = training;
            newShip.ShipUuid = uuid ?? Guid.NewGuid().ToString();
            newShip.Player = player;
            newShip.SetupDamageArray();
            return newShip;
        }

        public ShipJson ToSerializable()
        {
            return new ShipJson(UUID, Training, ShipUuid);
        }
    }
}