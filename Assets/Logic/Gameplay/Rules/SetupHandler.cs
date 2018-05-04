using System.Collections.Generic;
using System.Linq;
using Logic.Gameplay.Ships;
using Logic.Network;
using Logic.Utilities;
using UnityEngine;
using UnityEngine.Networking;

namespace Logic.Gameplay.Rules
{
    internal class SetupHandler
    {
        private readonly Referee _referee;
        private bool _setup, _headingSet;
        private Ship _selection;

        public SetupHandler(Referee referee)
        {
            _referee = referee;
        }

        public void Update()
        {
            if (!_setup) Setup();

            var state = _referee.UpdateGameState();

            if (_referee.CurrentPlayer == _referee.LocalPlayer)
            {
                _referee.DisplayUpperText(string.Format("{0:} Turn To Deploy",
                    _referee.Players[_referee.CurrentPlayer].Faction));

                if (_selection == null) Selection();
                else if (!_headingSet) SetPosition();
                else SetHeading();
            }
            else
            {
                _referee.DisplayUpperText(string.Format("Waiting for {0:} to deploy",
                    _referee.Players[_referee.CurrentPlayer].Faction));

                if (_referee.LastObservedInstruction < state.turns.Count)
                {
                    for (int i = _referee.LastObservedInstruction; i < state.turns.Count; i++)
                    {
                        var turn = state.turns[i];
                        var ship = _referee.Players[_referee.CurrentPlayer].Fleet.Single(s => s.ShipUuid == turn.ship);
                        ship.transform.position = new Vector3(turn.location[0], 0, turn.location[1]);
                        ship.Speed = turn.speed;
                        ship.transform.rotation = Quaternion.Euler(0, turn.rotation, 0);
                        ship.Deployed = true;
                        _referee.LastObservedInstruction = i;
                        if (!FindNextSetupPlayer()) _referee.Phase = GamePhase.Play;
                    }
                }
            }
        }

        private void BroadcastDeployment(Ship ship)
        {
            var turn = new Turn
            {
                action = TurnType.Deploy,
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
                www => _referee.FlashMessage("There was a server error (" + www.responseCode +  ") creating a game\n"+www.error),
                www => _referee.FlashMessage("There was a network error creating a game\n"+www.error)
            );
        }

        private void SetHeading()
        {
            var ray = _referee.Camera.ScreenPointToRay(Input.mousePosition);
            float enter;
            _referee.PlaySurface.Raycast(ray, out enter);
            var hitPoint = ray.GetPoint(enter);
            var delta = hitPoint - _selection.transform.position;
            _selection.transform.rotation = Quaternion.LookRotation(delta);
            if (Input.GetMouseButtonUp(0))
            {
                if (Mathf.Abs(hitPoint.x) < _referee.PlayArea / 2f &&
                    Mathf.Abs(hitPoint.z) < _referee.PlayArea / 2f)
                {
                    _headingSet = false;
                    _selection.Speed = Mathf.CeilToInt(delta.magnitude / 5);
                    _selection.Deployed = true;
                    BroadcastDeployment(_selection);
                    _selection = null;

                    if (!FindNextSetupPlayer()) _referee.Phase = GamePhase.Play;
                }
                else _referee.FlashMessage("Cannot set heading and speed, ship would fly off table", 10);
            }
        }

        private void SetPosition()
        {
            var ray = _referee.Camera.ScreenPointToRay(Input.mousePosition);
            float enter;
            _referee.PlaySurface.Raycast(ray, out enter);
            var hitPoint = ray.GetPoint(enter);
            _selection.transform.position = hitPoint;
            if (Input.GetMouseButtonUp(0))
            {
                if (_referee.ValidShipPosition(_selection.transform.position))
                {
                    if (Mathf.Abs(hitPoint.x) < _referee.PlayArea / 2f &&
                        Mathf.Abs(hitPoint.z) < _referee.PlayArea / 2f) _headingSet = true;
                    else _referee.FlashMessage("Cannot place ship here, out of bounds", 10);
                }
                else _referee.FlashMessage("Cannot place ship here, too close to something else", 10);
            }
        }

        private void Selection()
        {
            var direction = 360f / _referee.Players.Length * _referee.CurrentPlayer;
            _referee.CameraOperator.SetCameraPosition(
                Quaternion.Euler(0, direction, 0) * new Vector3(0, 0, -20),
                direction,
                3.5f
            );

            if (Input.GetMouseButtonUp(0))
            {
                RaycastHit hitInfo;
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo, 200))
                {
                    var ship = hitInfo.collider.gameObject.GetComponentInChildren<Ship>();
                    if (ship != null && !ship.Deployed && ship.Player.Uuid == _referee.PlayerUuid) _selection = ship;
                }
            }
        }

        private void Setup()
        {
            for (var player = 0; player < _referee.Players.Length; player++)
            {
                var n = 0;
                foreach (var ship in _referee.Players[player].Fleet)
                {
                    ship.gameObject.SetActive(true);
                    ship.transform.position = Quaternion.Euler(0, 360f / _referee.Players.Length * player, 0) *
                                              new Vector3(
                                                  -_referee.PlayArea / 2f + (float) _referee.PlayArea /
                                                  (_referee.Players[player].Fleet.Length - 1) * n, 5,
                                                  -_referee.PlayArea / 2 - 10);
                    ship.transform.rotation = Quaternion.Euler(0, 360f / _referee.Players.Length * player, 0);
                    n++;
                }

                _referee.CurrentPlayer = _referee.Rng.NextInt(0, _referee.Players.Length - 1);
                _setup = true;
            }
        }

        private bool FindNextSetupPlayer()
        {
            var playersDeployed = new HashSet<int>();
            _referee.CurrentPlayer = (_referee.CurrentPlayer + 1) % _referee.Players.Length;
            while (playersDeployed.Count < _referee.Players.Length)
            {
                if (!playersDeployed.Contains(_referee.CurrentPlayer) &&
                    _referee.Players[_referee.CurrentPlayer].Fleet.Sum(ship => ship.Deployed ? 0 : 1) > 0) break;
                playersDeployed.Add(_referee.CurrentPlayer);
                _referee.CurrentPlayer = (_referee.CurrentPlayer + 1) % _referee.Players.Length;
            }

            return playersDeployed.Count != _referee.Players.Length;
        }
    }
}