using System;

namespace Logic.Gameplay.Ships
{
    [Serializable]
    public class ShipSystemPosition
    {
        public ShipSystem System;
        public int X, Y, Width, Height;

        public ShipSystemPosition(ShipSystem system, int x, int y, int width, int height)
        {
            System = system;

            if (x < 0 || y < 0 || Width < 1 || Height < 1 || x + Width > 5 || y + Height > 4)
                throw new ArgumentException("Invalid Positon");
            
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}