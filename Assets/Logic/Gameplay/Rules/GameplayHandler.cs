using System;
using System.Collections.Generic;
using System.Linq;
using Logic.Display;
using Logic.Gameplay.Players;
using Logic.Gameplay.Ships;
using Logic.Network;
using Logic.Utilities;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Logic.Gameplay.Rules
{
    internal class GameplayHandler
    {
        private readonly Referee _referee;
        private TurnPhase _phase = TurnPhase.Initiative;
        private int _currentInitiativeStep;
        private OrderedSet<int> _playOrder;
        private Dictionary<Player, List<Ship>> _shipsInInitiativeStep;
        private Player _initativePhaseStartingPlayer;
        private Ship _selection;
        private List<GameObject> _arcs = new List<GameObject>();
        private RectTransform _lowerBar;

        public GameplayHandler(Referee referee)
        {
            _referee = referee;
            _playOrder = new OrderedSet<int>();
        }

        private Dictionary<Player, List<Ship>> DetermineShipsThatCanAct()
        {
            var playersAndShips = new DictionaryWithDefault<Player, List<Ship>>(() => new List<Ship>());

            foreach (var player in _referee.Players)
            {
                foreach (var ship in player.Fleet)
                {
                    if (ship.Alive && ship.Initiative == _currentInitiativeStep) playersAndShips[player].Add(ship);
                }
            }

            return playersAndShips;
        }

        private void RemoveShipFromStep(Ship ship)
        {
            _shipsInInitiativeStep[ship.Player].Remove(ship);
            if (_shipsInInitiativeStep[ship.Player].Count == 0) _shipsInInitiativeStep.Remove(ship.Player);
        }

        private void NextPlayer()
        {
            var currentIndex = _currentPlayer.Number;
            var n = 1;
            currentIndex = (currentIndex + 1) % _referee.Players.Length;
            while (n < _referee.Players.Length && !_shipsInInitiativeStep.ContainsKey(_referee.Players[currentIndex]))
            {
                currentIndex = (currentIndex + 1) % _referee.Players.Length;
                n++;
            }
            _currentPlayer = _referee.Players[currentIndex];
        }

        private InitiativeStepOutcome SetupInitiativeStep()
        {
            _shipsInInitiativeStep = DetermineShipsThatCanAct();

            if (_shipsInInitiativeStep.Count == 0) return InitiativeStepOutcome.NextInitiativeStep;
            if (_shipsInInitiativeStep.Count == 1)
            {
                _initativePhaseStartingPlayer = _shipsInInitiativeStep.Keys.First();
                return InitiativeStepOutcome.ShipsInStep;
            }

            var playersWithFewestShips = new HashSet<Player>();
            var noShips = int.MaxValue;
            foreach (var player in _shipsInInitiativeStep.Keys)
            {
                if (_shipsInInitiativeStep[player].Count < noShips)
                {
                    noShips = _shipsInInitiativeStep[player].Count;
                    playersWithFewestShips = new HashSet<Player> {player};
                }
                else if (_shipsInInitiativeStep[player].Count == noShips)
                {
                    playersWithFewestShips.Add(player);
                }
            }

            if (playersWithFewestShips.Count == 1)
            {
                _initativePhaseStartingPlayer = playersWithFewestShips.First();
                return InitiativeStepOutcome.ShipsInStep;
            }

            if (playersWithFewestShips.All(player => _playOrder.Contains(player.Number)))
            {
                _initativePhaseStartingPlayer =
                    playersWithFewestShips.Single(player => player.Number == _playOrder.First());
                return InitiativeStepOutcome.ShipsInStep;
            }

            _initativePhaseStartingPlayer = playersWithFewestShips.Where(player => !_playOrder.Contains(player.Number))
                .Random(_referee.Rng);
            return InitiativeStepOutcome.ShipsInStep;
        }

        private void IncrementPhase()
        {
            _currentInitiativeStep = 2;
            switch (_phase)
            {
                case TurnPhase.Initiative:
                    _phase = TurnPhase.Command;
                    break;
                case TurnPhase.Command:
                    _phase = TurnPhase.Movement;
                    break;
                case TurnPhase.Movement:
                    _phase = TurnPhase.Action;
                    break;
                case TurnPhase.Action:
                    _phase = TurnPhase.Cleanup;
                    break;
                case TurnPhase.Cleanup:
                    _phase = TurnPhase.Initiative;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Update()
        {
            if (_lowerBar == null) _lowerBar = Object.Instantiate(_referee.LowerBar, _referee.UiCanvas);

            switch (_phase)
            {
                case TurnPhase.Initiative:
                    InitiativePhase();
                    IncrementPhase();
                    goto case TurnPhase.Command;
                case TurnPhase.Command:
                    CommandPhase();
                    break;
                case TurnPhase.Movement:
                    _referee.DisplayUpperText("Movement phase");
                    break;
                case TurnPhase.Action:
                    break;
                case TurnPhase.Cleanup:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void InitiativePhase()
        {
            foreach (var player in _referee.Players)
            {
                foreach (var ship in player.Fleet.Where(ship => ship.Alive))
                {
                    ship.SetInitiative(_referee.Rng);
                }
            }
        }

        private Player _currentPlayer;
        private Ship _commandSelection;

        private void ClearArcs()
        {
            foreach (var arc in _arcs)
            {
                Object.Destroy(arc);
            }

            _arcs.Clear();
        }

        private void DrawSystemsDisplay(Ship ship)
        {
            _lowerBar.transform.DestroyAllChildren(obj => obj.GetComponent<Image>() != null);
            _lowerBar.transform.Find("Text").GetComponent<Text>().text = ship.Name();

            var systemsLength = ship.Systems.Length;
            RectTransform rectTransform;
            Button button;
            for (var i = 0; i < systemsLength; i++)
            {
                var system = ship.Systems[i];
                var damaged = ship.Damage[i];

                var icon = new GameObject("system icon", typeof(Image), typeof(Button), typeof(RectTransform));
                icon.SetAsChild(_lowerBar);

                icon.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

                rectTransform = icon.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(-55 / 2f * systemsLength + 55 * i, 0);

                var image = icon.GetComponent<Image>();
                if (system.Type == SystemType.Composite && system.ChosenSubsystem > -1)
                {
                    var texture2D = system.SubSystems[system.ChosenSubsystem].Icon;
                    image.sprite = Sprite.Create(texture2D,
                        new Rect(0, 0, texture2D.width, texture2D.height),
                        new Vector2(texture2D.width / 2f, texture2D.height / 2f));
                }
                else
                {
                    image.sprite = Sprite.Create(system.Icon,
                        new Rect(0, 0, system.Icon.width, system.Icon.height),
                        new Vector2(system.Icon.width / 2f, system.Icon.height / 2f));
                }

                button = icon.GetComponent<Button>();

                button.targetGraphic = image;
                var interactable =
                    !damaged && (system.Type == SystemType.Command || system.Type == SystemType.Composite);
                button.interactable = interactable;

                if (interactable)
                {
                    switch (system.Type)
                    {
                        case SystemType.Composite:
                            button.onClick.AddListener(() =>
                            {
                                system.ChosenSubsystem = (system.ChosenSubsystem + 1) % system.SubSystems.Length;
                                _referee.FlashMessage(string.Format("Set {0:} to {1:}", system.name,
                                    system.SubSystems[system.ChosenSubsystem].name));
                                var texture2D = system.SubSystems[system.ChosenSubsystem].Icon;
                                image.sprite = Sprite.Create(texture2D,
                                    new Rect(0, 0, texture2D.width, texture2D.height),
                                    new Vector2(texture2D.width / 2f, texture2D.height / 2f));
                            });
                            break;
                        case SystemType.Command:
                            button.onClick.AddListener(() => { _referee.FlashMessage("Would add an order"); });
                            break;
                    }
                }

                image.color = interactable
                    ? Color.white
                    : (damaged ? new Color(0.5f, 0, 0, 0.5f) : new Color(0.5f, 0.5f, 0.5f, 0.5f));
            }

            var done = new GameObject("done button", typeof(Image), typeof(Button), typeof(RectTransform));
            done.SetAsChild(_lowerBar);

            rectTransform = done.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(0, -55);
            rectTransform.sizeDelta = new Vector2(180, 35);

            var iconImage = done.GetComponent<Image>();
            iconImage.sprite = _referee.ButtonSprite;

            button = done.GetComponent<Button>();
            button.image = iconImage;
            button.onClick.AddListener(() =>
            {
                if (ship.Systems.Where(system => system.Type == SystemType.Composite)
                    .Any(system => system.ChosenSubsystem == -1))
                {
                    _referee.FlashMessage("All composite systems must be set to one of their options");
                    return;
                }
                _lowerBar.transform.DestroyAllChildren(obj => obj.GetComponent<Image>() != null);
                _commandSelection = null;
                RemoveShipFromStep(ship);
                BroadcastShipCommandState(ship);
                ClearArcs();
                _lowerBar.transform.Find("Text").GetComponent<Text>().text = "";
                
                NextPlayer();
            });

            var textObj = new GameObject("Text", typeof(Text), typeof(RectTransform));
            textObj.SetAsChild(done.transform);

            var component = textObj.GetComponent<RectTransform>();
            component.sizeDelta = new Vector2(180, 35);
            component.anchoredPosition = Vector2.zero;

            var text = textObj.GetComponent<Text>();
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = "Finish Phase";
            text.font = _referee.StandardFont;
            text.fontSize = 14;
        }

        private void BroadcastShipCommandState(Ship ship)
        {
            var turn = new Turn
            {
                action = TurnType.CommandPhase,
                player = ship.Player.Uuid,
                ship = ship.ShipUuid,
                system_status = new Dictionary<int, int>()
            };

            if (ship.UnderOrders) turn.order = ship.Order;

            for (var i = 0; i < ship.Systems.Length; i++)
            {
                if (ship.Systems[i].Type == SystemType.Composite)
                {
                    turn.system_status[i] = ship.Systems[i].ChosenSubsystem;
                }
            }

            var wwwForm = new WWWForm();
            wwwForm.AddField("player", _referee.PlayerUuid);
            wwwForm.AddField("turn", StringSerializationAPI.Serialize<Turn>(turn));

            SimpleRequest.Post(
                _referee.ServerUrl + "/game/" + _referee.GameUuid + "/turn", wwwForm,
                www =>
                {
                    var response = GameResponse.FromJson(www.downloadHandler.text);
                    _referee.LastObservedInstruction = response.turns.Count;
                    _referee.SetGameState(response);
                },
                www => _referee.FlashMessage("There was a server error (" + www.responseCode +  ") creating a game\n"+www.error),
                www => _referee.FlashMessage("There was a network error creating a game\n"+www.error)
            );
        }

        private void CommandPhase()
        {
            var state = _referee.UpdateGameState();

            while (_shipsInInitiativeStep == null || _shipsInInitiativeStep.Count == 0)
            {
                _currentInitiativeStep++;
                if (_currentInitiativeStep == 13)
                {
                    _phase = TurnPhase.Movement;
                    return;
                }
                SetupInitiativeStep();
                _currentPlayer = _initativePhaseStartingPlayer;
            }

            if (_currentPlayer.Number != _referee.LocalPlayer)
            {
                _referee.DisplayUpperText(string.Format("Waiting for {0:} to play", _currentPlayer.Faction));
                
                if (_referee.LastObservedInstruction < state.turns.Count)
                {
                    for (var i = _referee.LastObservedInstruction; i < state.turns.Count; i++)
                    {
                        var turn = state.turns[i];
                        if (turn.player != _referee.PlayerUuid || turn.action != TurnType.CommandPhase)
                        {
                            var ship = _currentPlayer.Fleet.Single(s => s.ShipUuid == turn.ship);
                            _referee.FlashMessage(string.Format("Just recieved order for {0:}", ship.Name()));

                            if (turn.order != null)
                            {
                                ship.Order = (Order) turn.order;
                            }

                            foreach (var system in turn.system_status.Keys)
                            {
                                ship.Systems[system].ChosenSubsystem = turn.system_status[system];
                            }
                        
                            RemoveShipFromStep(ship);
                        }
                        _referee.LastObservedInstruction = i + 1;
                        NextPlayer();
                    }
                }

            }
            else
            {
                _referee.DisplayUpperText("");

                if (_commandSelection == null)
                {
                    if (_arcs.Count == 0)
                    {
                        foreach (var ship in _shipsInInitiativeStep[_currentPlayer])
                        {
                            _arcs.Add(ArcRenderer.NewArc(ship.transform, 7, 0.5f, 0, Mathf.PI * 2, 64, Color.white));
                        }
                    }

                    if (Input.GetMouseButtonUp(0) && _referee.MouseSelection)
                    {
                        var ship = _referee.MouseSelection.GetComponent<Ship>();
                        if (ship != null && _shipsInInitiativeStep[_currentPlayer].Contains(ship))
                        {
                            ClearArcs();
                            _commandSelection = ship;
                            _arcs.Add(ArcRenderer.NewArc(ship.transform, 7, 0.5f, 0, Mathf.PI * 2, 64, Color.red));
                            DrawSystemsDisplay(ship);
                        }
                    }
                }
            }
        }
    }

    internal enum InitiativeStepOutcome
    {
        NextInitiativeStep,
        ShipsInStep
    }

    internal enum TurnPhase
    {
        Initiative,
        Command,
        Movement,
        Action,
        Cleanup
    }
}