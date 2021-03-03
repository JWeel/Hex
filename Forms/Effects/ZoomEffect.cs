using Microsoft.Xna.Framework;

namespace Forms.Effects
{
    public class ZoomEffect : Effect
    {
        public float ZoomTo { get; set; }
        public int Duration { get; set; }

        public override void Reset()
        {
            Zoom = 1.0f;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            float delta = (ZoomTo - 1.0f) / (float)Duration;

            if (Zoom < ZoomTo)
            {
                Zoom += delta;
            }
        }
    }
}