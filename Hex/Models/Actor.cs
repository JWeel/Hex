using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Extensions;

namespace Hex.Models
{
    public class Actor
    {
        #region Constructors

        public Actor(Texture2D texture, Vector2 position)
        {
            this.Texture = texture;
            this.Position = position;
        }

        #endregion

        #region Properties

        public Texture2D Texture { get; }
        public Cube Coordinates { get; set; }
        public Vector2 Position { get; set; }

        #endregion
    }
}