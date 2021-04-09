using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Helpers;
using Mogi.Inversion;

namespace Mogi.Framework
{
    public class Architect : IRegister, IUpdate<NormalUpdate>, IDraw<MenuDraw>
    {
        #region Constructors

        public Architect(InputHelper input, Texture2D blankTexture)
        {
            this.Input = input;
            this.BlankTexture = blankTexture;
        }

        #endregion

        #region Properties

        protected InputHelper Input { get; set; }
        protected Texture2D BlankTexture { get; set; }

        #endregion

        #region Methods

        public void Register(DependencyHandler dependency)
        {
        }

        public void Update(GameTime gameTime)
        {
        }

        public void Draw(SpriteBatch spriteBatch)
        {
        }

        #endregion
    }
}