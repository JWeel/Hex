using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Helpers;
using Mogi.Inversion;

namespace Mogi.Framework
{
    public class Architect : IRoot, IRegister, IUpdate<NormalUpdate>, IDraw<MenuDraw>
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

        #region Events

        public PhasedEvent<GameTime> OnUpdate { get; set; }
        public PhasedEvent<SpriteBatch> OnDraw { get; set; }

        #endregion

        #region Methods

        public void Register(DependencyMap dependencyMap)
        {
            var dependency = DependencyHelper.Create(this, dependencyMap);
        }

        public void Update(GameTime gameTime)
        {
            this.OnUpdate?.Invoke<NormalUpdate>(gameTime);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            this.OnDraw?.Invoke<MenuDraw>(spriteBatch);
        }

        #endregion
    }
}