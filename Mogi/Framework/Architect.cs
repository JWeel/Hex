using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Helpers;
using Mogi.Inversion;
using System;

namespace Mogi.Framework
{
    public class Architect : IRoot, ILoad, IUpdate, IDraw
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

        public event Action<GameTime> OnUpdate;
        public event Action<SpriteBatch> OnDraw;
        public event Action<ClientWindow> OnResize;

        #endregion

        #region Methods

        public void Load(DependencyMap dependencyMap)
        {
            var dependency = DependencyHelper.Create(this, dependencyMap);
            this.Input = dependency.Register<InputHelper>();
            dependency.Register<FramerateHelper>();
        }

        public void Update(GameTime gameTime)
        {
            this.OnUpdate?.Invoke(gameTime);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            this.OnDraw?.Invoke(spriteBatch);
        }

        #endregion
    }
}