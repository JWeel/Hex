using Microsoft.Xna.Framework;

namespace Mogi.Extensions
{
    public static class MatrixExtensions
    {
        #region Invert

        /// <summary> Creates a new <see cref="Matrix"/> containing the inversion of this matrix. </summary>
        public static Matrix Invert(this Matrix matrix) =>
            Matrix.Invert(matrix);

        #endregion
    }
}