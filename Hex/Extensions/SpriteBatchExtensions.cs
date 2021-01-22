using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hex.Extensions
{
    public static class SpriteBatchExtensions
    {
        #region Draw Methods
            
        public static void DrawAt(this SpriteBatch spriteBatch, Texture2D texture, Vector2 position, float scale, Color? color = default, float depth = 0f) =>
            spriteBatch.Draw(
                texture: texture,
                position: position,
                sourceRectangle: null,
                color: color ?? Color.White,
                rotation: 0f,
                origin: Vector2.Zero,
                scale: scale,
                effects: SpriteEffects.None,
                layerDepth: depth
            );
            
        public static void DrawTo(this SpriteBatch spriteBatch, Texture2D texture, Rectangle destinationRectangle, Color? color = default, float depth = 0f) =>
            spriteBatch.Draw(
                texture: texture,
                destinationRectangle: destinationRectangle,
                sourceRectangle: null,
                color: color ?? Color.White,
                rotation: 0f,
                origin: Vector2.Zero,
                effects: SpriteEffects.None,
                layerDepth: depth
            );

        public static void DrawText(this SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 position, Color? color = default, float scale = 1f, float depth = 1f)
        {
            var shadePosition = position + new Vector2(1 * scale);
            var shadeDepth = depth - 0.01f;

            spriteBatch.DrawString(font, text, shadePosition, Color.Black,
                rotation: 0f, origin: default, scale, SpriteEffects.None, shadeDepth);
            spriteBatch.DrawString(font, text, position, color ?? Color.White,
                rotation: 0f, origin: default, scale, SpriteEffects.None, depth);
        }

        #endregion
    }
}