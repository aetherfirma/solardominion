﻿using Logic.Gameplay.Ships;
using UnityEditor;
using UnityEngine;

namespace Logic.Utilities
{
    public class DebugShip : MonoBehaviour
    {
        public RectTransform Canvas;
        public ShipCard ShipCard;
        public bool[] Selectable;
        private ShipCard _shipCard;

        private void Start()
        {
            var ship = GetComponent<Ship>();
            ship.SetupStatusArrays();
            Selectable = new bool[ship.Systems.Length];
            _shipCard = Instantiate(ShipCard, Canvas);
            _shipCard.Ship = ship;
        }

        private void Update()
        {
            _shipCard.Selectable = Selectable;
        }
    }
}