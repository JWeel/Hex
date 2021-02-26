using Microsoft.Xna.Framework;
using System.Linq;

namespace Hex.Models
{
    public class Hexagon
    {
        #region Constructors

        public Hexagon((Cube Cube, Vector2 Position)[] coordinates)
        {
            this.Cubes = coordinates.Select(x => x.Cube).ToArray();
            this.Positions = coordinates.Select(x => x.Position).ToArray();
            this.Color = Color.LightYellow;
        }

        #endregion

        #region Properties

        public Cube[] Cubes { get; }
        public Vector2[] Positions { get; }
        public Color Color { get; set; }

        #endregion
    }
}