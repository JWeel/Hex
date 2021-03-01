using Hex.Models;
using Microsoft.Xna.Framework.Graphics;

namespace Hex.Controls
{
    public interface IControl
    {
        DrawableRectangle Outer { get; }
        DrawableRectangle Inner { get; }
    }
}