using System;
using System.Linq;
using Logic.Display;
using Logic.Gameplay.Players;
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

        public ShipSystem[] Systems;
        public string UUID;
        public string ShipClass;
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

        private readonly string[] _shipNameNouns =
        {
            "People",
            "History",
            "Way",
            "Art",
            "World",
            "Information",
            "Map",
            "Family",
            "Government",
            "Health",
            "System",
            "Year",
            "Music",
            "Method",
            "Food",
            "Understanding",
            "Theory",
            "Law",
            "Literature",
            "Control",
            "Knowledge",
            "Power",
            "Ability",
            "Science",
            "Library",
            "Nature",
            "Fact",
            "Idea",
            "Investment",
            "Society",
            "Story",
            "Industry",
            "Community",
            "Definition",
            "Quality",
            "Development",
            "Language",
            "Management",
            "Variety",
            "Security",
            "Organization",
            "Analysis",
            "Thought",
            "Basis",
            "Direction",
            "Strategy",
            "Technology",
            "Freedom",
            "Environment",
            "Truth",
            "Writing",
            "Difference",
            "Growth",
            "Meaning",
            "Philosophy",
            "Night",
            "Chemistry",
            "Energy",
            "Role",
            "Success",
            "Moment",
            "Painting",
            "Student",
            "Competition",
            "Population",
            "Context",
            "Opportunity",
            "Performance",
            "Flight",
            "Appearance",
            "Association",
            "Concept",
            "Advice",
            "Effort",
            "Expression",
            "Importance",
            "Opinion",
            "Reality",
            "Responsibility",
            "Skill",
            "Statement",
            "Depth",
            "Estate",
            "Foundation",
            "Perspective",
            "Collection",
            "Imagination",
            "Passion",
            "Resource",
            "Connection",
            "Memory",
            "Patience",
            "Attitude",
            "Response",
            "Loss",
            "Preparation",
            "Union",
            "Tradition",
            "Protection"
        };

        private PentagonRenderer _basePentagon;

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
                switch (Player.Faction)
                {
                    case Faction.UNM:
                        return UNNName();
                    case Faction.IP3:
                        return IP3Name();
                }
            }
            catch (NullReferenceException)
            {
            }

            return ShipClass;
        }

        private string IP3Name()
        {
            var guid = new Guid(ShipUuid).ToByteArray();

            var adjectives = new[]
            {
                "Adorable",
                "Adventurous",
                "Aggressive",
                "Agreeable",
                "Alert",
                "Alive",
                "Amused",
                "Angry",
                "Annoyed",
                "Annoying",
                "Anxious",
                "Arrogant",
                "Ashamed",
                "Attractive",
                "Average",
                "Awful",
                "Bad",
                "Beautiful",
                "Better",
                "Bewildered",
                "Black",
                "Bloody",
                "Blue",
                "Blue-eyed",
                "Blushing",
                "Bored",
                "Brainy",
                "Brave",
                "Breakable",
                "Bright",
                "Busy",
                "Calm",
                "Careful",
                "Cautious",
                "Charming",
                "Cheerful",
                "Clean",
                "Clear",
                "Clever",
                "Cloudy",
                "Clumsy",
                "Colorful",
                "Combative",
                "Comfortable",
                "Concerned",
                "Condemned",
                "Confused",
                "Cooperative",
                "Courageous",
                "Crazy",
                "Creepy",
                "Crowded",
                "Cruel",
                "Curious",
                "Cute",
                "Dangerous",
                "Dark",
                "Dead",
                "Defeated",
                "Defiant",
                "Delightful",
                "Depressed",
                "Determined",
                "Different",
                "Difficult",
                "Disgusted",
                "Distinct",
                "Disturbed",
                "Dizzy",
                "Doubtful",
                "Drab",
                "Dull",
                "Eager",
                "Easy",
                "Elated",
                "Elegant",
                "Embarrassed",
                "Enchanting",
                "Encouraging",
                "Energetic",
                "Enthusiastic",
                "Envious",
                "Evil",
                "Excited",
                "Expensive",
                "Exuberant",
                "Fair",
                "Faithful",
                "Famous",
                "Fancy",
                "Fantastic",
                "Fierce",
                "Filthy",
                "Fine",
                "Foolish",
                "Fragile",
                "Frail",
                "Frantic",
                "Friendly",
                "Frightened",
                "Funny",
                "Gentle",
                "Gifted",
                "Glamorous",
                "Gleaming",
                "Glorious",
                "Good",
                "Gorgeous",
                "Graceful",
                "Grieving",
                "Grotesque",
                "Grumpy",
                "Handsome",
                "Happy",
                "Healthy",
                "Helpful",
                "Helpless",
                "Hilarious",
                "Homeless",
                "Homely",
                "Horrible",
                "Hungry",
                "Hurt",
                "Ill",
                "Important",
                "Impossible",
                "Inexpensive",
                "Innocent",
                "Inquisitive",
                "Itchy",
                "Jealous",
                "Jittery",
                "Jolly",
                "Joyous",
                "Kind",
                "Lazy",
                "Light",
                "Lively",
                "Lonely",
                "Long",
                "Lovely",
                "Lucky",
                "Magnificent",
                "Misty",
                "Modern",
                "Motionless",
                "Muddy",
                "Mushy",
                "Mysterious",
                "Nasty",
                "Naughty",
                "Nervous",
                "Nice",
                "Nutty",
                "Obedient",
                "Obnoxious",
                "Odd",
                "Old-fashioned",
                "Open",
                "Outrageous",
                "Outstanding",
                "Panicky",
                "Perfect",
                "Plain",
                "Pleasant",
                "Poised",
                "Poor",
                "Powerful",
                "Precious",
                "Prickly",
                "Proud",
                "Puzzled",
                "Quaint",
                "Real",
                "Relieved",
                "Repulsive",
                "Rich",
                "Scary",
                "Selfish",
                "Shiny",
                "Shy",
                "Silly",
                "Sleepy",
                "Smiling",
                "Smoggy",
                "Sore",
                "Sparkling",
                "Splendid",
                "Spotless",
                "Stormy",
                "Strange",
                "Stupid",
                "Successful",
                "Super",
                "Talented",
                "Tame",
                "Tender",
                "Tense",
                "Terrible",
                "Testy",
                "Thankful",
                "Thoughtful",
                "Thoughtless",
                "Tired",
                "Tough",
                "Troubled",
                "Ugliest",
                "Ugly",
                "Uninterested",
                "Unsightly",
                "Unusual",
                "Upset",
                "Uptight",
                "Vast",
                "Victorious",
                "Vivacious",
                "Wandering",
                "Weary",
                "Wicked",
                "Wide-eyed",
                "Wild",
                "Witty",
                "Worrisome",
                "Worried",
                "Wrong",
                "Zany",
                "Zealous"
            };

            return string.Format("PDV {0:} {1:}", adjectives[guid[9] % adjectives.Length],
                _shipNameNouns[guid[6] % _shipNameNouns.Length]);
        }

        private string UNNName()
        {
            var guid = new Guid(ShipUuid).ToByteArray();

            var places = new[]
            {
                "Chongqing",
                "Shanghai",
                "Delhi",
                "Beijing",
                "Mumbai",
                "Dhaka",
                "Lagos",
                "Chengdu",
                "Karachi",
                "Guangzhou",
                "Istanbul",
                "Tokyo",
                "Tianjin",
                "Moscow",
                "São Paulo",
                "Kinshasa",
                "Lahore",
                "Cairo",
                "Seoul",
                "Jakarta",
                "Wenzhou",
                "Mexico City",
                "Lima",
                "London",
                "Bangkok",
                "Xi'an",
                "Chennai",
                "Bangalore",
                "New York City",
                "Ho Chi Minh City",
                "Hyderabad",
                "Shenzhen",
                "Suzhou",
                "Nanjing",
                "Dongguan",
                "Tehran",
                "Quanzhou",
                "Shenyang",
                "Bogotá",
                "Hong Kong",
                "Baghdad",
                "Fuzhou",
                "Changsha",
                "Wuhan",
                "Hanoi",
                "Rio de Janeiro",
                "Qingdao",
                "Foshan",
                "Zunyi",
                "Santiago",
                "Riyadh",
                "Ahmedabad",
                "Singapore",
                "Shantou",
                "Ankara",
                "Yangon",
                "Saint Petersburg",
                "Sydney",
                "Casablanca",
                "Melbourne",
                "Abidjan",
                "Alexandria",
                "Kolkata",
                "Surat",
                "Johannesburg",
                "Dar es Salaam",
                "Shijiazhuang",
                "Harbin",
                "Giza",
                "?zmir",
                "Zhengzhou",
                "New Taipei City",
                "Los Angeles",
                "Changchun",
                "Cape Town",
                "Yokohama",
                "Khartoum",
                "Guayaquil",
                "Hangzhou",
                "Xiamen",
                "Berlin",
                "Busan",
                "Ningbo",
                "Jeddah",
                "Durban",
                "Algiers",
                "Kabul",
                "Hefei",
                "Mashhad",
                "Pyongyang",
                "Madrid",
                "Faisalabad",
                "Baku",
                "Tangshan",
                "Ekurhuleni",
                "Nairobi",
                "Zhongshan",
                "Pune",
                "Addis Ababa",
                "Jaipur",
                "Buenos Aires",
                "Incheon",
                "Quezon City",
                "Toronto",
                "Kiev",
                "Salvador",
                "Rome",
                "Dubai",
                "Luanda",
                "Lucknow",
                "Kaohsiung",
                "Kanpur",
                "Surabaya",
                "Taichung",
                "Basra",
                "Taipei",
                "Chicago",
                "Osaka",
                "Quito",
                "Chaozhou",
                "Fortaleza",
                "Chittagong",
                "Bandung",
                "Managua",
                "Brasília",
                "Belo Horizonte",
                "Daegu",
                "Houston",
                "Douala",
                "Medellin",
                "Yaoundé",
                "Nagpur",
                "Cali",
                "Tashkent",
                "Nagoya",
                "Isfahan",
                "Phnom Penh",
                "Paris",
                "Ouagadougou",
                "Lanzhou",
                "Kano",
                "Dalian",
                "Guatemala City",
                "Havana",
                "Rawalpindi",
                "Medan",
                "Accra",
                "Visakhapatnam",
                "Gujranwala",
                "Jinan",
                "Karaj",
                "Peshawar",
                "Minsk",
                "Caracas",
                "Sana'a",
                "Sapporo",
                "Tainan",
                "Bucharest",
                "Curitiba",
                "Shiraz",
                "Vienna",
                "Brazzaville",
                "Bhopal",
                "Almaty",
                "Hamburg",
                "Manila",
                "Kuala Lumpur",
                "Maputo",
                "Budapest",
                "Warsaw",
                "Lusaka",
                "Kathmandu",
                "Tabriz",
                "Hyderabad",
                "Palembang",
                "Tijuana",
                "Patna",
                "Montreal",
                "Davao City",
                "Harare",
                "Barcelona",
                "Maracaibo",
                "Caloocan",
                "Philadelphia",
                "Novosibirsk",
                "Phoenix",
                "Oran",
                "Semarang",
                "Recife",
                "Kobe",
                "Daejeon",
                "Kampala",
                "Kawasaki",
                "Guadalajara",
                "Auckland",
                "Vijayawada",
                "Fukuoka",
                "Gwangju",
                "Porto Alegre",
                "Kyoto",
                "San Antonio",
                "Santa Cruz de la Sierra",
                "Munich",
                "Kharkiv",
                "Yekaterinburg",
                "San Diego",
                "Barranquilla",
                "Milan",
                "Ibadan",
                "Makassar",
                "Córdoba",
                "Prague",
                "Mandalay",
                "Dallas",
                "Montevideo",
                "Qom",
                "Ahvaz",
                "Sofia",
                "Nizhny Novgorod",
                "Abuja",
                "Calgary",
                "Saitama",
                "Suwon",
                "Hiroshima",
                "Rosario",
                "Brisbane",
                "Belgrade",
                "Campinas",
                "Ulsan",
                "Omsk",
                "Dakar",
                "Abu Dhabi",
                "Monterrey",
                "Tripoli",
                "Rostov-on-Don",
                "T'bilisi",
                "Fez",
                "Birmingham",
                "Yerevan",
                "Cologne",
                "Tunis",
                "Bulawayo",
                "Astana",
                "Islamabad"
            };

            return string.Format("UNN {0:} of {1:}", _shipNameNouns[guid[6] % _shipNameNouns.Length],
                places[guid[15] % places.Length]);
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
    }
}