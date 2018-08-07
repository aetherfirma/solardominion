using System;
using System.Collections.Generic;
using System.Linq;
using Logic.Display;
using Logic.Gameplay.Players;
using Logic.Gameplay.Ships;
using Logic.Maths;
using Logic.Network;
using Logic.Ui;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Logic.Gameplay.Rules
{
    public class Referee : MonoBehaviour
    {
        public Material[] Skyboxes;
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
        public GameObject SelectionRing;
        public GameObject RangeRings;

        private Vector3? _lastRightMousePos, _lastMiddleMousePos;

        public Plane PlaySurface;

        private Text _upperDisplayText;
        public Button LowerButton;
        public RectTransform SelectionPanel;
        public RectTransform FlashedMessageElement;
        public RectTransform StartScreen;
        public RectTransform FactionSelection;
        public RectTransform LowerBar;
        public Sprite ButtonSprite;
        public Font StandardFont;
        public MessageTooltip Tooltip;
        public string ServerUrl;

        private ListBuilder _listBuilder;
        private SetupHandler _setupHandler;
        private StartScreen _startScreen;
        private GameplayHandler _gameplayHandler;

        public GameResponse CurrentGameState;
        public float LastNetworkUpdate;
        public float UpdateInterval = 5;

        public Vector3 MouseLocation;
        public GameObject MouseSelection;

        public int LastObservedInstruction;
        
        private GameObject _gameGrid;

        public void SetGameState(GameResponse response)
        {
            CurrentGameState = response;
            LastNetworkUpdate = Time.time;
        }

        public GameResponse UpdateGameState(bool force = false)
        {
            if (force || Time.time - LastNetworkUpdate > UpdateInterval)
            {
                SimpleRequest.Get(
                    ServerUrl + "/game/" + GameUuid,
                    www => SetGameState(GameResponse.FromJson(www.downloadHandler.text)),
                    www => FlashMessage("There was a server error (" + www.responseCode + ")\n" + www.error),
                    www => FlashMessage("There was a network error\n" + www.error)
                );
            }

            return CurrentGameState;
        }

        private Queue<FlashedMessage> _flashedMessages = new Queue<FlashedMessage>();

        public CameraOperator CameraOperator;

        private void Awake()
        {
//            var grid = GetComponentInChildren<GameGrid>();
//            grid.Size = PlayArea;
//            grid.Gradiation = 5;

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

            _startScreen = new StartScreen(this);
            _listBuilder = new ListBuilder(this);
            _setupHandler = new SetupHandler(this);
            _gameplayHandler = new GameplayHandler(this);

            FindUiElements();

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
            _upperDisplayText.text = s;
        }

        private void FindUiElements()
        {
            _upperDisplayText = UiCanvas.Find("Upper Display Text").GetComponent<Text>();
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

            GameResponse state;
            switch (Phase)
            {
                case GamePhase.GameCreation:
                    _startScreen.Update();
                    break;
                case GamePhase.PlayerSelection:
                    _upperDisplayText.text = "Waiting on other players";
                    state = UpdateGameState();
                    if (state.players.Length == state.no_players)
                    {
                        Players = CreatePlayers();
                        Phase = GamePhase.ListBuilding;
                    }
                    break;
                case GamePhase.ListBuilding:
                    _listBuilder.Update();
                    break;
                case GamePhase.Waiting:
                    _upperDisplayText.text = "Waiting on other players";
                    UpdateGameState();
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

        private Tooltip _tooltip;
        public bool TooltipEnabled = true;

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