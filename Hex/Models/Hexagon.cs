using Hex.Enums;
using Microsoft.Xna.Framework;

namespace Hex.Models
{
    public class Hexagon
    {
        #region Constructors

        public Hexagon(Cube cube, Vector2 position, Vector2 size, int elevation, Direction slope, TileType type)
        {
            this.Cube = cube;
            this.Position = position;
            this.Size = size;
            this.Middle = position + size / 2;
            this.Elevation = elevation;
            this.TileType = type;
            this.Slope = slope;
        }

        #endregion

        #region Properties

        public Cube Cube { get; }
        public Vector2 Position { get; }
        public Vector2 Size { get; }
        public Vector2 Middle { get; }
        public int Elevation { get; set; }
        public Direction Slope { get;  set; }
        public TileType TileType { get; }
        public Color Color { get; set; }

        #endregion
    }
}