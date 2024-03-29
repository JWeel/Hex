using Extended.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mogi.Extensions
{
    public static class SpriteBatchExtensions
    {
        #region Draw Methods

        public static void DrawAt(this SpriteBatch spriteBatch, Texture2D texture, Vector2 position,
            Color? color = default, float rotation = 0f, float scale = 1f, float depth = 0f,
            SpriteEffects effects = SpriteEffects.None, Rectangle? sourceRectangle = null)
        {
            spriteBatch.Draw(
                texture: texture,
                position: position,
                sourceRectangle: sourceRectangle,
                color: color ?? Color.White,
                rotation: rotation,
                origin: Vector2.Zero,
                scale: scale,
                effects: effects,
                layerDepth: depth
            );
        }

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
            if (text.IsNullOrEmpty())
                return;

            var shadePosition = position + new Vector2(1 * scale);
            var shadeDepth = depth - 0.01f;

            spriteBatch.DrawString(font, text, shadePosition, Color.Black,
                rotation: 0f, origin: default, scale, SpriteEffects.None, shadeDepth);
            spriteBatch.DrawString(font, text, position, color ?? Color.White,
                rotation: 0f, origin: default, scale, SpriteEffects.None, depth);
        }

        public static void DrawNinePatchRectangle(this SpriteBatch spriteBatch, Texture2D texture,
            Rectangle destinationRectangle, int distanceToMiddle, Color color, float depth = 0f, SpriteEffects effects = SpriteEffects.None)
        {
            // Top left
            spriteBatch.Draw(
                texture,
                new Rectangle(destinationRectangle.Location, new Point(distanceToMiddle)),
                new Rectangle(0, 0, distanceToMiddle, distanceToMiddle),
                color,
                rotation: 0f,
                origin: Vector2.Zero,
                effects,
                depth);

            // Top
            spriteBatch.Draw(
                texture,
                new Rectangle(destinationRectangle.Location + new Point(distanceToMiddle, 0), new Point(destinationRectangle.Width - distanceToMiddle * 2, distanceToMiddle)),
                new Rectangle(distanceToMiddle, 0, texture.Width - distanceToMiddle * 2, distanceToMiddle),
                color,
                rotation: 0f,
                origin: Vector2.Zero,
                effects,
                depth);

            // Top right
            spriteBatch.Draw(
                texture,
                new Rectangle(destinationRectangle.Location + new Point(destinationRectangle.Width - distanceToMiddle, 0), new Point(distanceToMiddle)),
                new Rectangle(texture.Width - distanceToMiddle, 0, distanceToMiddle, distanceToMiddle),
                color,
                rotation: 0f,
                origin: Vector2.Zero,
                effects,
                depth);

            // Middle left
            spriteBatch.Draw(
                texture,
                new Rectangle(destinationRectangle.Location + new Point(0, distanceToMiddle), new Point(distanceToMiddle, destinationRectangle.Height - distanceToMiddle * 2)),
                new Rectangle(0, distanceToMiddle, distanceToMiddle, texture.Height - distanceToMiddle * 2),
                color,
                rotation: 0f,
                origin: Vector2.Zero,
                effects,
                depth);

            // Middle
            spriteBatch.Draw(
                texture,
                new Rectangle(destinationRectangle.Location + new Point(distanceToMiddle), destinationRectangle.Size - new Point(distanceToMiddle * 2)),
                new Rectangle(distanceToMiddle, distanceToMiddle, texture.Width - distanceToMiddle * 2, texture.Height - distanceToMiddle * 2),
                color,
                rotation: 0f,
                origin: Vector2.Zero,
                effects,
                depth);

            // Middle right
            spriteBatch.Draw(
                texture,
                new Rectangle(destinationRectangle.Location + new Point(destinationRectangle.Width - distanceToMiddle, distanceToMiddle), new Point(distanceToMiddle, destinationRectangle.Height - distanceToMiddle * 2)),
                new Rectangle(texture.Width - distanceToMiddle, distanceToMiddle, distanceToMiddle, texture.Height - distanceToMiddle * 2),
                color,
                rotation: 0f,
                origin: Vector2.Zero,
                effects,
                depth);

            // Bottom left
            spriteBatch.Draw(
                texture,
                new Rectangle(destinationRectangle.Location + new Point(0, destinationRectangle.Height - distanceToMiddle), new Point(distanceToMiddle)),
                new Rectangle(0, texture.Height - distanceToMiddle, distanceToMiddle, distanceToMiddle),
                color,
                rotation: 0f,
                origin: Vector2.Zero,
                effects,
                depth);

            // Bottom
            spriteBatch.Draw(
                texture,
                new Rectangle(destinationRectangle.Location + new Point(distanceToMiddle, destinationRectangle.Height - distanceToMiddle), new Point(destinationRectangle.Width - distanceToMiddle * 2, distanceToMiddle)),
                new Rectangle(distanceToMiddle, texture.Height - distanceToMiddle, texture.Width - distanceToMiddle * 2, distanceToMiddle),
                color,
                rotation: 0f,
                origin: Vector2.Zero,
                effects,
                depth);

            // Bottom right
            spriteBatch.Draw(
                texture,
                new Rectangle(destinationRectangle.Location + destinationRectangle.Size - new Point(distanceToMiddle), new Point(distanceToMiddle)),
                new Rectangle(texture.Width - distanceToMiddle, texture.Height - distanceToMiddle, distanceToMiddle, distanceToMiddle),
                color,
                rotation: 0f,
                origin: Vector2.Zero,
                effects,
                depth);
        }

        #endregion
    }
}