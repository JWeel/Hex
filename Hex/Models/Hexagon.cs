using Microsoft.Xna.Framework;

namespace Hex.Models
{
    public class Hexagon
    {
        public Hexagon(int x, int y, Color color)
        {
            this.X = x;
            this.Y = y;
            this.Color = color;
        }

        public int X { get; }
        public int Y { get; }
        public Color Color { get; }
        public Vector2 PositionPointyTop { get; set; }
        public Vector2 PositionFlattyTop { get; set; }
    }
}