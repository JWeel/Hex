using Microsoft.Xna.Framework;
using System;

namespace Hex.Models
{
    public class Hexagon
    {
        #region Constructors

        public Hexagon(Cube[] coordinates, Vector2[] positions)
        {
            if ((coordinates.Length != 12) || (positions.Length != 12))
                throw new ArgumentException("Arrays must have length 12.");
            this.Coordinates = coordinates;
            this.Positions = positions;
            this.Color = Color.WhiteSmoke;
        }

        #endregion

        #region Properties

        public Cube[] Coordinates { get; }
        public Vector2[] Positions { get; }
        public Color Color { get; set; }

        #endregion
    }
}