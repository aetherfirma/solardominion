using System;
using System.Collections.Generic;
using System.Linq;
using Logic.Display;
using Logic.Gameplay.Players;
using Logic.Gameplay.Ships;
using Logic.Network;
using Logic.Ui;
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

        private void SetupInitiativeStep()
        {
            _shipsInInitiativeStep = DetermineShipsThatCanAct();

            if (_shipsInInitiativeStep.Count == 0) return;
            if (_shipsInInitiativeStep.Count == 1)
            {
                _initativePhaseStartingPlayer = _shipsInInitiativeStep.Keys.First();
                return;
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
                return;
            }

            if (playersWithFewestShips.All(player => _playOrder.Contains(player.Number)))
            {
                _initativePhaseStartingPlayer =
                    playersWithFewestShips.Single(player => player.Number == _playOrder.First());
                return;
            }

            _initativePhaseStartingPlayer = playersWithFewestShips.Where(player => !_playOrder.Contains(player.Number))
                .Random(_referee.Rng);
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
                    MovementPhase();
                    break;
                case TurnPhase.Action:
                    ActionPhase();
                    break;
                case TurnPhase.Cleanup:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private Ship _target;

        private void ActionPhase()
        {
            GameResponse state;
            if (UpdateStateAndUpdateInitiative(out state)) return;

            if (_currentPlayer.Number != _referee.LocalPlayer)
            {
                _referee.DisplayUpperText(string.Format("Waiting for {0:} to play", _currentPlayer.Faction));

                if (_referee.LastObservedInstruction < state.turns.Count)
                {
                    for (var i = _referee.LastObservedInstruction; i < state.turns.Count; i++)
                    {
                        var turn = state.turns[i];
                        if (turn.player != _referee.PlayerUuid && turn.action == TurnType.ActionEnd)
                        {
                            var ship = _currentPlayer.Fleet.Single(s => s.ShipUuid == turn.ship);
                            _referee.FlashMessage(string.Format("Turn over for {0:}", ship.Name()));

                            // Obey Action

                            RemoveShipFromStep(ship);
                            NextPlayer();
                        } 
                        else if (turn.player != _referee.PlayerUuid && turn.action == TurnType.ActionChallenge)
                        {
                            _referee.FlashMessage("Need to respond");
                            var firingShip = _currentPlayer.Fleet.Single(s => s.ShipUuid == turn.ship);
                            var targetShip = _currentPlayer.Fleet.Single(s => s.ShipUuid == turn.ship);
                            
                        }

                        _referee.LastObservedInstruction = i + 1;
                    }
                }
            }
            else
            {
                _referee.DisplayUpperText("");

                if (_selection == null)
                {
                    CircleSelectableShips();

                    if (Input.GetMouseButtonUp(0) && _referee.MouseSelection)
                    {
                        var ship = _referee.MouseSelection.GetComponent<Ship>();
                        if (ship != null && _shipsInInitiativeStep[_currentPlayer].Contains(ship))
                        {
                            ClearArcs();
                            _selection = ship;
                            _arcs.Add(ArcRenderer.NewArc(ship.transform, 7, 0.5f, 0, Mathf.PI * 2, 64, Color.red));
                            DrawSystemsDisplay(
                                ship,
                                (index, system, subsystem) =>
                                    system.Type == SystemType.Weapon || system.Type == SystemType.Hangar ||
                                    (system.Type == SystemType.Composite &&
                                     (system.SubSystems[subsystem].Type == SystemType.Weapon ||
                                      system.SubSystems[subsystem].Type == SystemType.Hangar)),
                                (index, system, subsystem, image) =>
                                {
                                    if (system.Type == SystemType.Composite) system = system.SubSystems[subsystem];
                                    
                                    switch (system.Type)
                                    {
                                        case SystemType.Weapon:
                                            if (_selectedWeapon == null)
                                            {
                                                _selectedWeapon = system;
                                                _selectedSystems.Add(index);
                                                image.color = Color.yellow;
                                            }
                                            else
                                            {
                                                if (_selectedSystems.Contains(index))
                                                {
                                                    _selectedSystems.Remove(index);
                                                    image.color = Color.white;
                                                    if (_selectedSystems.Count == 0) _selectedWeapon = null;
                                                }
                                                else
                                                {
                                                    if (_selectedWeapon == system)
                                                    {
                                                        image.color = Color.yellow;
                                                        _selectedSystems.Add(index);
                                                    }
                                                    else
                                                    {
                                                        _referee.FlashMessage("Cannot combine different weapon types", 10);
                                                    }
                                                }
                                            }
                                            break;
                                        case SystemType.Hangar:
                                            _referee.FlashMessage("Would deploy a ship");
                                            break;
                                    }
                                },
                                selectedShip =>
                                {
                                    _lowerBar.transform.DestroyAllChildren(obj => obj.GetComponent<Image>() != null);
                                    _selection = null;
                                    RemoveShipFromStep(selectedShip);
                                    BroadcastEndOfAction(selectedShip);
                                    ClearArcs();
                                    _lowerBar.transform.Find("Text").GetComponent<Text>().text = "";

                                    NextPlayer();
                                }
                            );
                        }
                    }
                }
                else
                {
                    if (_target)
                    {
                        _referee.DisplayUpperText(string.Format("Waiting for {0:} to respond", _target.Player.Faction));
                        
                        if (_referee.LastObservedInstruction < state.turns.Count)
                        {
                            for (var i = _referee.LastObservedInstruction; i < state.turns.Count; i++)
                            {
                                var turn = state.turns[i];
                                if (turn.action == TurnType.ActionResponse)
                                {
                                    var ship = _currentPlayer.Fleet.Single(s => s.ShipUuid == turn.ship);
                                    _referee.FlashMessage(string.Format("Just recieved reply for {0:}", ship.Name()));
                                    _referee.DisplayUpperText("");

                                    // Obey Action

                                    RemoveShipFromStep(ship);
                                }

                                _referee.LastObservedInstruction = i + 1;
                                NextPlayer();
                            }
                        }

                    }
                    else if (Input.GetMouseButtonUp(0) && _referee.MouseSelection && _selectedWeapon != null)
                    {
                        var ship = _referee.MouseSelection.GetComponent<Ship>();
                        if (ship != null && ship.Player != _currentPlayer)
                        {
                            if (IsInRange(_selection, _selectedWeapon, ship))
                            {
                                _target = ship;
                                _lowerBar.transform.DestroyAllChildren(obj => obj.GetComponent<Image>() != null);
                                _arcs.Add(ArcRenderer.NewArc(ship.transform, 7, 0.5f, 0, Mathf.PI * 2, 64, Color.red));
                                BroadcastWeaponFiring(_selection, _selectedWeapon, _selectedSystems, _target);
                                _referee.DisplayUpperText("Waiting on response");
                            }
                            else
                            {
                                _referee.FlashMessage(string.Format("Cannot fire {0:} at {1:}, out of range.", _selectedWeapon.name, ship.Name()));
                            }
                        }
                    }
                }
            }
        }

        private void BroadcastWeaponFiring(Ship ship, ShipSystem selectedWeapon, List<int> selectedSystems, Ship target)
        {
            var modifier = GetRangeModifier(ship, selectedWeapon, target);

            var turn = new Turn
            {
                action = TurnType.ActionChallenge,
                player = ship.Player.Uuid,
                ship = ship.ShipUuid,
                target = target.ShipUuid,
                weapon = selectedWeapon.name,
                damage =  selectedWeapon.Damage,
                shots = selectedSystems.Count * selectedWeapon.Shots,
                defence_modifier = modifier
            };

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
                www => _referee.FlashMessage("There was a server error (" + www.responseCode + ") creating a game\n" +
                                             www.error),
                www => _referee.FlashMessage("There was a network error creating a game\n" + www.error)
            );
        }

        private void BroadcastEndOfAction(Ship ship)
        {
            var turn = new Turn
            {
                action = TurnType.ActionEnd,
                player = ship.Player.Uuid,
                ship = ship.ShipUuid
            };

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
                www => _referee.FlashMessage("There was a server error (" + www.responseCode + ") creating a game\n" +
                                             www.error),
                www => _referee.FlashMessage("There was a network error creating a game\n" + www.error)
            );
        }

        private static int GetRangeModifier(Ship ship, ShipSystem selectedWeapon, Ship target)
        {
            var range = (ship.transform.position - target.transform.position).magnitude;
            if (range < selectedWeapon.ShortRange) return selectedWeapon.ShortModifier;
            if (range < selectedWeapon.MediumRange) return selectedWeapon.MediumModifier;
            return selectedWeapon.LongModifier;
        }

        private static bool IsInRange(Ship ship, ShipSystem selectedWeapon, Ship target)
        {
            var range = (ship.transform.position - target.transform.position).magnitude;
            return range < selectedWeapon.LongRange;
        }

        private void CircleSelectableShips()
        {
            if (_arcs.Count != 0) return;
            foreach (var ship in _shipsInInitiativeStep[_currentPlayer])
            {
                _arcs.Add(ArcRenderer.NewArc(ship.transform, 7, 0.5f, 0, Mathf.PI * 2, 64, Color.white));
            }
        }

        private bool UpdateStateAndUpdateInitiative(out GameResponse state)
        {
            state = _referee.UpdateGameState();

            while (_shipsInInitiativeStep == null || _shipsInInitiativeStep.Count == 0)
            {
                _currentInitiativeStep++;
                if (_currentInitiativeStep == 13)
                {
                    IncrementPhase();
                    return true;
                }

                SetupInitiativeStep();
                _currentPlayer = _initativePhaseStartingPlayer;
            }

            return false;
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

        private void ClearArcs()
        {
            foreach (var arc in _arcs)
            {
                Object.Destroy(arc);
            }

            _arcs.Clear();
        }

        private void DrawSystemsDisplay(Ship ship, Func<int, ShipSystem, int, bool> isInteractable,
            Action<int, ShipSystem, int, Image> onClick, Action<Ship> onComplete)
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
                var used = ship.Used[i];
                var subsystem = ship.Subsystem[i];

                var icon = new GameObject("system icon", typeof(Image), typeof(Button), typeof(RectTransform));
                icon.SetAsChild(_lowerBar);

                icon.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

                rectTransform = icon.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(-55 / 2f * systemsLength + 55 * i, 0);

                var image = icon.GetComponent<Image>();
                if (system.Type == SystemType.Composite && subsystem > -1)
                {
                    var texture2D = system.SubSystems[subsystem].Icon;
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
                var interactable = !damaged && !used && isInteractable(i, system, subsystem);

                button.interactable = interactable;

                var card = button.gameObject.AddComponent<CardTooltip>();
                card.Image = system.CardImages[(int) ship.Player.Faction];
                card.ParentTransform = _referee.UiCanvas;

                var unmodifiedImage = image;
                var unmodifiedIndex = i;
                var unmodifiedSubsystem = subsystem;
                if (interactable) button.onClick.AddListener(() => onClick(unmodifiedIndex, system, unmodifiedSubsystem, unmodifiedImage));

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
            button.onClick.AddListener(() => { onComplete(ship); });

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
                    turn.system_status[i] = ship.Subsystem[i];
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
                www => _referee.FlashMessage("There was a server error (" + www.responseCode + ") creating a game\n" +
                                             www.error),
                www => _referee.FlashMessage("There was a network error creating a game\n" + www.error)
            );
        }

        private ShipSystem _selectedWeapon;
        private List<int> _selectedSystems = new List<int>();

        private void CommandPhase()
        {
            GameResponse state;
            if (UpdateStateAndUpdateInitiative(out state)) return;

            if (_currentPlayer.Number != _referee.LocalPlayer)
            {
                _referee.DisplayUpperText(string.Format("Waiting for {0:} to play", _currentPlayer.Faction));

                if (_referee.LastObservedInstruction < state.turns.Count)
                {
                    for (var i = _referee.LastObservedInstruction; i < state.turns.Count; i++)
                    {
                        var turn = state.turns[i];
                        if (turn.player != _referee.PlayerUuid && turn.action == TurnType.CommandPhase)
                        {
                            var ship = _currentPlayer.Fleet.Single(s => s.ShipUuid == turn.ship);
                            _referee.FlashMessage(string.Format("Just recieved order for {0:}", ship.Name()));

                            if (turn.order != null)
                            {
                                ship.Order = (Order) turn.order;
                            }

                            foreach (var system in turn.system_status.Keys)
                            {
                                ship.Subsystem[system] = turn.system_status[system];
                            }

                            ship.CalculateThrust();
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

                if (_selection == null)
                {
                    CircleSelectableShips();

                    if (Input.GetMouseButtonUp(0) && _referee.MouseSelection)
                    {
                        var ship = _referee.MouseSelection.GetComponent<Ship>();
                        if (ship != null && _shipsInInitiativeStep[_currentPlayer].Contains(ship))
                        {
                            ClearArcs();
                            _selection = ship;
                            _arcs.Add(ArcRenderer.NewArc(ship.transform, 7, 0.5f, 0, Mathf.PI * 2, 64, Color.red));
                            DrawSystemsDisplay(
                                ship,
                                (index, system, subsystem) =>
                                    system.Type == SystemType.Command || system.Type == SystemType.Composite,
                                (index, system, subsystem, image) =>
                                {
                                    switch (system.Type)
                                    {
                                        case SystemType.Composite:
                                            ship.Subsystem[index] =
                                                (subsystem + 1) % system.SubSystems.Length;
                                            _referee.FlashMessage(string.Format("Set {0:} to {1:}", system.name,
                                                system.SubSystems[ship.Subsystem[index]].name));
                                            var texture2D = system.SubSystems[ship.Subsystem[index]].Icon;
                                            image.sprite = Sprite.Create(texture2D,
                                                new Rect(0, 0, texture2D.width, texture2D.height),
                                                new Vector2(texture2D.width / 2f, texture2D.height / 2f));
                                            break;
                                        case SystemType.Command:
                                            _referee.FlashMessage("Would add an order");
                                            break;
                                    }
                                },
                                selectedShip =>
                                {
                                    if (selectedShip.Systems.Where((t, i) =>
                                        t.Type == SystemType.Composite && selectedShip.Subsystem[i] == -1).Any())
                                    {
                                        _referee.FlashMessage(
                                            "All composite systems must be set to one of their options");
                                        return;
                                    }

                                    _lowerBar.transform.DestroyAllChildren(obj => obj.GetComponent<Image>() != null);
                                    _selection = null;
                                    selectedShip.CalculateThrust();
                                    RemoveShipFromStep(selectedShip);
                                    BroadcastShipCommandState(selectedShip);
                                    ClearArcs();
                                    _lowerBar.transform.Find("Text").GetComponent<Text>().text = "";

                                    NextPlayer();
                                }
                            );
                        }
                    }
                }
            }
        }

        private void MovementPhase()
        {
            GameResponse state;
            if (UpdateStateAndUpdateInitiative(out state)) return;

            if (_currentPlayer.Number != _referee.LocalPlayer)
            {
                _referee.DisplayUpperText(string.Format("Waiting for {0:} to play", _currentPlayer.Faction));

                if (_referee.LastObservedInstruction < state.turns.Count)
                {
                    for (var i = _referee.LastObservedInstruction; i < state.turns.Count; i++)
                    {
                        var turn = state.turns[i];
                        if (turn.player != _referee.PlayerUuid && turn.action == TurnType.MovementPhase)
                        {
                            var ship = _currentPlayer.Fleet.Single(s => s.ShipUuid == turn.ship);
                            _referee.FlashMessage(string.Format("Just recieved movement for {0:}", ship.Name()));

                            var newPosition = new Vector3(turn.location[0], 0, turn.location[1]);

                            var leadingPoint = ship.transform.position +
                                               ship.transform.rotation * (Vector3.forward * ship.Speed);
                            var distance = newPosition - leadingPoint;
                            var thrust = Mathf.CeilToInt(distance.magnitude / ship.DistancePerThrust());
                            if (thrust > ship.ThrustRemaining)
                            {
                                _referee.FlashMessage("Cannot move here, insufficient thrust");
                                return;
                            }

                            ship.ThrustRemaining -= thrust;
                            var movementDelta = newPosition - ship.transform.position;
                            ship.Speed = Mathf.CeilToInt(movementDelta.magnitude / 5f);
                            ship.transform.position = newPosition;
                            ship.transform.rotation = Quaternion.LookRotation(movementDelta);

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

                if (_selection == null)
                {
                    CircleSelectableShips();

                    if (Input.GetMouseButtonUp(0) && _referee.MouseSelection)
                    {
                        var ship = _referee.MouseSelection.GetComponent<Ship>();
                        if (ship != null && _shipsInInitiativeStep[_currentPlayer].Contains(ship))
                        {
                            ClearArcs();
                            _selection = ship;
                            var arc = ArcRenderer.NewArc(ship.transform, 1, 0.5f, 0, Mathf.PI * 2, 64, Color.red);
                            for (var i = 1; i < Mathf.Min(ship.ThrustRemaining, 5); i++)
                            {
                                var c = 1.2f - 0.2f * i;
                                _arcs.Add(ArcRenderer.NewArc(arc.transform, ship.DistancePerThrust() * i, 0.1f, 0,
                                    Mathf.PI * 2, 64, new Color(c, c, c, c)));
                            }

                            arc.transform.localPosition = Vector3.forward * ship.Speed * 5;
                            _arcs.Add(arc);

                            // TODO: Add a buton to skip movement
                        }
                    }
                }
                else
                {
                    if (Input.GetMouseButtonUp(0))
                    {
                        var leadingPoint = _selection.transform.position +
                                           _selection.transform.rotation * (Vector3.forward * _selection.Speed * 5);
                        var distance = _referee.MouseLocation - leadingPoint;
                        var thrust = Mathf.CeilToInt(distance.magnitude / _selection.DistancePerThrust());
                        if (thrust > _selection.ThrustRemaining)
                        {
                            _referee.FlashMessage("Cannot move here, insufficient thrust");
                            return;
                        }

                        _selection.ThrustRemaining -= thrust;
                        var movementDelta = _referee.MouseLocation - _selection.transform.position;
                        _selection.Speed = Mathf.CeilToInt(movementDelta.magnitude / 5f);
                        _selection.transform.position = _referee.MouseLocation;
                        _selection.transform.rotation = Quaternion.LookRotation(movementDelta);
                        BroadcastMovement(_selection);
                        RemoveShipFromStep(_selection);
                        _selection = null;
                        ClearArcs();
                        NextPlayer();
                    }
                }
            }
        }

        private void BroadcastMovement(Ship ship)
        {
            var turn = new Turn
            {
                action = TurnType.MovementPhase,
                player = ship.Player.Uuid,
                ship = ship.ShipUuid,
                location = new[] {ship.transform.position.x, ship.transform.position.z},
                rotation = ship.transform.rotation.eulerAngles.y,
                speed = ship.Speed
            };

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
                www => _referee.FlashMessage("There was a server error (" + www.responseCode + ") creating a game\n" +
                                             www.error),
                www => _referee.FlashMessage("There was a network error creating a game\n" + www.error)
            );
        }
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