using System;

namespace Hex.Models
{
    public readonly struct Cube
    {
        #region Constructors

        public Cube(int x, int y, int z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        #endregion

        #region Data Members

        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        #endregion

        #region Instance Methods

        /// <summary> Returns a <see cref="Cube"/> instance with axes flipped to simulate a -60° rotation. </summary>
        public Cube Rotate() =>
            new Cube(-this.Z, -this.X, -this.Y);
            
        #endregion

        #region Static Methods

        public static Cube FromAxial(int q, int r) =>
            new Cube(q, -q - r, r);

        public (int Q, int R) ToAxial() =>
            (this.X, this.Z);

        public static Cube Round(double x, double y, double z)
        {
            var rx = Math.Round(x);
            var ry = Math.Round(y);
            var rz = Math.Round(z);
            var xDiff = Math.Abs(rx - x);
            var yDiff = Math.Abs(ry - y);
            var zDiff = Math.Abs(rz - z);

            if ((xDiff > yDiff) && (xDiff > zDiff))
                rx = -ry - rz;
            else if (yDiff > zDiff)
                ry = -rx - rz;
            else rz = -rx - ry;

            return new Cube((int) rx, (int) ry, (int) rz);
        }

        #endregion

        #region Overridden Methods

        /// <summary> Returns a string representation this instance. </summary>
        public override string ToString() =>
            $"({this.X}, {this.Y}, {this.Z})";

        /// <summary> Indicates whether this instance and a specified object are equal. </summary>
        public override bool Equals(object obj) =>
            (obj is Cube cube && this.Equals(cube));

        /// <summary> Indicates whether this instance and a specified <see cref="Cube"/> are equal. </summary>
        public bool Equals(Cube cube) =>
            ((this.X == cube.X) && (this.Y == cube.Y) && (this.Z == cube.Z));

        /// <summary> Returns the hash code for this instance. </summary>
        public override int GetHashCode() =>
            HashCode.Combine(this.X, this.Y, this.Z);

        #endregion

        #region Operators

        public static bool operator ==(Cube left, Cube right) =>
            left.Equals(right);

        public static bool operator !=(Cube left, Cube right) =>
            !left.Equals(right);

        #endregion
    }
}