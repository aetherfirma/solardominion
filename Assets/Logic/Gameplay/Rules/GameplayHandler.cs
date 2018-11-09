using System;
using System.Collections.Generic;
using System.Linq;
using Logic.Display;
using Logic.Gameplay.Players;
using Logic.Gameplay.Rules.GamePhases;
using Logic.Gameplay.Ships;
using Logic.Network;
using Logic.Ui;
using Logic.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Logic.Gameplay.Rules
{
    public class GameplayHandler
    {
        public readonly Referee Referee;
        private TurnPhase _phase = TurnPhase.Initiative;
        private int _currentInitiativeStep;
        private int _currentTurn = 1;
        private OrderedSet<int> _playOrder;
        public Dictionary<Player, List<Ship>> ShipsInInitiativeStep;
        private Player _initativePhaseStartingPlayer;
        public RectTransform LowerBar;
        public RectTransform TurnCounter;
        private TextMeshProUGUI _turnCounter;

        private List<ShipCard> _shipCards = new List<ShipCard>();
        public bool ShowShipCards = true;
        public Ship SelectedShip;

        private Image _initiativePhaseIndicator,
            _commandPhaseIndicator,
            _movementPhaseIndicator,
            _actionPhaseIndicator,
            _cleanupPhaseIndicator;

        private ActionPhase _actionPhase;
        private MovementPhase _movementPhase;
        private CommandPhase _commandPhase;

        public GameplayHandler(Referee referee)
        {
            Referee = referee;
            _playOrder = new OrderedSet<int>();
            _actionPhase = new ActionPhase(this);
            _movementPhase = new MovementPhase(this);
            _commandPhase = new CommandPhase(this);
        }

        public void DestroyShip(Ship ship)
        {
            if (ship.Initiative == _currentInitiativeStep) RemoveShipFromCurrentStep(ship);
            Object.Instantiate(Referee.ShipDestroyedExplosion, ship.transform.position, Quaternion.identity);
            ship.gameObject.SetActive(false);
        }

        private Dictionary<Player, List<Ship>> DetermineShipsThatCanAct()
        {
            var playersAndShips = new DictionaryWithDefault<Player, List<Ship>>(() => new List<Ship>());

            foreach (var player in Referee.Players)
            {
                foreach (var ship in player.Fleet)
                {
                    if (ship.Alive && ship.Initiative == _currentInitiativeStep) playersAndShips[player].Add(ship);
                }
            }

            return playersAndShips;
        }

        public void RemoveShipFromCurrentStep(Ship ship)
        {
            ShipsInInitiativeStep[ship.Player].Remove(ship);
            if (ShipsInInitiativeStep[ship.Player].Count == 0) ShipsInInitiativeStep.Remove(ship.Player);
        }

        public void NextPlayer()
        {
            var currentIndex = CurrentPlayer.Number;
            var n = 1;
            currentIndex = (currentIndex + 1) % Referee.Players.Length;
            while (n < Referee.Players.Length && !ShipsInInitiativeStep.ContainsKey(Referee.Players[currentIndex]))
            {
                currentIndex = (currentIndex + 1) % Referee.Players.Length;
                n++;
            }

            CurrentPlayer = Referee.Players[currentIndex];
        }

        private void SetupInitiativeStep()
        {
            ShipsInInitiativeStep = DetermineShipsThatCanAct();

            if (ShipsInInitiativeStep.Count == 0) return;
            if (ShipsInInitiativeStep.Count == 1)
            {
                _initativePhaseStartingPlayer = ShipsInInitiativeStep.Keys.First();
                return;
            }

            // TODO: Swap out for player who acted least recently

            var playersWithFewestShips = new HashSet<Player>();
            var noShips = Int32.MaxValue;
            foreach (var player in ShipsInInitiativeStep.Keys)
            {
                if (ShipsInInitiativeStep[player].Count < noShips)
                {
                    noShips = ShipsInInitiativeStep[player].Count;
                    playersWithFewestShips = new HashSet<Player> {player};
                }
                else if (ShipsInInitiativeStep[player].Count == noShips)
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
                .Random(Referee.Rng);
        }

        private void IncrementPhase()
        {
            SelectedShip = null;
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
            if (LowerBar == null) LowerBar = Object.Instantiate(Referee.LowerBar, Referee.UiCanvas);
            if (TurnCounter == null)
            {
                TurnCounter = Object.Instantiate(Referee.TurnIndicator, Referee.UiCanvas);
                FindTurnCounterComponents();
            }

            SetTurnDisplay();
            HandleShipCards();

            switch (_phase)
            {
                case TurnPhase.Initiative:
                    var initiativePhaseOver = InitiativePhase();
                    if (initiativePhaseOver)
                    {
                        IncrementPhase();
                    }

                    break;
                case TurnPhase.Command:
                    _commandPhase.Update();
                    break;
                case TurnPhase.Movement:
                    _movementPhase.Update();
                    break;
                case TurnPhase.Action:
                    _actionPhase.Update();
                    break;
                case TurnPhase.Cleanup:
                    IncrementPhase();
                    _currentTurn++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void HandleShipCards()
        {
            var fleet = Referee.Players[Referee.LocalPlayer].Fleet;
            if (fleet.Length != _shipCards.Count)
            {
                foreach (var card in _shipCards)
                {
                    Object.Destroy(card.gameObject);
                }
                _shipCards.Clear();
                foreach (var ship in fleet)
                {
                    var card = Object.Instantiate(Referee.ShipCard, LowerBar);
                    card.Ship = ship;
                    card.transform.localPosition = Vector3.zero;
                    _shipCards.Add(card);
                }
            }

            const float splay = 100f;
            var basic = splay/2 - splay * (_shipCards.Count - 1);
            for (int i = 0; i < _shipCards.Count; i++)
            {
                var card = _shipCards[i];
                var x = basic + splay * i;
                
                var y = -150;
                if (ShowShipCards == false)
                {
                    y = -300;
                }
                else if (SelectedShip != null)
                {
                    y = card.Ship == SelectedShip ? 50 : -255;
                }
                else if (Referee.MouseSelection != null)
                {
                    var hovership = Referee.MouseSelection.GetComponent<Ship>();
                    if (hovership != null && hovership.Player == Referee.Players[Referee.LocalPlayer])
                    {
                        y = hovership == card.Ship ? -100 : -200;
                    }
                } 
                
                var rot = SelectedShip != null ? 0 : 20;
                
                card.transform.localPosition = Vector3.Lerp(card.transform.localPosition, new Vector2(x, y), Time.deltaTime * 2);
                card.transform.localRotation = Quaternion.Lerp(card.transform.localRotation, Quaternion.Euler(0,0,rot), Time.deltaTime * 2);
            }
        }

        private void FindTurnCounterComponents()
        {
            _turnCounter = TurnCounter.Find("turn counter").GetComponent<TextMeshProUGUI>();
            _initiativePhaseIndicator = TurnCounter.Find("initiative").GetComponent<Image>();
            _commandPhaseIndicator = TurnCounter.Find("command").GetComponent<Image>();
            _movementPhaseIndicator = TurnCounter.Find("movement").GetComponent<Image>();
            _actionPhaseIndicator = TurnCounter.Find("action").GetComponent<Image>();
            _cleanupPhaseIndicator = TurnCounter.Find("cleanup").GetComponent<Image>();
        }

        private string TurnNumberToString()
        {
            switch (_currentTurn)
            {
                case 1:
                    return "Turn One";
                case 2:
                    return "Turn Two";
                case 3:
                    return "Turn Three";
                case 4:
                    return "Turn Four";
                case 5:
                    return "Turn Five";
                case 6:
                    return "Turn Six";
                case 7:
                    return "Turn Seven";
                case 8:
                    return "Turn Eight";
                case 9:
                    return "Turn Nine";
                default:
                    return "Turn " + _currentTurn;
            }
        }

        private void SetTurnDisplay()
        {
            _turnCounter.text = TurnNumberToString();

            switch (_phase)
            {
                case TurnPhase.Initiative:
                    _initiativePhaseIndicator.color = Color.white;
                    _initiativePhaseIndicator.rectTransform.sizeDelta = new Vector2(90, 90);

                    _commandPhaseIndicator.color = _inactiveTurnColor;
                    _commandPhaseIndicator.rectTransform.sizeDelta = new Vector2(60, 60);
                    _movementPhaseIndicator.color = _inactiveTurnColor;
                    _movementPhaseIndicator.rectTransform.sizeDelta = new Vector2(60, 60);
                    _actionPhaseIndicator.color = _inactiveTurnColor;
                    _actionPhaseIndicator.rectTransform.sizeDelta = new Vector2(60, 60);
                    _cleanupPhaseIndicator.color = _inactiveTurnColor;
                    _cleanupPhaseIndicator.rectTransform.sizeDelta = new Vector2(60, 60);
                    break;
                case TurnPhase.Command:
                    _commandPhaseIndicator.color = Color.white;
                    _commandPhaseIndicator.rectTransform.sizeDelta = new Vector2(90, 90);

                    _initiativePhaseIndicator.color = _inactiveTurnColor;
                    _initiativePhaseIndicator.rectTransform.sizeDelta = new Vector2(60, 60);
                    _movementPhaseIndicator.color = _inactiveTurnColor;
                    _movementPhaseIndicator.rectTransform.sizeDelta = new Vector2(60, 60);
                    _actionPhaseIndicator.color = _inactiveTurnColor;
                    _actionPhaseIndicator.rectTransform.sizeDelta = new Vector2(60, 60);
                    _cleanupPhaseIndicator.color = _inactiveTurnColor;
                    _cleanupPhaseIndicator.rectTransform.sizeDelta = new Vector2(60, 60);
                    break;
                case TurnPhase.Movement:
                    _movementPhaseIndicator.color = Color.white;
                    _movementPhaseIndicator.rectTransform.sizeDelta = new Vector2(90, 90);

                    _initiativePhaseIndicator.color = _inactiveTurnColor;
                    _initiativePhaseIndicator.rectTransform.sizeDelta = new Vector2(60, 60);
                    _commandPhaseIndicator.color = _inactiveTurnColor;
                    _commandPhaseIndicator.rectTransform.sizeDelta = new Vector2(60, 60);
                    _actionPhaseIndicator.color = _inactiveTurnColor;
                    _actionPhaseIndicator.rectTransform.sizeDelta = new Vector2(60, 60);
                    _cleanupPhaseIndicator.color = _inactiveTurnColor;
                    _cleanupPhaseIndicator.rectTransform.sizeDelta = new Vector2(60, 60);
                    break;
                case TurnPhase.Action:
                    _actionPhaseIndicator.color = Color.white;
                    _actionPhaseIndicator.rectTransform.sizeDelta = new Vector2(90, 90);

                    _initiativePhaseIndicator.color = _inactiveTurnColor;
                    _initiativePhaseIndicator.rectTransform.sizeDelta = new Vector2(60, 60);
                    _commandPhaseIndicator.color = _inactiveTurnColor;
                    _commandPhaseIndicator.rectTransform.sizeDelta = new Vector2(60, 60);
                    _movementPhaseIndicator.color = _inactiveTurnColor;
                    _movementPhaseIndicator.rectTransform.sizeDelta = new Vector2(60, 60);
                    _cleanupPhaseIndicator.color = _inactiveTurnColor;
                    _cleanupPhaseIndicator.rectTransform.sizeDelta = new Vector2(60, 60);
                    break;
                case TurnPhase.Cleanup:
                    _cleanupPhaseIndicator.color = Color.white;
                    _cleanupPhaseIndicator.rectTransform.sizeDelta = new Vector2(90, 90);

                    _initiativePhaseIndicator.color = _inactiveTurnColor;
                    _initiativePhaseIndicator.rectTransform.sizeDelta = new Vector2(60, 60);
                    _commandPhaseIndicator.color = _inactiveTurnColor;
                    _commandPhaseIndicator.rectTransform.sizeDelta = new Vector2(60, 60);
                    _movementPhaseIndicator.color = _inactiveTurnColor;
                    _movementPhaseIndicator.rectTransform.sizeDelta = new Vector2(60, 60);
                    _actionPhaseIndicator.color = _inactiveTurnColor;
                    _actionPhaseIndicator.rectTransform.sizeDelta = new Vector2(60, 60);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool UpdateStateAndUpdateInitiative(out GameResponse state)
        {
            state = Referee.CurrentGameState;

            while (ShipsInInitiativeStep == null || ShipsInInitiativeStep.Count == 0)
            {
                _currentInitiativeStep++;
                if (_currentInitiativeStep == 13)
                {
                    IncrementPhase();
                    return true;
                }

                SetupInitiativeStep();
                CurrentPlayer = _initativePhaseStartingPlayer;
            }

            return false;
        }

        private readonly List<Popup> _initiativePopups = new List<Popup>();

        private bool InitiativePhase()
        {
            if (_initiativePopups.Count == 0)
            {
                foreach (var player in Referee.Players)
                {
                    foreach (var ship in player.Fleet.Where(ship => ship.Alive))
                    {
                        ship.SetInitiative(Referee.Rng);
                        _initiativePopups.Add(Referee.Popup.Clone(string.Format("Initiative {0}", ship.Initiative),
                            ship.transform.position + Vector3.up, 0.5f, 5));
                    }
                }

                return false;
            }

            if (_initiativePopups.Any(popup => popup != null)) return false;
            _initiativePopups.Clear();
            return true;

        }

        public Player CurrentPlayer;
        private readonly Color _inactiveTurnColor = new Color(1, 1, 1, 0.5f);

        public void DrawSystemsDisplay(Ship ship, Func<int, ShipSystem, int, bool> isInteractable,
            Action<int, ShipSystem, int, Image> onClick, Action<Ship> onComplete)
        {
            ClearSystemsDisplay();

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
                icon.SetAsChild(LowerBar);

                icon.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

                rectTransform = icon.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(-55 / 2f * systemsLength + 55 * i, 0);

                var image = icon.GetComponent<Image>();
                if (system.System.Type == SystemType.Composite && subsystem > -1)
                {
                    var texture2D = system.System.SubSystems[subsystem].Icon;
                    image.sprite = system.System.SubSystems[subsystem].Icon;
                }
                else
                {
                    image.sprite = system.System.Icon;
                }

                button = icon.GetComponent<Button>();

                button.targetGraphic = image;
                var interactable = !damaged && !used && isInteractable(i, system.System, subsystem);

                button.interactable = interactable;

                var unmodifiedImage = image;
                var unmodifiedIndex = i;
                var unmodifiedSubsystem = subsystem;
                if (interactable)
                    button.onClick.AddListener(() =>
                        onClick(unmodifiedIndex, system.System, unmodifiedSubsystem, unmodifiedImage));

                image.color = interactable
                    ? Color.white
                    : (damaged ? new Color(0.5f, 0, 0, 0.5f) : new Color(0.5f, 0.5f, 0.5f, 0.5f));
            }

            var done = new GameObject("done button", typeof(Image), typeof(Button), typeof(RectTransform));
            done.SetAsChild(LowerBar);

            rectTransform = done.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(0, -55);
            rectTransform.sizeDelta = new Vector2(180, 35);

            var iconImage = done.GetComponent<Image>();
            iconImage.sprite = Referee.ButtonSprite;

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
            text.font = Referee.StandardFont;
            text.fontSize = 14;
        }

        public GameObject CreateButton(Vector2 position, Vector2 size, string buttonText, Action onClick)
        {
            var done = new GameObject("done button", typeof(Image), typeof(Button));
            done.SetAsChild(LowerBar);

            var rectTransform = done.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;

            var iconImage = done.GetComponent<Image>();
            iconImage.sprite = Referee.ButtonSprite;

            var button = done.GetComponent<Button>();
            button.image = iconImage;
            button.onClick.AddListener(() => onClick());

            CreateText(done.transform, size, buttonText);

            return done;
        }

        public GameObject CreateText(Transform parent, Vector2 size, string buttonText)
        {
            var textObj = new GameObject("Text", typeof(Text));
            textObj.SetAsChild(parent);

            var component = textObj.GetComponent<RectTransform>();
            component.sizeDelta = size;
            component.anchoredPosition = Vector2.zero;

            var text = textObj.GetComponent<Text>();
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = buttonText;
            text.font = Referee.StandardFont;
            text.fontSize = 14;

            return textObj;
        }

        public bool IsShipSelectable(Ship ship)
        {
            return ShipsInInitiativeStep[CurrentPlayer].Contains(ship);
        }

        public void ClearSystemsDisplay()
        {
            LowerBar.transform.DestroyAllChildren(obj => obj.GetComponent<ShipCard>() == null);
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