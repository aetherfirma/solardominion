﻿using UnityEngine;

namespace Logic.Gameplay.Ships
{
    public class ShipSystem : MonoBehaviour
    {
        public SystemType Type;
        public ShipSystem[] SubSystems;
        public int Thrust;
        public int Defence;
        public int Shots, Range, Damage;
        public Orders[] Orders;
        public GameObject Model;
        public bool Displayed;
        public Texture2D[] CardImages;
        public Texture2D Icon;
        public int[] Cost;
    }
}