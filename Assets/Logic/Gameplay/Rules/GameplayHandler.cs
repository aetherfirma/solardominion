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
        private OrderedSet<int> _playOrder;
        public Dictionary<Player, List<Ship>> ShipsInInitiativeStep;
        private Player _initativePhaseStartingPlayer;
        public List<GameObject> Arcs = new List<GameObject>();
        public RectTransform LowerBar;
        
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

        public void RemoveShipFromStep(Ship ship)
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

            switch (_phase)
            {
                case TurnPhase.Initiative:
                    InitiativePhase();
                    IncrementPhase();
                    goto case TurnPhase.Command;
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
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        public void CircleSelectableShips()
        {
            if (Arcs.Count != 0) return;
            foreach (var ship in ShipsInInitiativeStep[CurrentPlayer])
            {
                Arcs.Add(ArcRenderer.NewArc(ship.transform, 7, 0.5f, 0, Mathf.PI * 2, 64, Color.white));
            }
        }

        public bool UpdateStateAndUpdateInitiative(out GameResponse state)
        {
            state = Referee.UpdateGameState();

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

        private void InitiativePhase()
        {
            foreach (var player in Referee.Players)
            {
                foreach (var ship in player.Fleet.Where(ship => ship.Alive))
                {
                    ship.SetInitiative(Referee.Rng);
                }
            }
        }

        public Player CurrentPlayer;

        public void ClearArcs()
        {
            foreach (var arc in Arcs)
            {
                Object.Destroy(arc);
            }

            Arcs.Clear();
        }

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
                card.ParentTransform = Referee.UiCanvas;

                var unmodifiedImage = image;
                var unmodifiedIndex = i;
                var unmodifiedSubsystem = subsystem;
                if (interactable) button.onClick.AddListener(() => onClick(unmodifiedIndex, system, unmodifiedSubsystem, unmodifiedImage));

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

        public bool IsShipSelectable(Ship ship)
        {
            return ShipsInInitiativeStep[CurrentPlayer].Contains(ship);
        }

        public void ClearSystemsDisplay()
        {
            LowerBar.transform.DestroyAllChildren(obj => obj.GetComponent<Image>() != null);
            LowerBar.transform.Find("Text").GetComponent<Text>().text = "";
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