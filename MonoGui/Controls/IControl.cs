using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGui.Controls
{
    public interface IControl
    {
        void Update(GameTime gameTime);

        void Draw(SpriteBatch spriteBatch);
    }
}