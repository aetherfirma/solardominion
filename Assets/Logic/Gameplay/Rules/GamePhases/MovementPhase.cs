﻿using System.Collections.Generic;
using System.Linq;
using Logic.Display;
using Logic.Gameplay.Ships;
using Logic.Maths;
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
        private readonly List<Popup> _popups = new List<Popup>();
        private RectTransform _coastArrow, _thrustArrow;

        public MovementPhase(GameplayHandler gameplayHandler)
        {
            _gameplayHandler = gameplayHandler;
        }
        
        private void MarkSelectableShips()
        {
            if (_popups.Count == 0)
            {
                foreach (var ship in _gameplayHandler.ShipsInInitiativeStep[_gameplayHandler.CurrentPlayer])
                {
                    _popups.Add(_gameplayHandler.Referee.Popup.Clone("Ready to move", Vector3.zero, ship.transform));
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
                            _gameplayHandler.Referee.Popup.Clone(string.Format("Just recieved movement for {0:}",
                                ship.Name()), Vector3.zero, ship.transform, 0.5f, 5);

                            var newPosition = new Vector3(turn.location[0], 0, turn.location[1]);

                            var leadingPoint = ship.Position +
                                               ship.transform.rotation * (Vector3.forward * ship.Speed * 5);
                            var distance = newPosition - leadingPoint;
                            var thrust = Mathf.CeilToInt(distance.magnitude / ship.DistancePerThrust());
                            if (thrust > ship.ThrustRemaining)
                            {
                                _gameplayHandler.Referee.Popup.Clone("Cannot move here, insufficient thrust", newPosition, _gameplayHandler.Referee.RootTransform, 0.5f, 5);
                                return;
                            }

                            ship.ThrustRemaining -= thrust;
                            var movementDelta = newPosition - ship.Position;
                            ship.Speed = Mathf.CeilToInt(movementDelta.magnitude / 5f);
                            AsteroidDamage(ship, newPosition);
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
                    MarkSelectableShips();

                    if (Input.GetMouseButtonUp(0) && _gameplayHandler.Referee.MouseSelection)
                    {
                        var ship = _gameplayHandler.Referee.MouseSelection.GetComponent<Ship>();
                        if (ship != null && _gameplayHandler.IsShipSelectable(ship))
                        {
                            ClearPopups();
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

                            if (ship.Speed * 5 > 6)
                            {
                                _coastArrow = Object.Instantiate(_gameplayHandler.Referee.OutlineArrow, _gameplayHandler.Referee.RootTransform);
                                _coastArrow.localPosition = ship.Position + ship.transform.rotation * (Vector3.forward * 3) +
                                                            new Vector3(0, -3, 0);
                                _coastArrow.sizeDelta = new Vector2(256, (ship.Speed * 5 - 6) * 100);
                                _coastArrow.localRotation = Quaternion.Euler(90, 0, -ship.transform.rotation.eulerAngles.y);
                            }
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

                    if (_thrustArrow == null && movementDelta.magnitude > 6)
                    {
                        _thrustArrow = Object.Instantiate(_gameplayHandler.Referee.FullArrow, _gameplayHandler.Referee.RootTransform);
                        _thrustArrow.localPosition = _selection.Position + eventualRotation * (Vector3.forward * 3) +
                                                    new Vector3(0, -3, 0);
                        _thrustArrow.sizeDelta = new Vector2(256, (movementDelta.magnitude - 6) * 100);
                        _thrustArrow.localRotation = Quaternion.Euler(90, 0, -eventualRotation.eulerAngles.y);
                    }
                    else if (_thrustArrow != null)
                    {
                        if (movementDelta.magnitude > 6)
                        {
                            _thrustArrow.localPosition = _selection.Position + eventualRotation * (Vector3.forward * 3) +
                                                         new Vector3(0, -3, 0);
                            _thrustArrow.sizeDelta = new Vector2(256, (movementDelta.magnitude - 6) * 100);
                            _thrustArrow.localRotation = Quaternion.Euler(90, 0, -eventualRotation.eulerAngles.y);
                        }
                        else
                        {
                            Object.Destroy(_thrustArrow.gameObject);
                            _thrustArrow = null;
                        }
                    }

                    if (Input.GetMouseButtonUp(0))
                    {
                        if (thrust > _selection.ThrustRemaining)
                        {
                            _gameplayHandler.Referee.Popup.Clone("Cannot move here, insufficient thrust", targetLocation, _gameplayHandler.Referee.RootTransform, 0.5f, 5);
                            return;
                        }
                        
                        if (_coastArrow != null)
                        {
                            Object.Destroy(_coastArrow.gameObject);
                            _coastArrow = null;
                        }
                        if (_thrustArrow != null)
                        {
                            Object.Destroy(_thrustArrow.gameObject);
                            _thrustArrow = null;
                        }

                        Object.Destroy(_movementPentagon);
                        _movementPentagon = null;
                        Object.Destroy(_thrustPentagon);
                        _thrustPentagon = null;
                        _movementMarker = null;

                        _selection.ThrustRemaining -= thrust;
                        _selection.Speed = newSpeed;
                        AsteroidDamage(_selection, targetLocation);
                        _selection.Position = targetLocation;
                        _selection.transform.rotation = eventualRotation;
                        BroadcastMovement(_selection);
                        _gameplayHandler.RemoveShipFromCurrentStep(_selection);
                        _selection = null;
                        ClearPopups();
                        _gameplayHandler.NextPlayer();
                    }
                }
            }
        }

        private void AsteroidDamage(Ship ship, Vector3 newPosition)
        {
            var rng = _gameplayHandler.Referee.Rng;

            Debug.DrawLine(ship.Position, newPosition, Color.red, 10);
            foreach (var asteroidField in _gameplayHandler.Referee.AsteroidFields)
            {
                var point = asteroidField.Location.ClosestPointOnLine(ship.Position, newPosition);
                var distance = asteroidField.Location.Distance(point);
                Debug.DrawLine(asteroidField.Location, point, Color.blue, 10);
                
                if (!(distance <= asteroidField.Size + 2.5f)) continue;
                var damage = rng.D6() - 1;
                if (damage <= 0) continue;
                
                ship.TakeDamage(rng, damage);
                if (!ship.Alive)
                {
                    _gameplayHandler.DestroyShip(ship);
                    _gameplayHandler.Referee.Popup.Clone(string.Format("{0} has hit an asteroid and was destroyed", ship.Name()), asteroidField.Location, _gameplayHandler.Referee.RootTransform, 0.5f, 5);
                }
                else
                {
                    _gameplayHandler.Referee.Popup.Clone(string.Format("{0} has hit an asteroid and took {1} damage", ship.Name(), damage), asteroidField.Location, _gameplayHandler.Referee.RootTransform, 0.5f, 5);                        
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