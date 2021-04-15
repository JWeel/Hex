namespace Hex.Enums
{
    public enum ControlState
    {
        Normal,
        Hover,
        Click,
        Disabled,
        Hidden
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
        Rectangle,
        Triangle,
        Parallelogram,
        Line
    }

    public enum PointyHexagonDirection
    {
        Left,
        UpLeft,
        UpRight,
        Right,
        DownRight,
        DownLeft
    }
}