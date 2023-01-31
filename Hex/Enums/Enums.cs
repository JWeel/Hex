using System;

namespace Hex.Enums
{
    public enum State
    {
        Storybook,
        Designer,
        Stage
    }

    public enum TileType
    {
        Grass,
        Mountain,
        Sea
    }

    public enum Shape
    {
        Hexagon,
        //Donut, // TODO add donut
        Rectangle,
        Triangle,
        Parallelogram,
        Line
    }

    // TODO separate Rectangular, Hexagonal and triangular direction enums
    /// <summary> Represents a three-dimensional direction in a two-dimensional space. </summary>
    [Flags]
    public enum Direction
    {
        None = 0,
        UpRight = 1,    // 0b_000001,
        Right = 2,      // 0b_000010,
        DownRight = 4,  // 0b_000100,
        DownLeft = 8,   // 0b_001000,
        Left = 16,      // 0b_010000,
        UpLeft = 32,    // 0b_100000,
    }

    [Flags]
    public enum BorderType
    {
        None,
        Small = 1,
        Large = 2,
        Edge = 4,
        Slope = 8,
    }
}