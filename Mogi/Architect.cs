using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Helpers;
using Mogi.Inversion;
using System;

namespace Mogi
{
    public class Architect : IRoot, ILoad, IUpdate, IDraw
    {
        #region Constructors

        public Architect()
        {
            // this.VirtualWindowSize = virtualWindowSize;

        }

        #endregion

        #region Properties

        protected InputHelper Input { get; set; }

        protected Vector2 VirtualWindowSize { get; }
        protected Vector2 AspectRatio { get; set; }
        protected Vector2 ClientSizeTranslation { get; set; }

        protected Texture2D BlankTexture { get; set; }

        #endregion

        #region Events

        public event Action<GameTime> OnUpdate;
        public event Action<SpriteBatch> OnDraw;
        public event Action<GraphicsDevice, GameWindow> OnResize;

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

        protected void HandleClientSizeChange()
        {
            // ((SpriteBatch)null)
            // .GraphicsDevice.Viewport.

            // this.AspectRatio = new Vector2(
            //     this.Graphics.PreferredBackBufferWidth / (float) VirtualWindowSize.X,
            //     this.Graphics.PreferredBackBufferHeight / (float) VirtualWindowSize.Y);
            // this.ClientSizeTranslation = new Vector2(
            //     this.Graphics.PreferredBackBufferWidth / (float) this.Window.ClientBounds.Width,
            //     this.Graphics.PreferredBackBufferHeight / (float) this.Window.ClientBounds.Height);
        }

        #endregion
    }
}