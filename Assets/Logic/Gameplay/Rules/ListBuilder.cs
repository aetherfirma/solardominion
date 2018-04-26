using System;
using System.Collections.Generic;
using System.Linq;
using Logic.Gameplay.Ships;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Logic.Gameplay.Rules
{
    internal class ListBuilder
    {
        private bool _setup;
        private readonly Referee _referee;

        public ListBuilder(Referee referee)
        {
            _referee = referee;
        }

        public void Update()
        {
            if (!_setup)
            {
                var itemsForRemoval = new List<GameObject>();
                var lowerButton = Object.Instantiate(_referee.LowerButton, _referee.UiCanvas);
                itemsForRemoval.Add(lowerButton.gameObject);

                lowerButton.onClick.AddListener(delegate
                {
                    foreach (var item in itemsForRemoval)
                    {
                        Object.Destroy(item);
                    }

                    _setup = false;
                    if (_referee.CurrentPlayer < _referee.Players.Length - 1)
                        _referee.CurrentPlayer++;
                    else
                    {
                        _referee.CurrentPlayer = 0;
                        _referee.Phase = GamePhase.Setup;
                    }
                });

                var factionShips = _referee.Ships[(int) _referee.Players[_referee.CurrentPlayer].Faction].Ships;
                for (var n = 0; n < factionShips.Length; n++)
                {
                    var ship = factionShips[n];

                    var selectable = Object.Instantiate(ship, _referee.Camera.transform);
                    itemsForRemoval.Add(selectable.gameObject);
                    selectable.transform.localPosition =
                        new Vector3(-2.5f + 5f / (factionShips.Length - 1) * n, -1, 3.5f);
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
                        itemsForRemoval.Add(selection.gameObject);
                        selection.anchoredPosition =
                            screenPoint + new Vector2(0, +(training - selectable.MinimumTraining + 1.5f) * 100);
                        var label = selection.Find("Label").gameObject.GetComponent<Text>();
                        label.text = string.Format("{0:}\nRating {1:}\n{2:} points", ship.name, training,
                            ship.CalculateCost(training));

                        selection.Find("Buy Button").gameObject.GetComponent<Button>().onClick.AddListener(delegate
                        {
                            // give some kind of feedback
                            if (_referee.PointsLimit - _referee.Players[_referee.CurrentPlayer].FleetCost() < ship.CalculateCost(unmodifiedTraining)) return;

                            var oldLength = _referee.Players[_referee.CurrentPlayer].Fleet.Length;
                            Array.Resize(ref _referee.Players[_referee.CurrentPlayer].Fleet, oldLength + 1);
                            var newShip = Object.Instantiate(ship);
                            newShip.name = ship.name;
                            newShip.gameObject.active = false;
                            newShip.Training = unmodifiedTraining;
                            _referee.Players[_referee.CurrentPlayer].Fleet[oldLength] = newShip;

                            label.text = string.Format("{0:}\nRating {1:}\n{2:} points\n{3:} in fleet", ship.name,
                                unmodifiedTraining,
                                ship.CalculateCost(unmodifiedTraining), _referee.Players[_referee.CurrentPlayer].Fleet.Sum(s =>
                                    s.name == ship.name && s.Training == unmodifiedTraining ? 1 : 0));
                        });

                        selection.Find("Remove Button").gameObject.GetComponent<Button>().onClick.AddListener(delegate
                        {
                            var newFleet = new Ship[_referee.Players[_referee.CurrentPlayer].Fleet.Length - 1];
                            var removed = false;
                            var i = 0;
                            foreach (var ship1 in _referee.Players[_referee.CurrentPlayer].Fleet)
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

                            _referee.Players[_referee.CurrentPlayer].Fleet = newFleet;

                            var shipsRemaining = _referee.Players[_referee.CurrentPlayer].Fleet.Sum(s =>
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

            _referee.DisplayUpperText(string.Format("{0:} Points Remaining", _referee.PointsLimit - _referee.Players[_referee.CurrentPlayer].FleetCost()));
        }
    }
}