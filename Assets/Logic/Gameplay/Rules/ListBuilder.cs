using System;
using System.Collections.Generic;
using System.Linq;
using Logic.Gameplay.Players;
using Logic.Gameplay.Ships;
using Logic.Maths;
using Logic.Network;
using Logic.Utilities;
using TMPro;
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
        private TextMeshProUGUI _title;
        private RectTransform _ui;
        private List<GameObject> _itemsForRemoval;
        private Transform _roster;
        private Transform _shipList;

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

                _ui = Object.Instantiate(_referee.ListBuilderUi, _referee.UiCanvas);
                _itemsForRemoval.Add(_ui.gameObject);
                _title = _ui.Find("Ships/Title").GetComponent<TextMeshProUGUI>();

                var finishListButton = _ui.Find("Finish Button").GetComponent<Button>();

                _shipList = _ui.Find("Ships/Scroll View/Viewport/Content");
                _roster = _ui.Find("Fleet/Scroll View/Viewport/Content");

                RecalculateFleetListing();

                finishListButton.onClick.AddListener(delegate
                {
                    ClearItems();

                    var wwwForm = new WWWForm();
                    wwwForm.AddField("player", _referee.PlayerUuid);
                    wwwForm.AddField("roster", _referee.Players[_referee.LocalPlayer].FleetJson());

                    SimpleRequest.Post(
                        _referee.ServerUrl + "/game/" + _referee.GameUuid + "/roster", _referee.Username,
                        _referee.Password, wwwForm,
                        www =>
                        {
                            _referee.SetGameState(GameResponse.FromJson(www.downloadHandler.text));
                            _referee.Phase = GamePhase.Waiting;
                        },
                        www => _referee.FlashMessage("There was a server error (" + www.responseCode +
                                                     ") creating a game\n" + www.error),
                        www => _referee.FlashMessage("There was a network error creating a game\n" + www.error)
                    );
                });

                var factionShips = _referee.Ships[(int) _referee.Players[_referee.LocalPlayer].Faction].Ships;
                for (var n = 0; n < factionShips.Length; n++)
                {
                    var ship = factionShips[n];

                    var shipListing = Object.Instantiate(_referee.ShipListing, _shipList);
                    shipListing.localPosition = new Vector2(0, -110 * n);

                    shipListing.Find("Ship Icon").GetComponent<Image>().sprite = ship.ShipIcon;

                    var costs = "";

                    for (var training = ship.MinimumTraining; training <= ship.MaximumTraining; training++)
                    {
                        if (costs != "") costs += ", ";
                        costs += string.Format("{0} points at rating {1}", ship.CalculateCost(training), training);
                        
                        var unmodifiedTraining = training;
                        var button = _referee.CreateButton(Vector2.zero, new Vector2(125, 40),
                            string.Format("Buy at rating {0}", training),
                            () =>
                            {
                                if (_referee.CurrentGameState.points_limit -
                                    _referee.Players[_referee.LocalPlayer].FleetCost() <
                                    ship.CalculateCost(unmodifiedTraining))
                                {
                                    _referee.FlashMessage("Insufficient points remaining");
                                    return;
                                }

                                var oldLength = _referee.Players[_referee.LocalPlayer].Fleet.Length;
                                Array.Resize(ref _referee.Players[_referee.LocalPlayer].Fleet, oldLength + 1);
                                var newShip = ship.Initialise(unmodifiedTraining,
                                    _referee.Players[_referee.LocalPlayer]);
                                _referee.Players[_referee.LocalPlayer].Fleet[oldLength] = newShip;

                                RecalculateFleetListing();
                            },
                            shipListing
                        ).GetComponent<RectTransform>();
                        button.anchorMin = new Vector2(1, 0.5f);
                        button.anchorMax = new Vector2(1, 0.5f);
                        button.anchoredPosition = new Vector2(-75, 22 - 44 * (training - ship.MinimumTraining));
                    }

                    shipListing.Find("Ship Description").GetComponent<TextMeshProUGUI>().text = string.Format(
                        "<b><size=24>{0} {1}</size><b>\n{2}\nEquipped with {3}",
                        ship.ClassVariant, ship.Class,
                        costs,
                        ship.Systems.Select(system => system.System.name).ToArray().DescribeList()
                    );

                }

                var content = _shipList.GetComponent<RectTransform>();
                content.sizeDelta = new Vector2(content.sizeDelta.x, 110 * factionShips.Length);

                _setup = true;
            }
        }

        private void RecalculateFleetListing()
        {
            _title.text = string.Format("Fleet Builder - {0} Points Left",
                _referee.CurrentGameState.points_limit - _referee.Players[_referee.LocalPlayer].FleetCost());

            _roster.DestroyAllChildren();

            var n = 0;
            foreach (var ship in _referee.Players[_referee.LocalPlayer].Fleet)
            {
                var listing = Object.Instantiate(_referee.RosterListing, _roster);
                listing.localPosition = new Vector3(0, -n);
                n += 110;
                listing.Find("Ship Icon").GetComponent<Image>().sprite = ship.ShipIcon;
                listing.Find("Ship Description").GetComponent<TextMeshProUGUI>().text = string.Format(
                    "<b><size=16>{0}</size>\n{1} points<b>\n{2} {3}",
                    ship.Name(),
                    ship.CalculateCost(ship.Training),
                    ship.ClassVariant,
                    ship.Class
                );

                var thisShip = ship;

                listing.Find("Remove Button").GetComponent<Button>().onClick.AddListener(() =>
                {
                    var newFleet = new Ship[_referee.Players[_referee.LocalPlayer].Fleet.Length - 1];
                    var removed = false;
                    var i = 0;
                    foreach (var ship1 in _referee.Players[_referee.LocalPlayer].Fleet)
                    {
                        if (!removed && ship1.UUID == thisShip.UUID && ship1.Training == thisShip.Training)
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

                    RecalculateFleetListing();
                });
            }
            
            var content = _roster.GetComponent<RectTransform>();
            content.sizeDelta = new Vector2(content.sizeDelta.x, n);
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