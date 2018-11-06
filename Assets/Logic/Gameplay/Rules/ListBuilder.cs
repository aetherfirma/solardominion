using System;
using System.Collections.Generic;
using System.Linq;
using Logic.Gameplay.Players;
using Logic.Gameplay.Ships;
using Logic.Maths;
using Logic.Network;
using Logic.Utilities;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Logic.Gameplay.Rules
{
    internal class ListBuilder
    {
        private bool _factionSelected, _factionSetup, _setup;
        private readonly Referee _referee;
        private List<GameObject> _itemsForRemoval;

        public ListBuilder(Referee referee)
        {
            _referee = referee;
            _itemsForRemoval = new List<GameObject>();
        }

        public void Update()
        {
            if (!_factionSelected)
            {
                if (!_factionSetup)
                {
                    var factionSelection = Object.Instantiate(_referee.FactionSelection, _referee.UiCanvas);
                    _itemsForRemoval.Add(factionSelection.gameObject);

                    factionSelection.Find("UNM Button").gameObject.GetComponent<Button>().onClick.AddListener(delegate
                    {
                        _referee.Players[_referee.LocalPlayer].Faction = Faction.UNM;
                        _factionSelected = true;
                    });
                    factionSelection.Find("IP3 Button").gameObject.GetComponent<Button>().onClick.AddListener(delegate
                    {
                        _referee.Players[_referee.LocalPlayer].Faction = Faction.IP3;
                        _factionSelected = true;
                    });

                    _factionSetup = true;
                }

                return;
            }

            if (!_setup)
            {
                ClearItems();

                var finishListButton = Object.Instantiate(_referee.LowerButton, _referee.UiCanvas);
                _itemsForRemoval.Add(finishListButton.gameObject);

                finishListButton.onClick.AddListener(delegate
                {
                    ClearItems();

                    var wwwForm = new WWWForm();
                    wwwForm.AddField("player", _referee.PlayerUuid);
                    wwwForm.AddField("roster", _referee.Players[_referee.LocalPlayer].FleetJson());

                    SimpleRequest.Post(
                        _referee.ServerUrl + "/game/" + _referee.GameUuid + "/roster", _referee.Username, _referee.Password, wwwForm,
                        www =>
                        {
                            _referee.SetGameState(GameResponse.FromJson(www.downloadHandler.text));
                            _referee.Phase = GamePhase.Waiting;
                        },
                        www => _referee.FlashMessage("There was a server error (" + www.responseCode +  ") creating a game\n"+www.error),
                        www => _referee.FlashMessage("There was a network error creating a game\n"+www.error)
                    );
                });

                var factionShips = _referee.Ships[(int) _referee.Players[_referee.LocalPlayer].Faction].Ships;
                for (var n = 0; n < factionShips.Length; n++)
                {
                    var ship = factionShips[n];

                    var selectable = Object.Instantiate(ship, _referee.Camera.transform);
                    _itemsForRemoval.Add(selectable.gameObject);
                    selectable.transform.localPosition =
                        new Vector3(-2.5f + 5f / (factionShips.Length - 1) * n, -1, 3.5f);
                    selectable.Position = selectable.transform.position;
                    selectable.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                    selectable.transform.rotation = Quaternion.Euler(0, 180 - 45, 0);

                    Vector2 viewportPoint = _referee.Camera.WorldToViewportPoint(selectable.transform.position);
                    var screenPoint = new Vector2(
                        viewportPoint.x * _referee.UiCanvas.sizeDelta.x - _referee.UiCanvas.sizeDelta.x * 0.5f,
                        viewportPoint.y * _referee.UiCanvas.sizeDelta.y - _referee.UiCanvas.sizeDelta.y * 0.5f);

                    for (var training = selectable.MinimumTraining; training <= selectable.MaximumTraining; training++)
                    {
                        var unmodifiedTraining = training;
                        var selection = Object.Instantiate(_referee.SelectionPanel, _referee.UiCanvas);
                        _itemsForRemoval.Add(selection.gameObject);
                        selection.anchoredPosition =
                            screenPoint + new Vector2(0, +(training - selectable.MinimumTraining + 1.5f) * 100);
                        var label = selection.Find("Label").gameObject.GetComponent<Text>();
                        label.text = string.Format("{0:}\nRating {1:}\n{2:} points", ship.name, training,
                            ship.CalculateCost(training));

                        selection.Find("Buy Button").gameObject.GetComponent<Button>().onClick.AddListener(delegate
                        {
                            // give some kind of feedback
                            if (_referee.CurrentGameState.points_limit - _referee.Players[_referee.LocalPlayer].FleetCost() <
                                ship.CalculateCost(unmodifiedTraining)) return;

                            var oldLength = _referee.Players[_referee.LocalPlayer].Fleet.Length;
                            Array.Resize(ref _referee.Players[_referee.LocalPlayer].Fleet, oldLength + 1);
                            var newShip = ship.Initialise(unmodifiedTraining, _referee.Players[_referee.LocalPlayer]);
                            _referee.Players[_referee.LocalPlayer].Fleet[oldLength] = newShip;

                            label.text = string.Format("{0:}\nRating {1:}\n{2:} points\n{3:} in fleet", ship.name,
                                unmodifiedTraining,
                                ship.CalculateCost(unmodifiedTraining), _referee.Players[_referee.LocalPlayer].Fleet
                                    .Sum(s =>
                                        s.name == ship.name && s.Training == unmodifiedTraining ? 1 : 0));
                        });

                        selection.Find("Remove Button").gameObject.GetComponent<Button>().onClick.AddListener(delegate
                        {
                            var newFleet = new Ship[_referee.Players[_referee.LocalPlayer].Fleet.Length - 1];
                            var removed = false;
                            var i = 0;
                            foreach (var ship1 in _referee.Players[_referee.LocalPlayer].Fleet)
                            {
                                if (!removed && ship1.UUID == ship.UUID && ship1.Training == unmodifiedTraining)
                                {
                                    removed = true;
                                    Object.Destroy(ship1.gameObject);
                                }
                                else
                                {
                                    newFleet[i++] = ship1;
                                }
                            }

                            _referee.Players[_referee.LocalPlayer].Fleet = newFleet;

                            var shipsRemaining = _referee.Players[_referee.LocalPlayer].Fleet.Sum(s =>
                                s.name == ship.name && s.Training == unmodifiedTraining ? 1 : 0);
                            if (shipsRemaining > 0)
                                label.text = string.Format("{0:}\nRating {1:}\n{2:} points\n{3:} in fleet", ship.name,
                                    unmodifiedTraining,
                                    ship.CalculateCost(unmodifiedTraining), shipsRemaining);
                            else
                                label.text = string.Format("{0:}\nRating {1:}\n{2:} points", ship.name,
                                    unmodifiedTraining,
                                    ship.CalculateCost(unmodifiedTraining));
                        });
                    }
                }

                _setup = true;
            }

            _referee.DisplayUpperText(string.Format("{0:} Points Remaining",
                _referee.CurrentGameState.points_limit - _referee.Players[_referee.LocalPlayer].FleetCost()));
        }

        private void ClearItems()
        {
            foreach (var item in _itemsForRemoval)
            {
                Object.Destroy(item);
            }
        }
    }
}