using Extended.Collections;
using Extended.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGui.Controls
{
    public class Panel : Control<Panel>
    {
        #region Constructors

        public Panel()
        {
            this.Controls = new OrderedSet<IControl>();
        }

        #endregion

        #region Properties

        public OrderedSet<IControl> Controls { get; }

        #endregion

        #region Methods

        public void Add(IControl control) =>
            this.Controls.Insert(control);

        public void Focus(IControl control)
        {
            // Remove does not care if it does not exist in the set
            this.Controls.Remove(control);
            this.Controls.Insert(control);
        }

        public override void Update(GameTime gameTime)
        {
            this.Controls.Each(control => control.Update(gameTime));
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            this.Controls.Each(control => control.Draw(spriteBatch));
        }

        // void a()
        // {
        //     var points = new (float, float)[pointsCount];
        //     for (int i = 0; i < pointsCount; ++i)
        //     {
        //         points[i] = new PointF(
        //             circleRadius.x + Math.Cos(startAngle + i * sweepAngle / pointsCount) * radius,
        //             circleRadius.y + Math.Sin(startAngle + i * sweepAngle / pointsCount) * radius);
        //     }
        // }

        #endregion
    }
}