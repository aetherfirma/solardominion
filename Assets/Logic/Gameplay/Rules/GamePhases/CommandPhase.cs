using System.Collections.Generic;
using System.Linq;
using Logic.Display;
using Logic.Gameplay.Ships;
using Logic.Network;
using Logic.Utilities;
using UnityEngine;

namespace Logic.Gameplay.Rules.GamePhases
{
    public class CommandPhase
    {
        private Ship _selection;
        private GameplayHandler _gameplayHandler;
        private List<Popup> _popups = new List<Popup>();

        public CommandPhase(GameplayHandler gameplayHandler)
        {
            _gameplayHandler = gameplayHandler;
        }

        public void Update()
        {
            GameResponse state;
            if (_gameplayHandler.UpdateStateAndUpdateInitiative(out state)) return;

            if (_gameplayHandler.CurrentPlayer.Number != _gameplayHandler.Referee.LocalPlayer)
            {
                _gameplayHandler.Referee.DisplayUpperText(string.Format("Waiting for {0:} to play", _gameplayHandler.CurrentPlayer.Faction));

                if (_gameplayHandler.Referee.LastObservedInstruction < state.turns.Count)
                {
                    for (var i = _gameplayHandler.Referee.LastObservedInstruction; i < state.turns.Count; i++)
                    {
                        var turn = state.turns[i];
                        if (turn.player != _gameplayHandler.Referee.PlayerUuid && turn.action == TurnType.CommandPhase)
                        {
                            var ship = _gameplayHandler.CurrentPlayer.Fleet.Single(s => s.ShipUuid == turn.ship);
                            _gameplayHandler.Referee.Popup.Clone(string.Format("Orders received for {0}", ship.Name()), ship.transform.position, 0.5f, 5);

                            if (turn.order != null)
                            {
                                ship.Order = (Order) turn.order;
                            }

                            foreach (var system in turn.system_status.Keys)
                            {
                                ship.Subsystem[system] = turn.system_status[system];
                            }

                            ship.CalculateThrust();
                            _gameplayHandler.RemoveShipFromCurrentStep(ship);
                        }

                        _gameplayHandler.Referee.LastObservedInstruction = i + 1;
                        _gameplayHandler.NextPlayer();
                    }
                }
            }
            else
            {
                _gameplayHandler.Referee.DisplayUpperText("");

                if (_selection == null)
                {
                    MarkSelectableShips();

                    if (Input.GetMouseButtonUp(0) && _gameplayHandler.Referee.MouseSelection)
                    {
                        var ship = _gameplayHandler.Referee.MouseSelection.GetComponent<Ship>();
                        if (ship != null && _gameplayHandler.IsShipSelectable(ship))
                        {
                            ClearPopups();
                            _selection = ship;
                            var cameraOperator = _gameplayHandler.Referee.CameraOperator;
                            cameraOperator.SetCameraPosition(ship.transform.position, cameraOperator.Direction, cameraOperator.Zoom);
                            
                            _gameplayHandler.DrawSystemsDisplay(
                                ship,
                                (index, system, subsystem) =>
                                    system.Type == SystemType.Command || system.Type == SystemType.Composite,
                                (index, system, subsystem, image) =>
                                {
                                    switch (system.Type)
                                    {
                                        case SystemType.Composite:
                                            ship.Subsystem[index] =
                                                (ship.Subsystem[index] + 1) % system.SubSystems.Length;
                                            _gameplayHandler.Referee.FlashMessage(string.Format("Set {0:} to {1:}", system.name,
                                                system.SubSystems[ship.Subsystem[index]].name));
                                            var texture2D = system.SubSystems[ship.Subsystem[index]].Icon;
                                            image.sprite = Sprite.Create(texture2D,
                                                new Rect(0, 0, texture2D.width, texture2D.height),
                                                new Vector2(texture2D.width / 2f, texture2D.height / 2f));
                                            break;
                                        case SystemType.Command:
                                            _gameplayHandler.Referee.FlashMessage("Would add an order");
                                            break;
                                    }
                                },
                                selectedShip =>
                                {
                                    if (selectedShip.Systems.Where((t, i) =>
                                        t.System.Type == SystemType.Composite && !selectedShip.Damage[i] && selectedShip.Subsystem[i] == -1).Any())
                                    {
                                        _gameplayHandler.Referee.FlashMessage(
                                            "All composite systems must be set to one of their options");
                                        return;
                                    }

                                    _gameplayHandler.ClearSystemsDisplay();
                                    _selection = null;
                                    selectedShip.CalculateThrust();
                                    _gameplayHandler.RemoveShipFromCurrentStep(selectedShip);
                                    BroadcastShipCommandState(selectedShip);
                                    ClearPopups();

                                    _gameplayHandler.NextPlayer();
                                }
                            );
                        }
                    }
                }
            }
        }

        private void MarkSelectableShips()
        {
            if (_popups.Count == 0)
            {
                foreach (var ship in _gameplayHandler.ShipsInInitiativeStep[_gameplayHandler.CurrentPlayer])
                {
                    _popups.Add(_gameplayHandler.Referee.Popup.Clone("Needs commands", ship.transform.position));
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
                if (ship.Systems[i].System.Type == SystemType.Composite)
                {
                    turn.system_status[i] = ship.Subsystem[i];
                }
            }

            var wwwForm = new WWWForm();
            wwwForm.AddField("player", _gameplayHandler.Referee.PlayerUuid);
            wwwForm.AddField("turn", StringSerializationAPI.Serialize<Turn>(turn));

            SimpleRequest.Post(_gameplayHandler.Referee.ServerUrl + "/game/" + _gameplayHandler.Referee.GameUuid + "/turn", wwwForm,
                www =>
                {
                    var response = GameResponse.FromJson(www.downloadHandler.text);
                    _gameplayHandler.Referee.LastObservedInstruction = response.turns.Count;
                    _gameplayHandler.Referee.SetGameState(response);
                },
                www => _gameplayHandler.Referee.FlashMessage("There was a server error (" + www.responseCode + ") creating a game\n" +
                                                            www.error),
                www => _gameplayHandler.Referee.FlashMessage("There was a network error creating a game\n" + www.error)
            );
        }
    }
}