using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Helpers;

namespace Mogi
{
    public class Architect
    {
        #region Constructors

        public Architect(Vector2 virtualWindowSize)
        {
            this.VirtualWindowSize = virtualWindowSize;
        }

        #endregion

        #region Properties

        protected InputHelper Input { get; }
        protected FramerateHelper Framerate { get; }

        protected Vector2 VirtualWindowSize { get; }
        protected Vector2 AspectRatio { get; set; }
        protected Vector2 ClientSizeTranslation { get; set; }

        protected Texture2D BlankTexture { get; set; }

        #endregion

        #region Methods

        public void UpdateCritical()
        {
        }

        public void UpdateOrdinary()
        {
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

        protected void Register<T>()
            where T : class
        {
            var constructors = typeof(T).GetConstructors(BindingFlags.Public);
            if (constructors.Length != 1)
            {
                throw new InvalidOperationException($"Cannot register type '{typeof(T).Name}' because it does not have exactly one public constructor.");
            }
            var constructor = constructors.First();

            var parameters = constructor.GetParameters();
            var arguments = parameters
                .Select(parameter => this.RegisteredTypes.TryGetValue(parameter.ParameterType, out var instance)
                    ? instance : throw new InvalidOperationException($"Cannot register type '{typeof(T).Name}' because dependency type '{parameter.ParameterType}' is not registered."))
                .ToArray();

            var instance = constructor.Invoke(arguments);
            if (instance is IUpdateEarly updateEarly)
                this.OnUpdateEarly += updateEarly.UpdateEarly;
        }

        protected void Unregister<T>()
        {
            if (!this.RegisteredTypes.TryGetValue(typeof(T), out var instance))
                return;
            if (instance is IUpdateEarly updateEarly)
                this.OnUpdateEarly -= updateEarly.UpdateEarly;
        }

        IDictionary<Type, object> RegisteredTypes = new Dictionary<Type, object>();
        event Action<GameTime> OnUpdateEarly;

        private interface IUpdateEarly
        {
            void UpdateEarly(GameTime gameTime);
        }

        private interface IUpdateLater
        {
        }

        #endregion
    }
}