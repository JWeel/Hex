using Extended.Patterns;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Mogi.Scopes
{
    /// <summary> Provides a mechanism for automatically setting and restoring a scissor rectangle. </summary>
    public class ScissorScope : Scope
    {
        #region Constructors

        /// <summary> Creates a scope which sets a given scissor <see cref="Rectangle"/> on a given <see cref="GraphicsDevice"/> and restores it when disposed. </summary>
        /// <param name="device"> The device on which a scissor will be set. </param>
        /// <param name="rectangle"> The rectangle that will be used as a scissor. </param>
        public ScissorScope(GraphicsDevice device, Rectangle rectangle)
            : base(preOperation: ScissorScope.CreateAction(device, rectangle),
                postOperation: ScissorScope.CreateAction(device, device.ScissorRectangle))
        {
        }

        #endregion

        #region Methods

        // The actions are created in advance in order to preserve thte original scissor rectangle.
        protected static Action CreateAction(GraphicsDevice device, Rectangle rectangle) =>
            () => device.ScissorRectangle = rectangle;

        #endregion
    }
}