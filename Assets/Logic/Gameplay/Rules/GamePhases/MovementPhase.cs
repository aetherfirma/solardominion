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
        private GameObject _movementPentagon, _thrustPentagon;
        private PentagonRenderer _movementMarker;

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
                _gameplayHandler.Referee.DisplayUpperText(string.Format("Waiting for {0:} to play",
                    _gameplayHandler.CurrentPlayer.Faction));

                if (_gameplayHandler.Referee.LastObservedInstruction < state.turns.Count)
                {
                    for (var i = _gameplayHandler.Referee.LastObservedInstruction; i < state.turns.Count; i++)
                    {
                        var turn = state.turns[i];
                        if (turn.player != _gameplayHandler.Referee.PlayerUuid && turn.action == TurnType.MovementPhase)
                        {
                            var ship = _gameplayHandler.CurrentPlayer.Fleet.Single(s => s.ShipUuid == turn.ship);
                            _gameplayHandler.Referee.FlashMessage(string.Format("Just recieved movement for {0:}",
                                ship.Name()));

                            var newPosition = new Vector3(turn.location[0], 0, turn.location[1]);

                            var leadingPoint = ship.Position +
                                               ship.transform.rotation * (Vector3.forward * ship.Speed * 5);
                            var distance = newPosition - leadingPoint;
                            var thrust = Mathf.CeilToInt(distance.magnitude / ship.DistancePerThrust());
                            if (thrust > ship.ThrustRemaining)
                            {
                                _gameplayHandler.Referee.FlashMessage("Cannot move here, insufficient thrust");
                                return;
                            }

                            ship.ThrustRemaining -= thrust;
                            var movementDelta = newPosition - ship.Position;
                            ship.Speed = Mathf.CeilToInt(movementDelta.magnitude / 5f);
                            ship.Position = newPosition;
                            ship.transform.rotation = Quaternion.LookRotation(movementDelta);

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
                    _gameplayHandler.CircleSelectableShips();

                    if (Input.GetMouseButtonUp(0) && _gameplayHandler.Referee.MouseSelection)
                    {
                        var ship = _gameplayHandler.Referee.MouseSelection.GetComponent<Ship>();
                        if (ship != null && _gameplayHandler.IsShipSelectable(ship))
                        {
                            _gameplayHandler.ClearArcs();
                            _selection = ship;

                            _movementPentagon =
                                PentagonRenderer.CreatePentagon(0.1f, _gameplayHandler.Referee.TextPrefab);
                            _movementPentagon.transform.position =
                                ship.Position + ship.transform.rotation * (Vector3.forward * ship.Speed * 5) +
                                new Vector3(0, -3, 0);
                            _movementPentagon.transform.rotation = ship.transform.rotation;

                            _thrustPentagon =
                                PentagonRenderer.CreatePentagon(0.25f, _gameplayHandler.Referee.TextPrefab);

                            _movementMarker = _thrustPentagon.GetComponent<PentagonRenderer>();
                        }
                    }
                }
                else
                {
                    var targetLocation = _gameplayHandler.Referee.MouseLocation;
                    var movementDelta = targetLocation - _selection.Position;
                    var eventualRotation = Quaternion.LookRotation(movementDelta);
                    var leadingPoint = _selection.Position +
                                       _selection.transform.rotation * (Vector3.forward * _selection.Speed * 5);
                    var distance = targetLocation - leadingPoint;

                    if (distance.magnitude < 1)
                    {
                        targetLocation = leadingPoint;
                        distance = Vector3.zero;
                    }

                    var thrust = Mathf.CeilToInt(distance.magnitude / _selection.DistancePerThrust());
                    var newSpeed = Mathf.CeilToInt(movementDelta.magnitude / 5f);


                    if (_thrustPentagon != null)
                    {
                        _thrustPentagon.transform.position = new Vector3(0, -3, 0) + targetLocation;
                        _thrustPentagon.transform.rotation = eventualRotation;

                        if (thrust <= _selection.ThrustRemaining)
                        {
                            _movementMarker.Text = string.Format("New Speed {0:}\nCost {1:} Thrust\n{2:} Remaining",
                                newSpeed, thrust, _selection.ThrustRemaining - thrust);
                            _movementMarker.Color = Color.white;
                        }
                        else
                        {
                            _movementMarker.Text = "INSUFFICIENT THRUST";
                            _movementMarker.Color = Color.red;
                        }
                    }

                    if (Input.GetMouseButtonUp(0))
                    {
                        if (thrust > _selection.ThrustRemaining)
                        {
                            _gameplayHandler.Referee.FlashMessage("Cannot move here, insufficient thrust");
                            return;
                        }

                        Object.Destroy(_movementPentagon);
                        _movementPentagon = null;
                        Object.Destroy(_thrustPentagon);
                        _thrustPentagon = null;
                        _movementMarker = null;

                        _selection.ThrustRemaining -= thrust;
                        _selection.Speed = newSpeed;
                        _selection.Position = targetLocation;
                        _selection.transform.rotation = eventualRotation;
                        BroadcastMovement(_selection);
                        _gameplayHandler.RemoveShipFromCurrentStep(_selection);
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
                location = new[] {ship.Position.x, ship.Position.z},
                rotation = ship.transform.rotation.eulerAngles.y,
                speed = ship.Speed
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