using Extended.Extensions;
using Hex.Enums;
using Microsoft.Xna.Framework;
using System.Linq;

namespace Hex.Models
{
    public class Hexagon
    {
        #region Constructors

        public Hexagon(Cube cube, Vector2 position)
        {
            this.Cube = cube;
            this.Position = position;

            this.TileType = 
                (cube.X % 7 == cube.Z) ? TileType.Mountain :
                // (cube.Z % 3 == cube.Y-1) ? TileType.Sea :
                TileType.Grass;
        }

        #endregion

        #region Properties

        public Cube Cube { get; }
        public Vector2 Position { get; }
        public TileType TileType { get; }
        public Color Color { get; set; }

        #endregion
    }
}