using Hex.Models;
using Microsoft.Xna.Framework;

namespace Hex.Extensions
{
    public static class ModelExtensions
    {
        #region To Vector3

        /// <summary> Returns a new vector constructed from the three coordinates of this cube. </summary>
        public static Vector3 ToVector3(this Cube cube) =>
            new Vector3(cube.X, cube.Y, cube.Z);

        /// <summary> Returns a cube constructed from rounding the three floating point values in this vector. </summary>
        public static Cube ToRoundedCube(this Vector3 vector) =>
            Cube.Round(vector.X, vector.Y, vector.Z);

        #endregion
    }
}