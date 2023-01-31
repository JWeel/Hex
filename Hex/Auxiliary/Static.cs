using Hex.Enums;
using Microsoft.Xna.Framework;
using System.Text;

namespace Hex.Auxiliary
{
    public class Static
    {
        public static StringBuilder Memo { get; } = new StringBuilder();

        public static Rectangle[] FocalSquares { get; set; }

        public static Shape? Shape { get; set; }
    }
}