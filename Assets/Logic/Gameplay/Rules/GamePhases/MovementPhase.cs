using System.Linq;
using Logic.Display;
using Logic.Gameplay.Ships;
using Logic.Network;
using Logic.Utilities;
using UnityEngine;

namespace Logic.Gameplay.Rules.GamePhases
{
    public class MovementPhase
    {
        private Ship _selection;
        private GameplayHandler _gameplayHandler;

        public MovementPhase(GameplayHandler gameplayHandler)
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
                        if (turn.player != _gameplayHandler.Referee.PlayerUuid && turn.action == TurnType.MovementPhase)
                        {
                            var ship = _gameplayHandler.CurrentPlayer.Fleet.Single(s => s.ShipUuid == turn.ship);
                            _gameplayHandler.Referee.FlashMessage(string.Format("Just recieved movement for {0:}", ship.Name()));

                            var newPosition = new Vector3(turn.location[0], 0, turn.location[1]);

                            var leadingPoint = ship.transform.position +
                                               ship.transform.rotation * (Vector3.forward * ship.Speed * 5);
                            var distance = newPosition - leadingPoint;
                            var thrust = Mathf.CeilToInt(distance.magnitude / ship.DistancePerThrust());
                            if (thrust > ship.ThrustRemaining)
                            {
                                _gameplayHandler.Referee.FlashMessage("Cannot move here, insufficient thrust");
                                return;
                            }

                            ship.ThrustRemaining -= thrust;
                            var movementDelta = newPosition - ship.transform.position;
                            ship.Speed = Mathf.CeilToInt(movementDelta.magnitude / 5f);
                            ship.transform.position = newPosition;
                            ship.transform.rotation = Quaternion.LookRotation(movementDelta);

                            _gameplayHandler.RemoveShipFromStep(ship);
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
                    _gameplayHandler.CircleSelectableShips();

                    if (Input.GetMouseButtonUp(0) && _gameplayHandler.Referee.MouseSelection)
                    {
                        var ship = _gameplayHandler.Referee.MouseSelection.GetComponent<Ship>();
                        if (ship != null && _gameplayHandler.IsShipSelectable(ship))
                        {
                            _gameplayHandler.ClearArcs();
                            _selection = ship;
                            var arc = ArcRenderer.NewArc(ship.transform, 1, 0.5f, 0, Mathf.PI * 2, 64, Color.red);
                            for (var i = 1; i < Mathf.Min(ship.ThrustRemaining, 5); i++)
                            {
                                var c = 1.2f - 0.2f * i;
                                _gameplayHandler.Arcs.Add(ArcRenderer.NewArc(arc.transform, ship.DistancePerThrust() * i, 0.1f, 0,
                                    Mathf.PI * 2, 64, new Color(c, c, c, c)));
                            }

                            arc.transform.localPosition = Vector3.forward * ship.Speed * 5;
                            _gameplayHandler.Arcs.Add(arc);

                            // TODO: Add a buton to skip movement
                        }
                    }
                }
                else
                {
                    if (Input.GetMouseButtonUp(0))
                    {
                        var leadingPoint = _selection.transform.position + _selection.transform.rotation * (Vector3.forward * _selection.Speed * 5);
                        var distance = _gameplayHandler.Referee.MouseLocation - leadingPoint;
                        var thrust = Mathf.CeilToInt(distance.magnitude / _selection.DistancePerThrust());
                        if (thrust > _selection.ThrustRemaining)
                        {
                            _gameplayHandler.Referee.FlashMessage("Cannot move here, insufficient thrust");
                            return;
                        }

                        _selection.ThrustRemaining -= thrust;
                        var movementDelta = _gameplayHandler.Referee.MouseLocation - _selection.transform.position;
                        _selection.Speed = Mathf.CeilToInt(movementDelta.magnitude / 5f);
                        _selection.transform.position = _gameplayHandler.Referee.MouseLocation;
                        _selection.transform.rotation = Quaternion.LookRotation(movementDelta);
                        BroadcastMovement(_selection);
                        _gameplayHandler.RemoveShipFromStep(_selection);
                        _selection = null;
                        _gameplayHandler.ClearArcs();
                        _gameplayHandler.NextPlayer();
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