using System;
using Microsoft.Xna.Framework;

namespace Hex.Extensions
{
    public static class Vector2Extensions
    {
        #region Print

        public static string Print(this Vector2 vector) =>
            $"({vector.X}, {vector.Y})";

        public static string PrintRounded(this Vector2 vector) =>
            $"({vector.X:0}, {vector.Y:0})";

        #endregion

        #region Odd to Even
            
        public static Vector2 IfOddAddOne(this Vector2 vector) =>
            new Vector2(((int) vector.X).IfOddAddOne(), ((int) vector.Y).IfOddAddOne());
            
        public static Vector2 IfOddSubtractOne(this Vector2 vector) =>
            new Vector2(((int) vector.X).IfOddSubtractOne(), ((int) vector.Y).IfOddSubtractOne());

        #endregion

        #region Floored

        // cannot call it floor because thats a void method (which mutates the struct yuck)
        public static Vector2 Floored(this Vector2 vector) =>
            Vector2.Floor(vector);
            
        #endregion

        #region Absolute

        public static Vector2 Absolute(this Vector2 vector) =>
            new Vector2(Math.Abs(vector.X), Math.Abs(vector.Y));
            
        #endregion
    }
}