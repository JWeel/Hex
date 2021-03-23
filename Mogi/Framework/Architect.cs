using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Helpers;
using Mogi.Inversion;

namespace Mogi.Framework
{
    public class Architect : IRoot, IRegister, IUpdate<NormalUpdate>, IDraw<MenuDraw>
    {
        #region Constructors

        public Architect()
        {
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
            this.Input = dependency.Register<InputHelper>();
        }

        public void Update(GameTime gameTime)
        {
            // this.OnUpdate?.Invoke(gameTime);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // this.OnDraw?.Invoke(spriteBatch);
        }

        #endregion
    }
}