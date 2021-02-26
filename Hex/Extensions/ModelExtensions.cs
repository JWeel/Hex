using System.Collections.Generic;
using Hex.Models;
using Microsoft.Xna.Framework;

namespace Hex.Extensions
{
    public static class ModelExtensions
    {
        #region To Vector3

        public static Vector3 ToVector3(this Cube cube) =>
            new Vector3(cube.X, cube.Y, cube.Z);

        public static Cube ToRoundCube(this Vector3 vector) =>
            Cube.Round(vector.X, vector.Y, vector.Z);
            
        #endregion
    }
}