using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Logic.Display;
using Logic.Gameplay.Players;
using Logic.Gameplay.Ships;
using Logic.Maths;
using Logic.Network;
using Logic.Ui;
using Logic.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Logic.Gameplay.Rules
{
    public class Referee : MonoBehaviour
    {
        public Material[] Skyboxes;
        public GameObject[] Asteroids;
        public AsteroidField[] AsteroidFields;
        public GameObject ShipDestroyedExplosion;
        public GameObject ShipHitExplosion;
        public Camera Camera;
        public RectTransform UiCanvas;
        public ShipArray[] Ships;
        public Player[] Players;
        public GamePhase Phase;
        public int CurrentPlayer, LocalPlayer;
        public int PlayArea = 120;
        public string Seed;
        public WellRng Rng;
        public int PointsLimit;
        public GameObject RangeRings;
        public TextMeshPro TextPrefab;
        public Popup Popup;

        public string Username, Password;

        private Vector3? _lastRightMousePos, _lastMiddleMousePos;

        public Plane PlaySurface;

        public Button LowerButton;
        public RectTransform SelectionPanel;
        public RectTransform FlashedMessageElement;
        public RectTransform StartScreen;
        public RectTransform LoginScreen;
        public RectTransform ServerBrowser;
        public RectTransform ServerBrowserItem;
        public RectTransform NewGameScreen;
        public RectTransform FactionSelection;
        public RectTransform PauseMenu;
        public RectTransform LowerBar;
        public RectTransform TurnIndicator;
        public RectTransform FullArrow, OutlineArrow;
        public Sprite ButtonSprite;
        public Font StandardFont;
        public MessageTooltip Tooltip;
        public string ServerUrl;

        private ListBuilder _listBuilder;
        private SetupHandler _setupHandler;
        private StartScreen _startScreen;
        private GameplayHandler _gameplayHandler;
        private PauseHandler _pauseHandler;

        public GameResponse CurrentGameState;
        public float UpdateInterval = 5;

        public Vector3 MouseLocation;
        public GameObject MouseSelection;

        public int LastObservedInstruction;

        private GameObject _gameGrid;

        public void SetGameState(GameResponse response)
        {
            CurrentGameState = response;
        }

        public IEnumerator GameStateCoroutine()
        {
            for (;;)
            {
                SimpleRequest.Get(
                    ServerUrl + "/game/" + GameUuid,
                    www => SetGameState(GameResponse.FromJson(www.downloadHandler.text)),
                    www => FlashMessage("There was a server error (" + www.responseCode + ")\n" + www.error),
                    www => FlashMessage("There was a network error\n" + www.error)
                );
                yield return new WaitForSecondsRealtime(UpdateInterval);
            }
        }

        private Queue<FlashedMessage> _flashedMessages = new Queue<FlashedMessage>();

        public CameraOperator CameraOperator;

        private void Awake()
        {
            _gameGrid = transform.Find("Game Grid").gameObject;

            Rng = new WellRng(Seed);
        }

        public string GameUuid, PlayerUuid;
        public Scenario Scenario;

        private void Start()
        {
            CameraOperator = new CameraOperator(Camera.transform.parent);
            PlaySurface = new Plane(Vector3.up, Vector3.zero);
            Scenario = Scenario.Confrontation;

            _loginScreen = new LoginScreen(this);
            _newGameScreen = new NewGameScreen(this);
            _joinGameScreen = new JoinGameScreen(this);
            _startScreen = new StartScreen(this);
            _listBuilder = new ListBuilder(this);
            _setupHandler = new SetupHandler(this);
            _gameplayHandler = new GameplayHandler(this);
            _pauseHandler = new PauseHandler(this);

            RenderSettings.skybox = Skyboxes[Random.Range(0, Skyboxes.Length)];
        }

        public void UpdateMouseLocationAndSelection()
        {
            var ray = Camera.ScreenPointToRay(Input.mousePosition);
            float enter;
            PlaySurface.Raycast(ray, out enter);
            MouseLocation = ray.GetPoint(enter);

            RaycastHit hitInfo;
            MouseSelection = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo, 200)
                ? hitInfo.collider.gameObject
                : null;
        }

        public void DisplayUpperText(string s)
        {
            Debug.Log(s);
        }

        private void UpdateFlashedMessages()
        {
            while (_flashedMessages.Count > 0 && _flashedMessages.Peek().DiesAt < Time.time)
            {
                Destroy(_flashedMessages.Dequeue().Transform.gameObject);
            }

            var currentHeight = -45f;
            foreach (var message in _flashedMessages)
            {
                message.Transform.anchoredPosition = new Vector2(message.Transform.anchoredPosition.x,
                    currentHeight - message.Transform.sizeDelta.y / 2);
                currentHeight -= 10 + message.Transform.sizeDelta.y;
            }
        }

        public void FlashMessage(string message, float ttl = 30)
        {
            var m = new FlashedMessage(message, ttl, FlashedMessageElement, UiCanvas);
            m.Transform.anchoredPosition = new Vector2(m.Transform.anchoredPosition.x,
                -45 - _flashedMessages.Sum(mess => mess.Transform.sizeDelta.y) -
                10 * _flashedMessages.Count - m.Transform.sizeDelta.y / 2);
            _flashedMessages.Enqueue(m);
        }

        private Player[] CreatePlayers()
        {
            if (Players != null)
            {
                foreach (var player in Players)
                {
                    foreach (var ship in player.Fleet)
                    {
                        Destroy(ship.gameObject);
                    }
                }
            }

            var players = new Player[CurrentGameState.no_players];

            for (var i = 0; i < players.Length; i++)
            {
                var uuid = CurrentGameState.players[i];
                players[i] = new Player(uuid, uuid == PlayerUuid ? PlayerType.Local : PlayerType.Network);
                if (CurrentGameState.rosters.ContainsKey(uuid))
                    players[i].UpdateFleet(CurrentGameState.rosters[uuid],
                        Ships[(int) CurrentGameState.rosters[uuid].Faction].Ships);
                if (uuid == PlayerUuid) LocalPlayer = i;
                players[i].Number = i;
            }

            return players;
        }

        private void Update()
        {
            HandleCameraMovement();
            CameraOperator.UpdateCamera();

            UpdateMouseLocationAndSelection();

            _gameGrid.GetComponentInChildren<MeshRenderer>().material.SetVector("_MousePosition", MouseLocation);

            SetTooltip();

            UpdateFlashedMessages();
            
            _pauseHandler.Update();

            switch (Phase)
            {
                case GamePhase.Login:
                    _loginScreen.Update();
                    break;
                case GamePhase.MainMenu:
                    _startScreen.Update();
                    break;
                case GamePhase.NewGame:
                    _newGameScreen.Update();
                    break;
                case GamePhase.JoinGame:
                    _joinGameScreen.Update();
                    break;
                case GamePhase.PlayerSelection:
                    DisplayUpperText("Waiting on other players");
                    if (CurrentGameState.players.Length == CurrentGameState.no_players)
                    {
                        Players = CreatePlayers();
                        SetupGameWorld();
                        Phase = GamePhase.ListBuilding;
                    }

                    break;
                case GamePhase.ListBuilding:
                    _listBuilder.Update();
                    break;
                case GamePhase.Waiting:
                    DisplayUpperText("Waiting on other players");
                    Players = CreatePlayers();
                    if (Players.All(player => player.Faction != null))
                    {
                        Phase = GamePhase.Setup;
                    }

                    break;
                case GamePhase.Setup:
                    _setupHandler.Update();
                    break;
                case GamePhase.Play:
                    _gameplayHandler.Update();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private float ClosestDistanceTo(Vector3 point, IEnumerable<Vector3> targets)
        {
            float minDist = float.MaxValue;

            foreach (var target in targets)
            {
                if (target != Vector3.zero) minDist = Mathf.Min(minDist, (point - target).magnitude);
            }

            return minDist;
        }

        private Vector3 PlaceAsteroidField()
        {
            var asteroidField = new Vector3(Rng.NextFloat(-45, 45), 0, Rng.NextFloat(-45, 45));

            var asteroidLocations = AsteroidFields.Select(field => field.Location).ToArray();
            while (ClosestDistanceTo(asteroidField, asteroidLocations) < 25)
            {
                asteroidField = new Vector3(Rng.NextFloat(-45, 45), 0, Rng.NextFloat(-45, 45));
            }

            return asteroidField;
        }

        private void SetupGameWorld()
        {
            var guid = new Guid(CurrentGameState.id).ToByteArray();

            RenderSettings.skybox = Skyboxes[guid[5] % Skyboxes.Length];

            var largeAsteroidFields = guid[3] % 3 + 1;
            var smallAsteroidFields = guid[9] % 4 + 2;

            AsteroidFields = new AsteroidField[largeAsteroidFields + smallAsteroidFields];
            
            

            for (int i = 0; i < largeAsteroidFields; i++)
            {
                var asteroidField = PlaceAsteroidField();
                AsteroidFields[i] = new AsteroidField(asteroidField, 12);
                var ring = new GameObject("Large Asteroid Field");
                ring.transform.position = asteroidField;
                var arc = ArcRenderer.NewArc(ring.transform, 12, 0.25f, 0, Mathf.PI * 2, 32, Color.white);
                arc.transform.position = arc.transform.position + new Vector3(0, -5, 0);

                var rockLocations = new List<Vector3>();

                for (int j = 0; j < Rng.NextInt(4,10); j++)
                {
                    var asteroid = Asteroids[Rng.NextInt(0, Asteroids.Length-1)];
                    var rock = Instantiate(asteroid, ring.transform);
                    
                    var rockLocation = (new Vector3(Rng.NextFloat(-1,1), 0, Rng.NextFloat(-1,1))).normalized * Rng.NextFloat(-10,10);
                    while (ClosestDistanceTo(rockLocation, rockLocations) < 5f)
                        rockLocation = (new Vector3(Rng.NextFloat(-1,1), 0, Rng.NextFloat(-1,1))).normalized * Rng.NextFloat(-10,10);
                    rockLocations.Add(rockLocation);

                    rock.transform.localPosition = rockLocation;
                    var scale = Rng.NextFloat(1, 3);
                    rock.transform.localScale = new Vector3(scale, scale, scale);
                    rock.transform.rotation = Quaternion.Euler(Rng.NextFloat(0,360), Rng.NextFloat(0,360), Rng.NextFloat(0,360));
                }
            }

            for (int i = 0; i < smallAsteroidFields; i++)
            {
                var asteroidField = PlaceAsteroidField();
                AsteroidFields[i+largeAsteroidFields] = new AsteroidField(asteroidField, 8);
                var ring = new GameObject("Small Asteroid Field");
                ring.transform.position = asteroidField;
                var arc = ArcRenderer.NewArc(ring.transform, 8, 0.25f, 0, Mathf.PI * 2, 32, Color.white);
                arc.transform.position = arc.transform.position + new Vector3(0, -5, 0);

                var rockLocations = new List<Vector3>();
                for (int j = 0; j < Rng.NextInt(3,6); j++)
                {
                    var asteroid = Asteroids[Rng.NextInt(0, Asteroids.Length-1)];
                    var rock = Instantiate(asteroid, ring.transform);

                    var rockLocation = (new Vector3(Rng.NextFloat(-1,1), 0, Rng.NextFloat(-1,1))).normalized * Rng.NextFloat(-6.5f,6.5f);
                    while (ClosestDistanceTo(rockLocation, rockLocations) < 2.5f)
                        rockLocation = (new Vector3(Rng.NextFloat(-1,1), 0, Rng.NextFloat(-1,1))).normalized * Rng.NextFloat(-6.5f,6.5f);
                    rockLocations.Add(rockLocation);
                    
                    rock.transform.localPosition = rockLocation;
                    var scale = Rng.NextFloat(0.5f, 2);
                    rock.transform.localScale = new Vector3(scale, scale, scale);
                    rock.transform.rotation = Quaternion.Euler(Rng.NextFloat(0,360), Rng.NextFloat(0,360), Rng.NextFloat(0,360));
                }
            }
        }

        private Tooltip _tooltip;
        public bool TooltipEnabled = true;
        private LoginScreen _loginScreen;
        private NewGameScreen _newGameScreen;
        private JoinGameScreen _joinGameScreen;

        private void SetTooltip()
        {
            if (_tooltip != null && (!TooltipEnabled || MouseSelection == null))
            {
                Destroy(_tooltip.gameObject);
                _tooltip = null;
            }
            else if (TooltipEnabled && _tooltip == null && MouseSelection != null)
            {
                var ship = MouseSelection.GetComponent<Ship>();
                if (ship == null) return;
                _tooltip = Tooltip.Create(UiCanvas, ship.Describe());
            }
        }

        public bool ValidShipPosition(Vector3 position)
        {
            return Players.Sum(player =>
                       player.Fleet.Sum(ship =>
                           ship.Deployed && (ship.transform.position - position).magnitude < 5 ? 1 : 0)) == 0;
        }

        private void HandleCameraMovement()
        {
            var mousePos = Input.mousePosition;

            var location = CameraOperator.Location;
            var direction = CameraOperator.Direction;
            var zoom = CameraOperator.Zoom;

            if (Input.GetMouseButton(1))
            {
                if (_lastRightMousePos != null)
                {
                    var rightPos = (Vector3) _lastRightMousePos;
                    direction = ((direction + (mousePos.x - rightPos.x) * 0.25f) % 360 + 360) % 360;
                }

                _lastRightMousePos = mousePos;
            }

            if (Input.GetMouseButtonUp(1)) _lastRightMousePos = null;

            if (Input.GetMouseButton(2))
            {
                if (_lastMiddleMousePos != null)
                {
                    var middlePos = (Vector3) _lastMiddleMousePos;
                    float dx = -(mousePos.x - middlePos.x) * 0.1f,
                        dy = -(mousePos.y - middlePos.y) * 0.1f;
                    /* x' = x cos f - y sin f
                       y' = y cos f + x sin f */
                    var rot = -CameraOperator.DirectionRadians;
                    location += new Vector3(dx * Mathf.Cos(rot) - dy * Mathf.Sin(rot), 0,
                                    dy * Mathf.Cos(rot) + dx * Mathf.Sin(rot)) * zoom;
                }

                _lastMiddleMousePos = mousePos;
            }

            if (Input.GetMouseButtonUp(2)) _lastMiddleMousePos = null;

            var scroll = Input.GetAxis("Mouse ScrollWheel");
            zoom = Mathf.Clamp(zoom - scroll * 100 * Time.deltaTime, 0.5f, 3.5f);

            CameraOperator.SetCameraPosition(location, direction, zoom);
            CameraOperator.EnforceCameraBoundaries(PlayArea);
        }
    }
}