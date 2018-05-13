using System;
using System.Collections.Generic;
using System.Linq;
using Logic.Display;
using Logic.Gameplay.Ships;
using Logic.Network;
using Logic.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Logic.Gameplay.Rules.GamePhases
{
    public class ActionPhase
    {
        private GameplayHandler _gameplayHandler;

        private Ship _selection;
        private Ship _target;
        private ShipSystem _selectedWeapon;
        private List<int> _selectedSystems = new List<int>();

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

            if (_selection == null) SelectActiveShip();
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

        private void SelectActiveShip()
        {
            _gameplayHandler.CircleSelectableShips();

            if (Input.GetMouseButtonUp(0) && _gameplayHandler.Referee.MouseSelection)
            {
                var ship = _gameplayHandler.Referee.MouseSelection.GetComponent<Ship>();
                if (ship != null && _gameplayHandler.IsShipSelectable(ship))
                {
                    _gameplayHandler.ClearArcs();
                    _selection = ship;
                    _gameplayHandler.Arcs.Add(ArcRenderer.NewArc(_selection.transform, 7, 0.5f, 0, Mathf.PI * 2, 64,
                        Color.red));
                    DrawActionSystemsDisplay(ship);
                }
            }
        }

        private void SelectAttackTarget()
        {
            if (Input.GetMouseButtonUp(0) && _gameplayHandler.Referee.MouseSelection && _selectedWeapon != null)
            {
                var ship = _gameplayHandler.Referee.MouseSelection.GetComponent<Ship>();
                if (ship != null && ship.Player != _gameplayHandler.CurrentPlayer)
                {
                    if (IsInRange(_selection, _selectedWeapon, ship))
                    {
                        _target = ship;
                        _gameplayHandler.ClearSystemsDisplay();
                        _gameplayHandler.ClearArcs();
                        _gameplayHandler.Arcs.Add(ArcRenderer.NewArc(_selection.transform, 7, 0.5f, 0, Mathf.PI * 2, 64,
                            Color.red));
                        _gameplayHandler.Arcs.Add(ArcRenderer.NewArc(_target.transform, 7, 0.5f, 0, Mathf.PI * 2, 64,
                            Color.red));
                        BroadcastWeaponFiring(_selection, _selectedWeapon, _selectedSystems, _target);
                        _gameplayHandler.Referee.DisplayUpperText("Waiting on response");
                    }
                    else
                    {
                        _gameplayHandler.Referee.FlashMessage(string.Format("Cannot fire {0:} at {1:}, out of range.",
                            _selectedWeapon.name, ship.Name()));
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
                        var ship = _gameplayHandler.CurrentPlayer.Fleet.Single(s => s.ShipUuid == turn.ship);
                        _gameplayHandler.Referee.FlashMessage(
                            String.Format("Just recieved reply for {0:}", ship.Name()));
                        _gameplayHandler.Referee.DisplayUpperText("");

                        // Obey Action

                        _gameplayHandler.RemoveShipFromStep(ship);
                    }

                    _gameplayHandler.Referee.LastObservedInstruction = i + 1;
                    _gameplayHandler.NextPlayer();
                }
            }
        }

        private void DrawActionSystemsDisplay(Ship ship)
        {
            _gameplayHandler.DrawSystemsDisplay(
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
                                _gameplayHandler.Arcs.Add(ArcRenderer.NewArc(_selection.transform,
                                    _selectedWeapon.ShortRange, 0.1f, 0, Mathf.PI * 2, 64, Color.white));
                                _gameplayHandler.Arcs.Add(ArcRenderer.NewArc(_selection.transform,
                                    _selectedWeapon.MediumRange, 0.1f, 0, Mathf.PI * 2, 64, Color.white));
                                _gameplayHandler.Arcs.Add(ArcRenderer.NewArc(_selection.transform,
                                    _selectedWeapon.LongRange, 0.1f, 0, Mathf.PI * 2, 64, Color.white));
                            }
                            else
                            {
                                if (_selectedSystems.Contains(index))
                                {
                                    _selectedSystems.Remove(index);
                                    image.color = Color.white;
                                    if (_selectedSystems.Count == 0)
                                    {
                                        _selectedWeapon = null;
                                        _gameplayHandler.ClearArcs();
                                        _gameplayHandler.Arcs.Add(ArcRenderer.NewArc(_selection.transform, 7, 0.5f, 0,
                                            Mathf.PI * 2, 64, Color.red));
                                    }
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
                                        _gameplayHandler.Referee.FlashMessage(
                                            "Cannot combine different weapon types", 10);
                                    }
                                }
                            }

                            break;
                        case SystemType.Hangar:
                            _gameplayHandler.Referee.FlashMessage("Would deploy a ship");
                            break;
                    }
                },
                selectedShip =>
                {
                    _gameplayHandler.ClearSystemsDisplay();
                    _selection = null;
                    _gameplayHandler.RemoveShipFromStep(selectedShip);
                    BroadcastEndOfAction(selectedShip);
                    _gameplayHandler.ClearArcs();
                    _gameplayHandler.LowerBar.transform.Find("Text").GetComponent<Text>().text = "";

                    _gameplayHandler.NextPlayer();
                }
            );
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

                        // Obey Action

                        _gameplayHandler.RemoveShipFromStep(ship);
                        _gameplayHandler.NextPlayer();
                    }
                    else if (turn.player != _gameplayHandler.Referee.PlayerUuid &&
                             turn.action == TurnType.ActionChallenge)
                    {
                        _gameplayHandler.Referee.FlashMessage("Need to respond");
                        var firingShip = _gameplayHandler.CurrentPlayer.Fleet.Single(s => s.ShipUuid == turn.ship);
                        var targetShip = _gameplayHandler.CurrentPlayer.Fleet.Single(s => s.ShipUuid == turn.ship);
                    }

                    _gameplayHandler.Referee.LastObservedInstruction = i + 1;
                }
            }
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
                damage = selectedWeapon.Damage,
                shots = selectedSystems.Count * selectedWeapon.Shots,
                defence_modifier = modifier
            };

            var wwwForm = new WWWForm();
            wwwForm.AddField("player", _gameplayHandler.Referee.PlayerUuid);
            wwwForm.AddField("turn", StringSerializationAPI.Serialize<Turn>(turn));

            SimpleRequest.Post(
                _gameplayHandler.Referee.ServerUrl + "/game/" + _gameplayHandler.Referee.GameUuid + "/turn", wwwForm,
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
                _gameplayHandler.Referee.ServerUrl + "/game/" + _gameplayHandler.Referee.GameUuid + "/turn", wwwForm,
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