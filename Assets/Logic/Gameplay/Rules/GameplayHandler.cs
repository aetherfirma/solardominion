using System;
using System.Collections.Generic;
using System.Linq;
using Logic.Gameplay.Players;
using Logic.Gameplay.Ships;
using Logic.Utilities;

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

        private void CommandPhase()
        {
            while (_shipsInInitiativeStep == null || _shipsInInitiativeStep.Count == 0)
            {
                _currentInitiativeStep++;
                SetupInitiativeStep();
                _currentPlayer = _initativePhaseStartingPlayer;
            }

            if (_currentPlayer.Number != _referee.LocalPlayer)
            {
                _referee.DisplayUpperText(String.Format("Waiting for {0:} to play", _currentPlayer.Faction));
            }
            else
            {
                _referee.DisplayUpperText("It's your turn");
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