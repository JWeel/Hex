using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGui.Controls
{
    /// <summary> Defines a graphical user interface element. </summary>
    public interface IControl
    {
        #region Properties

        bool IsActive { get; }

        #endregion 

        #region Methods

        void Update(GameTime gameTime);

        void Draw(SpriteBatch spriteBatch);

        #endregion
    }
}