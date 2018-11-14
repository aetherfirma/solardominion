using System;
using System.Collections.Generic;
using System.Linq;
using Logic.Display;
using Logic.Gameplay.Ships;
using Logic.Maths;
using Logic.Network;
using Logic.Utilities;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Logic.Gameplay.Rules.GamePhases
{
    public class ActionPhase
    {
        private GameplayHandler _gameplayHandler;

        private Ship _target;
        private readonly List<Popup> _popups = new List<Popup>();
        private GameObject _rangeMarker;
        private ShipSystem _selectedWeapon;
        private int _selectedShots;

        public ActionPhase(GameplayHandler gameplayHandler)
        {
            _gameplayHandler = gameplayHandler;
        }

        public void Update()
        {
            GameResponse state;
            if (_gameplayHandler.UpdateStateAndUpdateInitiative(out state)) return;

            if (_gameplayHandler.CurrentPlayer.Number != _gameplayHandler.Referee.LocalPlayer) NetworkPlayer(state);
            else LocalPlayer(state);
        }

        private void LocalPlayer(GameResponse state)
        {
            _gameplayHandler.Referee.DisplayUpperText("");

            if (_gameplayHandler.SelectedShip == null) SelectActiveShip();
            else
            {
                if (_target)
                {
                    _gameplayHandler.Referee.DisplayUpperText(String.Format("Waiting for {0:} to respond",
                        _target.Player.Faction));

                    WaitForAttackResponse(state);
                }
                else SelectAttackTarget();
            }
        }
        
        private void MarkSelectableShips()
        {
            if (_popups.Count == 0)
            {
                foreach (var ship in _gameplayHandler.ShipsInInitiativeStep[_gameplayHandler.CurrentPlayer])
                {
                    _popups.Add(_gameplayHandler.Referee.Popup.Clone("Ready to act", ship.transform.position));
                }
            }
        }

        private void ClearPopups()
        {
            foreach (var popup in _popups)
            {
                popup.Destroy();
            }
            _popups.Clear();
        }

        private void SelectActiveShip()
        {
            MarkSelectableShips();

            if (Input.GetMouseButtonUp(0) && _gameplayHandler.Referee.MouseSelection)
            {
                var ship = _gameplayHandler.Referee.MouseSelection.GetComponent<Ship>();
                if (ship != null && _gameplayHandler.IsShipSelectable(ship))
                {
                    ClearPopups();
                    _gameplayHandler.SelectedShip = ship;
                    var cameraOperator = _gameplayHandler.Referee.CameraOperator;
                    cameraOperator.SetCameraPosition(ship.transform.position, cameraOperator.Direction, cameraOperator.Zoom);
                    var shipCard = ship.Card;
                    shipCard.SelectionCallback = ActionPhaseSelectionCallback;
                    shipCard.DeselectionCallback = ActionPhaseDeselectionCallback;
                    FindSelectableSystems(shipCard);
                    _gameplayHandler.Referee.CreateButton(new Vector2(300, 50), new Vector2(80, 25), "End Phase", OnComplete, _gameplayHandler.LowerBar);
                }
            }
        }

        private ShipSystemSelectionResult ActionPhaseSelectionCallback(ShipCard card, int index, ShipSystem system)
        {
            var realSystem = system.ResolveSystem(card.Ship.Subsystem[index]);
            
            if (realSystem.Type == SystemType.Hangar)
            {
                // TODO: Implement strike craft
                _gameplayHandler.Referee.FlashMessage("Hangars aren't supported yet");
                card.Ship.Used[index] = true;
                return ShipSystemSelectionResult.DoNothing;
            }
            
            if (realSystem.Type == SystemType.Weapon)
            {
                FindLikeSystems(card, realSystem);
                if (_rangeMarker == null) SetupRangeMarker(realSystem);

                return ShipSystemSelectionResult.SetSelection;
            }

            return ShipSystemSelectionResult.DoNothing;
        }

        private void SetupRangeMarker(ShipSystem system)
        {
            _rangeMarker = Object.Instantiate(_gameplayHandler.Referee.RangeRings,
                new Vector3(0, -3, 0), Quaternion.identity);
            _rangeMarker.GetComponent<RangeMarkers>().Setup(
                system.ShortRange,
                system.MediumRange,
                system.LongRange,
                _gameplayHandler.SelectedShip.transform.position - new Vector3(0, -3, 0),
                _gameplayHandler.Referee
            );
        }

        private static void FindLikeSystems(ShipCard card, ShipSystem realSystem)
        {
            for (var i = 0; i < card.Selectable.Length; i++)
            {
                var shipSystem = card.Ship.Systems[i].System.ResolveSystem(card.Ship.Subsystem[i]);
                card.Selectable[i] = !card.Ship.Used[i] && !card.Ship.Damage[i] && shipSystem == realSystem;
            }
        }

        private void ActionPhaseDeselectionCallback(ShipCard card, int index, ShipSystem system)
        {
            if (!card.Selected.Any(s => s))
            {
                FindSelectableSystems(card);
                if (_rangeMarker != null) DestroyRangeMarker();
            }
        }

        private void DestroyRangeMarker()
        {
            Object.Destroy(_rangeMarker);
            _rangeMarker = null;
        }

        private static void FindSelectableSystems(ShipCard card)
        {
            for (var i = 0; i < card.Selectable.Length; i++)
            {
                var shipSystem = card.Ship.Systems[i].System.ResolveSystem(card.Ship.Subsystem[i]);
                card.Selectable[i] = !card.Ship.Used[i] && !card.Ship.Damage[i] &&
                                     (shipSystem.Type == SystemType.Weapon || shipSystem.Type == SystemType.Hangar);
            }
        }

        private void SelectAttackTarget()
        {
            var selections = _gameplayHandler.SelectedShip.Card.Selected.Select((selected, index) =>
            {
                if (!selected) return null;
                return _gameplayHandler.SelectedShip.Systems[index].System
                    .ResolveSystem(_gameplayHandler.SelectedShip.Subsystem[index]);
            }).Where(system => system != null).ToArray();
            
            if (Input.GetMouseButtonUp(0) && _gameplayHandler.Referee.MouseSelection && selections.Any())
            {                
                var ship = _gameplayHandler.Referee.MouseSelection.GetComponent<Ship>();
                if (ship != null && ship.Player != _gameplayHandler.CurrentPlayer)
                {
                    var weapon = selections.First();
                    
                    if (IsInRange(_gameplayHandler.SelectedShip, weapon, ship))
                    {
                        _target = ship;
                        _gameplayHandler.ClearSystemsDisplay();
                        // TODO: Show target better
                        BroadcastWeaponFiring(_gameplayHandler.SelectedShip, weapon, selections.Length, _target);
                        _selectedWeapon = weapon;
                        _selectedShots = selections.Length;
                        _gameplayHandler.SelectedShip.MarkAsUsed(_gameplayHandler.SelectedShip.Card.Selected);
                        _gameplayHandler.SelectedShip.Card.ClearSelected();
                        _gameplayHandler.Referee.DisplayUpperText("Waiting on response");
                        _gameplayHandler.ShowShipCards = false;
                        DestroyRangeMarker();
                    }
                    else
                    {
                        _gameplayHandler.Referee.FlashMessage(string.Format("Cannot fire {0:} at {1:}, out of range.",
                            weapon.name, ship.Name()));
                    }
                }
            }
        }

        private void WaitForAttackResponse(GameResponse state)
        {
            if (_gameplayHandler.Referee.LastObservedInstruction < state.turns.Count)
            {
                for (var i = _gameplayHandler.Referee.LastObservedInstruction; i < state.turns.Count; i++)
                {
                    var turn = state.turns[i];
                    if (turn.action == TurnType.ActionResponse)
                    {
                        var firingShip = _gameplayHandler.CurrentPlayer.Fleet.Single(s => s.ShipUuid == turn.ship);
                        var targetShip = _gameplayHandler.Referee.Players.Single(p => p.Uuid == turn.target_player).Fleet.Single(s => s.ShipUuid == turn.target);
                        
                        _gameplayHandler.Referee.FlashMessage(
                            string.Format("Just recieved reply for {0:}", firingShip.Name()));
                        _gameplayHandler.Referee.DisplayUpperText("");

                        targetShip.ThrustRemaining -= turn.thrust;

                        var shots = _selectedWeapon.Shots * _selectedShots;
                        var damage = _selectedWeapon.Damage;
                        var defenderPool = turn.thrust + GetRangeModifier(firingShip, _selectedWeapon, targetShip) + targetShip.CalculateDefensiveModifier();

                        _selectedWeapon = null;

                        ResolveAttack(firingShip, shots, damage, targetShip, defenderPool);

                        _target = null;
                        _gameplayHandler.ShowShipCards = true;
                        FindSelectableSystems(_gameplayHandler.SelectedShip.Card);
                        _gameplayHandler.Referee.CreateButton(new Vector2(300, 50), new Vector2(80, 25), "End Phase", OnComplete, _gameplayHandler.LowerBar);
                    }

                    _gameplayHandler.Referee.LastObservedInstruction = i + 1;
                    _gameplayHandler.NextPlayer();
                }
            }
        }


        private void OnComplete()
        {
            _gameplayHandler.SelectedShip.Card.Selectable = new bool[_gameplayHandler.SelectedShip.Card.Selectable.Length];
            _gameplayHandler.ClearSystemsDisplay();
            _gameplayHandler.RemoveShipFromCurrentStep(_gameplayHandler.SelectedShip);
            BroadcastEndOfAction(_gameplayHandler.SelectedShip);
            DestroyRangeMarker();
            _gameplayHandler.SelectedShip.Card.ClearSelected();
            _gameplayHandler.SelectedShip = null;
            _gameplayHandler.NextPlayer();
        }

        private void NetworkPlayer(GameResponse state)
        {
            _gameplayHandler.Referee.DisplayUpperText(string.Format("Waiting for {0:} to play",
                _gameplayHandler.CurrentPlayer.Faction));

            if (_gameplayHandler.Referee.LastObservedInstruction < state.turns.Count)
            {
                for (var i = _gameplayHandler.Referee.LastObservedInstruction; i < state.turns.Count; i++)
                {
                    var turn = state.turns[i];
                    if (turn.player != _gameplayHandler.Referee.PlayerUuid && turn.action == TurnType.ActionEnd)
                    {
                        var ship = _gameplayHandler.CurrentPlayer.Fleet.Single(s => s.ShipUuid == turn.ship);
                        _gameplayHandler.Referee.FlashMessage(string.Format("Turn over for {0:}", ship.Name()));

                        _gameplayHandler.RemoveShipFromCurrentStep(ship);
                        _gameplayHandler.NextPlayer();
                    }
                    else if (turn.player != _gameplayHandler.Referee.PlayerUuid && turn.target_player == _gameplayHandler.Referee.PlayerUuid &&
                             turn.action == TurnType.ActionChallenge)
                    {
                        _gameplayHandler.ShowShipCards = false;
                        _gameplayHandler.Referee.DisplayUpperText(string.Format("Being fired at by {0:} {1:} shot{2:}", turn.weapon, turn.shots, turn.shots == 1 ? "" : "s"));
                        var firingShip = _gameplayHandler.CurrentPlayer.Fleet.Single(s => s.ShipUuid == turn.ship);
                        var targetShip = _gameplayHandler.Referee.Players.Single(p => p.Uuid == turn.target_player).Fleet.Single(s => s.ShipUuid == turn.target);

                        var thrustToSpend = 0;

                        var shipDefenceModifier = targetShip.CalculateDefensiveModifier();
                        var turnDefenceModifier = turn.defence_modifier + shipDefenceModifier;
                        var defenceIndicator = _gameplayHandler.Referee.CreateText(_gameplayHandler.LowerBar, new Vector2(90, 45),
                            string.Format("{0:} + {1:} + 0/{2:} = {3:}", turn.defence_modifier, shipDefenceModifier, targetShip.ThrustRemaining, turnDefenceModifier));

                        _gameplayHandler.Referee.CreateButton(new Vector2(95, 0), new Vector2(45, 45), "+",
                            () =>
                            {
                                if (thrustToSpend >= targetShip.ThrustRemaining) return;
                                thrustToSpend++;
                                defenceIndicator.GetComponent<Text>().text =
                                    string.Format("{0:} + {1:} + {2:}/{3:} = {4:}",
                                        turn.defence_modifier, shipDefenceModifier, thrustToSpend,
                                        targetShip.ThrustRemaining, turnDefenceModifier + thrustToSpend);
                            }, _gameplayHandler.LowerBar);
                        _gameplayHandler.Referee.CreateButton(new Vector2(-95, 0), new Vector2(45, 45), "-",
                            () => {
                                if (thrustToSpend <= 0) return;
                                thrustToSpend--;
                                defenceIndicator.GetComponent<Text>().text =
                                    string.Format("{0:} + {1:} + {2:}/{3:} = {4:}",
                                        turn.defence_modifier, shipDefenceModifier, thrustToSpend,
                                        targetShip.ThrustRemaining, turnDefenceModifier + thrustToSpend);
                            }, _gameplayHandler.LowerBar);

                        _gameplayHandler.Referee.CreateButton(new Vector2(0, -55), new Vector2(135, 35),
                            "Commit Defence",
                            () =>
                            {
                                Object.Destroy(defenceIndicator);
                                _gameplayHandler.ClearSystemsDisplay();
                                BroadcastWeaponDefence(firingShip, thrustToSpend, targetShip);
                                targetShip.ThrustRemaining -= thrustToSpend;

                                var shots = turn.shots;
                                var damage = turn.damage;
                                var defenderPool = turnDefenceModifier + thrustToSpend;

                                _gameplayHandler.ShowShipCards = true;
                                ResolveAttack(firingShip, shots, damage, targetShip, defenderPool);
                            }, _gameplayHandler.LowerBar);

                    }

                    _gameplayHandler.Referee.LastObservedInstruction = i + 1;
                }
            }
        }

        private void ResolveAttack(Ship firingShip, int shots, int damage, Ship targetShip, int defenderPool)
        {
            var refereeRng = _gameplayHandler.Referee.Rng;

            if (firingShip.FirstShipFiredAt == null) firingShip.FirstShipFiredAt = targetShip;

            var bracing = targetShip.UnderOrders && targetShip.Order == Order.BraceForImpact;
            var locked = firingShip.UnderOrders && firingShip.Order == Order.LockOnTarget && targetShip == firingShip.FirstShipFiredAt;
            var unprepared = firingShip.UnderOrders && (firingShip.Order == Order.LockOnTarget && targetShip != firingShip.FirstShipFiredAt || firingShip.Order == Order.BraceForImpact);

            var attackerResults = refereeRng.D6(shots);
            if (locked) attackerResults = attackerResults.RerollFailures(firingShip.Training, refereeRng);
            else if (unprepared) attackerResults = attackerResults.RerollSuccesses(firingShip.Training, refereeRng);
            var attackerSuccesses = attackerResults.Successes(firingShip.Training);
            
            _gameplayHandler.Referee.Popup.Clone(
                string.Format(
                    "{0} has rolled {1}{4}\nfor {2} success{3}", 
                    firingShip.Name(), 
                    attackerResults.DescribeDiceRolls(),
                    attackerSuccesses, 
                    attackerSuccesses == 1 ? "" : "es",
                    locked ? ", rerolling failures," : (unprepared ? ", rerolling successes,": "")
                ),
                firingShip.Position, 0.5f, 5
            );
            
            var defenderResults = refereeRng.D6(defenderPool);
            if (bracing) defenderResults = defenderResults.RerollFailures(targetShip.Training, refereeRng);
            var defenderSuccesses = defenderResults.Successes(targetShip.Training);
            var result = attackerSuccesses - defenderSuccesses;
            if (result > 0)
            {
                var totalDamage = result * damage;
                targetShip.TakeDamage(refereeRng, totalDamage);
                if (targetShip.Alive)
                {
                    _gameplayHandler.Referee.Popup.Clone(
                        string.Format(
                            "{0} has rolled {1}{5}\nfor {2} success{3} and took {4} damage", 
                            targetShip.Name(), 
                            defenderResults.DescribeDiceRolls(),
                            defenderSuccesses, 
                            defenderSuccesses == 1 ? "" : "es",
                            totalDamage,
                            bracing ? ", rerolling failures," : ""
                        ),
                        targetShip.Position, 0.5f, 5
                    );
                    for (var i = 0; i < totalDamage; i++)
                    {
                        Object.Instantiate(_gameplayHandler.Referee.ShipHitExplosion, targetShip.transform.position + Random.onUnitSphere,
                            Quaternion.identity);
                    }
                }
                else
                {
                    _gameplayHandler.Referee.Popup.Clone(
                        string.Format(
                            "{0} has rolled {1}{4}\nfor {2} success{3} and was destroyed", 
                            targetShip.Name(), 
                            defenderResults.DescribeDiceRolls(),
                            defenderSuccesses, 
                            defenderSuccesses == 1 ? "" : "es",
                            bracing ? ", rerolling failures," : ""
                        ),
                        targetShip.Position, 0.5f, 5
                    );
                   _gameplayHandler.DestroyShip(targetShip);
                }
            }
            else
            {
                _gameplayHandler.Referee.Popup.Clone(
                    string.Format(
                        "{0} has rolled {1}{4}\n for {2} success{3} and took no damage", 
                        targetShip.Name(), 
                        defenderResults.DescribeDiceRolls(),
                        defenderSuccesses, 
                        defenderSuccesses == 1 ? "" : "es",
                        bracing ? ", rerolling failures," : ""
                    ),
                    targetShip.Position, 0.5f, 5
                );
            }
        }

        private static int GetRangeModifier(Ship ship, ShipSystem selectedWeapon, Ship target)
        {
//            var range = (ship.transform.position - target.transform.position).magnitude;

            var shipHitPoints = ship.HitPoints();
            var targetHitPoints = target.HitPoints();

            var range = shipHitPoints.Aggregate(
                float.MaxValue, 
                (current1, shipPoint) => targetHitPoints.Aggregate(
                    current1, 
                    (current, targetPoint) => Mathf.Min(current, (shipPoint - targetPoint).magnitude)
                )
            );

            if (range < selectedWeapon.ShortRange) return selectedWeapon.ShortModifier;
            if (range < selectedWeapon.MediumRange) return selectedWeapon.MediumModifier;
            return selectedWeapon.LongModifier;
        }

        private static bool IsInRange(Ship ship, ShipSystem selectedWeapon, Ship target)
        {
            var range = (ship.transform.position - target.transform.position).magnitude;
            return range < selectedWeapon.LongRange;
        }

        private void BroadcastWeaponFiring(Ship ship, ShipSystem selectedWeapon, int numWeapons, Ship target)
        {
            var modifier = GetRangeModifier(ship, selectedWeapon, target);

            var turn = new Turn
            {
                action = TurnType.ActionChallenge,
                player = ship.Player.Uuid,
                ship = ship.ShipUuid,
                target_player = target.Player.Uuid,
                target = target.ShipUuid,
                weapon = selectedWeapon.name,
                damage = selectedWeapon.Damage,
                shots = numWeapons * selectedWeapon.Shots,
                defence_modifier = modifier
            };

            var wwwForm = new WWWForm();
            wwwForm.AddField("player", _gameplayHandler.Referee.PlayerUuid);
            wwwForm.AddField("turn", StringSerializationAPI.Serialize<Turn>(turn));

            SimpleRequest.Post(
                _gameplayHandler.Referee.ServerUrl + "/game/" + _gameplayHandler.Referee.GameUuid + "/turn", 
                _gameplayHandler.Referee.Username, _gameplayHandler.Referee.Password,
                wwwForm,
                www =>
                {
                    var response = GameResponse.FromJson(www.downloadHandler.text);
                    _gameplayHandler.Referee.LastObservedInstruction = response.turns.Count;
                    _gameplayHandler.Referee.SetGameState(response);
                },
                www => _gameplayHandler.Referee.FlashMessage("There was a server error (" + www.responseCode +
                                                             ") creating a game\n" +
                                                             www.error),
                www => _gameplayHandler.Referee.FlashMessage("There was a network error creating a game\n" + www.error)
            );
        }

        private void BroadcastWeaponDefence(Ship ship, int defensiveThrust, Ship target)
        {
            var turn = new Turn
            {
                action = TurnType.ActionResponse,
                player = ship.Player.Uuid,
                ship = ship.ShipUuid,
                target_player = target.Player.Uuid,
                target = target.ShipUuid,
                thrust = defensiveThrust
            };

            var wwwForm = new WWWForm();
            wwwForm.AddField("player", _gameplayHandler.Referee.PlayerUuid);
            wwwForm.AddField("turn", StringSerializationAPI.Serialize<Turn>(turn));

            SimpleRequest.Post(
                _gameplayHandler.Referee.ServerUrl + "/game/" + _gameplayHandler.Referee.GameUuid + "/turn", 
                _gameplayHandler.Referee.Username, _gameplayHandler.Referee.Password,
                wwwForm,
                www =>
                {
                    var response = GameResponse.FromJson(www.downloadHandler.text);
                    _gameplayHandler.Referee.LastObservedInstruction = response.turns.Count;
                    _gameplayHandler.Referee.SetGameState(response);
                },
                www => _gameplayHandler.Referee.FlashMessage("There was a server error (" + www.responseCode +
                                                             ") creating a game\n" +
                                                             www.error),
                www => _gameplayHandler.Referee.FlashMessage("There was a network error creating a game\n" + www.error)
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
            wwwForm.AddField("player", _gameplayHandler.Referee.PlayerUuid);
            wwwForm.AddField("turn", StringSerializationAPI.Serialize<Turn>(turn));

            SimpleRequest.Post(
                _gameplayHandler.Referee.ServerUrl + "/game/" + _gameplayHandler.Referee.GameUuid + "/turn", 
                _gameplayHandler.Referee.Username, _gameplayHandler.Referee.Password,
                wwwForm,
                www =>
                {
                    var response = GameResponse.FromJson(www.downloadHandler.text);
                    _gameplayHandler.Referee.LastObservedInstruction = response.turns.Count;
                    _gameplayHandler.Referee.SetGameState(response);
                },
                www => _gameplayHandler.Referee.FlashMessage("There was a server error (" + www.responseCode +
                                                             ") creating a game\n" +
                                                             www.error),
                www => _gameplayHandler.Referee.FlashMessage("There was a network error creating a game\n" + www.error)
            );
        }
    }
}