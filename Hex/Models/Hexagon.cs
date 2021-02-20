using Microsoft.Xna.Framework;

namespace Hex.Models
{
    public class Hexagon
    {
        public Hexagon(int q, int r, Color color)
        {
            this.Q = q;
            this.R = r;
            this.Cube = new Cube(q, -q-r, r);
            this.Color = color;
        }

        public int Q { get; }
        public int R { get; }
        public Cube Cube { get; }
        public Color Color { get; }
        public Vector2 PositionPointyTop { get; set; }
        public Vector2 PositionFlattyTop { get; set; }
    }
}