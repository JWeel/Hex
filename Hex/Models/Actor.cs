using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hex.Models
{
    public class Actor
    {
        #region Constructors

        public Actor()
        {
        }

        #endregion

        #region Properties

        public Cube Coordinates { get; set; }
        public Vector2 Position { get; set; }

        #endregion

        #region Methods

        public void Draw(SpriteBatch spriteBatch)
        {
        }

        #endregion
    }
}