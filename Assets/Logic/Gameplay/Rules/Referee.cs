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

namespace Logic.Gameplay.Rules
{
    public class Referee : MonoBehaviour
    {
        public Camera Camera;
        public RectTransform UiCanvas;
        public ShipArray[] Ships;
        public Player[] Players;
        public GamePhase Phase;
        public int CurrentPlayer;
        public int PlayArea = 120;
        public string Seed;
        public WellRng Rng;
        public int PointsLimit;
        public GameObject SelectionRing;

        private Vector3? _lastRightMousePos, _lastMiddleMousePos;

        public Plane PlaySurface;

        private Text _upperDisplayText;
        public Button LowerButton;
        public RectTransform SelectionPanel;
        public RectTransform FlashedMessageElement;
        public RectTransform StartScreen;
        public string ServerUrl;

        private ListBuilder _listBuilder;
        private SetupHandler _setupHandler;
        private StartScreen _startScreen;

        public GameResponse CurrentGameState;
        public float LastNetworkUpdate;
        public float UpdateInterval = 5;

        public void SetGameState(GameResponse response)
        {
            CurrentGameState = response;
            LastNetworkUpdate = Time.time;
        }

        public GameResponse UpdateGameState()
        {
            if (Time.time - LastNetworkUpdate > UpdateInterval)
            {
                var www = UnityWebRequest.Get(ServerUrl + "/game/" + GameUuid);
                www.SendWebRequest();

                while (!www.isDone) ;

                if (www.isNetworkError)
                {
                    FlashMessage("There was a network error\n" + www.error);
                }
                else if (www.isHttpError)
                {
                    FlashMessage("There was a server error (" + www.responseCode + ")\n" + www.error);
                }
                else
                {
                    SetGameState(GameResponse.FromJson(www.downloadHandler.text));
                }
            }
            return CurrentGameState;
        }

        private Queue<FlashedMessage> _flashedMessages = new Queue<FlashedMessage>();

        public CameraOperator CameraOperator;

        private void Awake()
        {
            var grid = GetComponentInChildren<GameGrid>();
            grid.Size = PlayArea;
            grid.Gradiation = 5;

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

            Players = new[]
            {
                new Player(PlayerType.Local)
            };
            FindUiElements();
        }
//
//        private string Serialize()
//        {
//            
//        }

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

        private void Update()
        {
            HandleCameraMovement();
            CameraOperator.UpdateCamera();

            UpdateFlashedMessages();

            switch (Phase)
            {
                case GamePhase.GameCreation:
                    _startScreen.Update();
                    break;
                case GamePhase.PlayerSelection:
                    _upperDisplayText.text = "Waiting on other players";
                    var state = UpdateGameState();
                    if (state.players.Length == state.no_players) Phase = GamePhase.ListBuilding;
                    break;
                case GamePhase.ListBuilding:
                    _listBuilder.Update();
                    break;
                case GamePhase.Setup:
                    _setupHandler.Update();
                    break;
                case GamePhase.Play:
                    _upperDisplayText.text = "PLAY THE GAME";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
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