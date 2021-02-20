using System;

namespace Hex.Models
{
    public readonly struct Cube
    {
        public Cube(int x, int y, int z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public readonly int X;
        public readonly int Y;
        public readonly int Z;

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

        public override string ToString() =>
            $"({this.X}, {this.Y}, {this.Z})";

        public override bool Equals(object obj) =>
            (obj is Cube cube) ? ((this.X == cube.X) && (this.Y == cube.Y) && (this.Z == cube.Z)) : false;

        public override int GetHashCode() =>
            HashCode.Combine(X, Y, Z);

        public static bool operator ==(Cube left, Cube right) =>
            left.Equals(right);

        public static bool operator !=(Cube left, Cube right) =>
            !left.Equals(right);
    }
}