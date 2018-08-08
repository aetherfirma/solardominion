using System;
using System.Linq;
using Logic.Gameplay.Players;
using Logic.Maths;
using Logic.Utilities;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Logic.Gameplay.Ships
{
    public class Ship : MonoBehaviour
    {
        public ShipSystem[] Systems;
        public string UUID;
        public Player Player;
        public Faction Faction;
        public string ShipUuid;
        public bool[] Damage;
        public bool[] Used;
        public int[] Subsystem;
        public int Speed;
        public int Training;
        public int Initiative;
        public bool UnderOrders;
        public bool Fleeing, Fled;
        public Order Order;
        public bool Deployed;
        public int ThrustRemaining;
        [Range(2, 6)] public int MinimumTraining;
        [Range(2, 6)] public int MaximumTraining;

        private Text _speedMarker;
        private int _setSpeed = -1;
        private Vector3 _desiredPosition;

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
            var movement = delta.normalized * Speed * Time.deltaTime;
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
                .Select((system, index) =>
                    system.Type == SystemType.Composite ? system.SubSystems[Subsystem[index]] : system)
                .Where(system => system.Type == SystemType.Engine)
                .Sum(system => system.Thrust);
        }

        public int CalculateDefensiveModifier()
        {
            return Systems
                .Select((system, index) =>
                    system.Type == SystemType.Composite ? system.SubSystems[Subsystem[index]] : system)
                .Where(system => system.Type == SystemType.Defence)
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
            for (var i = 0; i < Systems.Length; i++)
            {
                Subsystem[i] = -1;
                Used[i] = false;
            }
        }

        public int CalculateCost(int training)
        {
            return Systems.Sum(shipSystem => shipSystem.Cost[training - 2]) + Systems.Length * 10;
        }

        private void Start()
        {
            DisplayPentagon();
            AddSpeedMarker();

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
                    Instantiate(Systems[systemSearch].Model[(int) Faction], hardpoint.transform);
                    systemSearch++;
                }
            }
        }

        private void AddSpeedMarker()
        {
            var markerCanvas = new GameObject("Speed Marker Canvas", typeof(Canvas));
            markerCanvas.transform.parent = transform;
            markerCanvas.transform.localPosition = new Vector3(0,-3,-4);
            markerCanvas.transform.localRotation = Quaternion.Euler(90,0,0);
            markerCanvas.transform.localScale = new Vector3(.1f,.1f,.1f);
            
            var marker = new GameObject("Speed Marker", typeof(Text));
            marker.transform.parent = markerCanvas.transform;
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localRotation = Quaternion.identity;
            marker.transform.localScale = Vector3.one;

            _speedMarker = marker.GetComponent<Text>();
            _speedMarker.supportRichText = true;
            SetSpeedMarker(0);
            _speedMarker.font = Font.CreateDynamicFontFromOSFont("Sansasion", 14);
            _speedMarker.alignment = TextAnchor.MiddleCenter;
        }

        private void SetSpeedMarker(int speed)
        {
            if (speed == _setSpeed) return;
            _setSpeed = speed;

            if (speed == 0) _speedMarker.text = string.Format("<size=10>{0:}</size>", Name());
            else _speedMarker.text = string.Format("<size=10>{0:}</size>\nSpeed {1:}", Name(), speed);
        }

        private void DisplayPentagon()
        {
            var outline = new GameObject("BaseOutline", typeof(LineRenderer));
            outline.transform.parent = transform;
            outline.transform.localPosition = new Vector3(0,-3,0);
            outline.transform.localRotation = Quaternion.identity;
            outline.transform.localScale = Vector3.one;
            var outlineRenderer = outline.GetComponent<LineRenderer>();
            outlineRenderer.useWorldSpace = false;
            outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
            outlineRenderer.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
            outlineRenderer.widthMultiplier = 0.25f * outline.transform.lossyScale.x;
            outlineRenderer.positionCount = 5;
            outlineRenderer.loop = true;
            for (int i = 0; i < 5; i++)
            {
                outlineRenderer.SetPosition(i, new Vector2().FromAngleAndMagnitude(Mathf.PI*2/5*i+(Mathf.PI/2), 2.5f).Vec2ToVec3());
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
            return string.Format(
                "{0:} - {1:}\nTraining {2:} - Initative {3:}\nSpeed {4:} - {5:} Thrust remaining\n{6:} damage taken",
                Name(), Class, Training, Initiative, Speed, ThrustRemaining, Damage.Count(d => d)
            );
        }

        public string Name()
        {
            var prefix = "SS";
            try
            {
                switch (Player.Faction)
                {
                    case Faction.UNM:
                        prefix = "UNN";
                        break;
                    case Faction.IP3:
                        prefix = "PDV";
                        break;
                }
            }
            catch (NullReferenceException)
            {
            }

            return prefix + " Spaceship";
        }

        private void SetupStatusArrays()
        {
            Damage = new bool[Systems.Length];
            Used = new bool[Systems.Length];
            Subsystem = new int[Systems.Length];
            for (var i = 0; i < Systems.Length; i++) Subsystem[i] = -1;
        }

        public bool ShouldShipContinue()
        {
            return Systems.Select((t, i) => t.ResolveSystem(Subsystem[i]))
                .Where((system, i) => (system.Type == SystemType.Weapon ||
                                      system.Type == SystemType.Hangar) && !Damage[i] && !Used[i])
                .Any();
        }

        public Ship Initialise(int training, Player player = null, string uuid = null)
        {
            var newShip = Instantiate(this);
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

        public void TakeDamage(WellRng rng, int result)
        {
            while (result > 0 && Alive)
            {
                var systemToDamage =
                    Damage.Select((damaged, index) => damaged ? -1 : index).Where(i => i >= 0).Random(rng);
                Damage[systemToDamage] = true;
                result--;
            }
        }

        private void Update()
        {
            SetSpeedMarker(Speed);
            LerpPosition();
        }
    }
}