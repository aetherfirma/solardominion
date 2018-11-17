using System;
using System.Collections.Generic;
using System.Linq;
using Logic.Display;
using Logic.Gameplay.Players;
using Logic.Gameplay.Rules;
using Logic.Maths;
using Logic.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Logic.Gameplay.Ships
{
    public class Ship : MonoBehaviour
    {
        public TextMeshPro TextPrefab;

        public ShipSystemPosition[] Systems;
        public string UUID;
        public string ShipClass;
        public string ClassVariant;
        public Player Player;
        public Faction Faction;
        public string ShipUuid;
        public bool[] Damage;
        public bool[] Used;
        public int[] Subsystem;
        public int Speed;
        public int Training;
        public int Initiative;
        public bool UnderOrders, Orderable;
        public bool Fleeing, Fled;
        public Order Order;
        public bool Deployed;
        public int ThrustRemaining;
        public ShipCard Card;
        public Ship FirstShipFiredAt;
        public Sprite ShipIcon;
        [Range(2, 6)] public int MinimumTraining;
        [Range(2, 6)] public int MaximumTraining;

        private Text _speedMarker;
        private int _setSpeed = -1;
        private Vector3 _desiredPosition;

        private PentagonRenderer _basePentagon;

        private void CheckSystemDiagram()
        {
            var systems = new bool[20];
            foreach (var systemPosition in Systems)
            {
                for (int dx = 0; dx < systemPosition.Width; dx++)
                {
                    for (int dy = 0; dy < systemPosition.Height; dy++)
                    {
                        var i = 5 * (systemPosition.Y + dy) + dx + systemPosition.X;
                        if (systems[i] == true) throw new Exception(string.Format("{0},{1} is double filled", systemPosition.X + dx, systemPosition.Y + dy));
                        systems[i] = true;
                    }
                }
            }

            var unfilled = new List<string>();
            for (int i = 0; i < 20; i++)
            {
                if (systems[i] == false) unfilled.Add(string.Format("{0}, {1}", i / 5, i % 5));
            }
            if (unfilled.Count > 0) throw new Exception(string.Format("Positions {0} filled", string.Join(", ", unfilled.ToArray())));
        }

        public ShipSystem GetCompositeSystem()
        {
            return (from systemPosition in Systems where systemPosition.System.Type == SystemType.Composite select systemPosition.System).FirstOrDefault();
        }

        public int[] GetCompositeSystems()
        {
            var composites = new List<int>();

            for (int i = 0; i < Systems.Length; i++)
            {
                if (Systems[i].System.Type == SystemType.Composite) composites.Add(i);
            }

            return composites.ToArray();
        }

        public bool Alive
        {
            get { return !(Fled || Damage.All(d => d)); }
        }

        public Vector3 Position
        {
            get { return _desiredPosition; }
            set { _desiredPosition = value; }
        }

        private void LerpPosition()
        {
            var delta = _desiredPosition - transform.position;
            var movement = delta.normalized * Speed * 2.5f * Time.deltaTime;
            if (delta.magnitude < movement.magnitude) transform.position = _desiredPosition;
            else transform.position = transform.position + movement;
        }

        public void SkipMovement()
        {
            transform.position = _desiredPosition;
        }

        public void CalculateThrust()
        {
            ThrustRemaining = Systems
                .Select(GetSystemOrSubsystem)
                .Where((system, index) => system.Type == SystemType.Engine && !Damage[index])
                .Sum(system => system.Thrust);
            if (UnderOrders && Order == Order.MilitaryThrust) ThrustRemaining *= 2;
        }

        public bool CanIssueOrders()
        {
            return Systems
                .Select(GetSystemOrSubsystem)
                .Where((system, index) => system.Type == SystemType.Command && !Damage[index])
                .Any();
        }

        private ShipSystem GetSystemOrSubsystem(ShipSystemPosition system, int index)
        {
            if (system.System.Type == SystemType.Composite)
            {
                if (Damage[index] || Subsystem[index] == -1) return system.System;
                return system.System.SubSystems[Subsystem[index]];
            }
            return system.System;
        }

        public int CalculateDefensiveModifier()
        {
            return Systems
                .Select(GetSystemOrSubsystem)
                .Where((system, index) => system.Type == SystemType.Defence && !Damage[index])
                .Sum(system => system.Defence);
        }

        public float DistancePerThrust()
        {
            switch (Class)
            {
                case ShipSize.StrikeCraft:
                    return 20;
                case ShipSize.Corvette:
                    return 15;
                case ShipSize.Frigate:
                    return 10;
                case ShipSize.Destroyer:
                    return 5;
                case ShipSize.Cruiser:
                    return 2.5f;
                default:
                    return 0;
            }
        }

        public void SetInitiative(WellRng rng)
        {
            Initiative = Training + rng.D6();
            UnderOrders = false;
            FirstShipFiredAt = null;
            for (var i = 0; i < Systems.Length; i++)
            {
                Subsystem[i] = -1;
                Used[i] = false;
            }
        }

        public int CalculateCost(int training)
        {
            return Systems.Sum(shipSystem => shipSystem.System.Cost[training - 2]) + Systems.Length * 10;
        }

        private void Start()
        {
            CheckSystemDiagram();
            DisplayPentagon();

            var hardpoints = gameObject.FindChildrenWithName("Hardpoint");
            var systemSearch = 0;
            foreach (var hardpoint in hardpoints)
            {
                while (systemSearch < Systems.Length && !Systems[systemSearch].System.Displayed)
                {
                    systemSearch++;
                }

                if (systemSearch < Systems.Length && Systems[systemSearch].System.Displayed)
                {
                    Instantiate(Systems[systemSearch].System.Model[(int) Faction], hardpoint.transform);
                    systemSearch++;
                }
            }
        }

        private void SetSpeedMarker(int speed)
        {
            if (speed == _setSpeed) return;
            _setSpeed = speed;

            _basePentagon.Text = speed == 0
                ? string.Format("<size=7.5><smallcaps>{0:}</smallcaps></size>", Name())
                : string.Format("<size=7.5><smallcaps>{0:}</smallcaps></size>\nSpeed {1:}", Name(), speed);
        }

        private void DisplayPentagon()
        {
            _basePentagon = PentagonRenderer.CreatePentagon(0.25f, TextPrefab).GetComponent<PentagonRenderer>();

            _basePentagon.transform.parent = transform;
            _basePentagon.transform.localPosition = new Vector3(0, -3, 0);
            _basePentagon.transform.localRotation = Quaternion.identity;
            _basePentagon.transform.localScale = Vector3.one;
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
            return string.Format(
                "{0:} - {1:}\nTraining {2:} - Initative {3:}\nSpeed {4:} - {5:} Thrust remaining\n{6:} damage taken",
                Name(), Class, Training, Initiative, Speed, ThrustRemaining, Damage.Count(d => d)
            );
        }

        public string Name()
        {
            try
            {
                switch (Faction)
                {
                    case Faction.UNM:
                        return ShipNameGenerator.UNNName(this);
                    case Faction.IP3:
                        return ShipNameGenerator.IP3Name(this);
                }
            }
            catch (NullReferenceException)
            {
            }

            return ShipClass;
        }

        public void SetupStatusArrays()
        {
            Damage = new bool[Systems.Length];
            Used = new bool[Systems.Length];
            Subsystem = new int[Systems.Length];
            for (var i = 0; i < Systems.Length; i++) Subsystem[i] = -1;
        }

        public bool ShouldShipContinue()
        {
            return Systems.Select((t, i) => t.System.ResolveSystem(Subsystem[i]))
                .Where((system, i) => (system.Type == SystemType.Weapon ||
                                       system.Type == SystemType.Hangar) && !Damage[i] && !Used[i])
                .Any();
        }

        public Ship Initialise(int training, Referee referee, Player player = null, string uuid = null)
        {
            var newShip = Instantiate(this, referee.RootTransform);
            newShip.name = name;
            newShip.gameObject.active = false;
            newShip.Training = training;
            newShip.ShipUuid = uuid ?? Guid.NewGuid().ToString();
            newShip.Player = player;
            newShip.SetupStatusArrays();
            return newShip;
        }

        public ShipJson ToSerializable()
        {
            return new ShipJson(UUID, Training, ShipUuid);
        }

        public void TakeDamage(WellRng rng, int damage)
        {
            while (damage > 0 && Alive)
            {
                var systemToDamage =
                    Damage.Select((damaged, index) => damaged ? -1 : index).Where(i => i >= 0).Random(rng);
                Damage[systemToDamage] = true;
                damage--;
            }
        }

        public Vector3[] HitPoints()
        {
            return HitPoints(Position, transform.rotation.eulerAngles.y);
        }

        public Vector3[] HitPoints(Vector3 atLocation, float rotation)
        {
            var points = new Vector3[5];
            
            for (int i = 0; i < 5; i++)
            {
                points[i] = new Vector2().FromAngleAndMagnitude(Mathf.PI * 2 / 5 * i + (Mathf.PI / 2) + rotation, 2.5f).Vec2ToVec3() + atLocation;
            }

            return points;
        }

        private void Update()
        {
            SetSpeedMarker(Speed);
            LerpPosition();
        }

        public void MarkAsUsed(bool[] used)
        {
            for (var i = 0; i < used.Length; i++)
            {
                if (used[i]) Used[i] = true;
            }
        }
    }
}