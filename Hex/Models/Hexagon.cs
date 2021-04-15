using Hex.Enums;
using Microsoft.Xna.Framework;

namespace Hex.Models
{
    public class Hexagon
    {
        #region Constructors

        public Hexagon(Cube cube, Vector2 position, Vector2 size, TileType type)
        {
            this.Cube = cube;
            this.Position = position;
            this.Size = size;
            this.Middle = position + size / 2;
            this.TileType = type;
        }

        #endregion

        #region Properties

        public Cube Cube { get; }
        public Vector2 Position { get; }
        public Vector2 Size { get; }
        public Vector2 Middle { get; }
        public TileType TileType { get; }
        public Color Color { get; set; }

        #endregion
    }
}