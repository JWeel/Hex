using Hex.Phases;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mogi.Extensions;
using Mogi.Helpers;
using Mogi.Inversion;

namespace Hex.Helpers
{
    public class OperationHelper : IRegister, IUpdate<NormalUpdate>, IDraw<BackgroundDraw>, IActivate
    {
        #region Constructors

        public OperationHelper(InputHelper input, Texture2D blankTexture, ContentManager content)
        {
            this.Input = input;
            this.BlankTexture = blankTexture;
            this.BackgroundTexture = content.Load<Texture2D>("graphics/operation/background1");
        }

        #endregion

        #region Data Members

        public bool IsActive { get; protected set; }

        /// <summary> The rectangle of the widget, control, or component that contains this scenario. </summary>
        public Rectangle Container { get; protected set; }

        /// <summary> The size of the widget, control, or component that contains this scenario. </summary>
        public Vector2 ContainerSize => this.Container.Size.ToVector2();

        /// <summary> The unbound size of the operation space. This is the max of <see cref="ContainerSize"/> and <see cref="ContainerSize"/>. </summary>
        public Vector2 OperationSize { get; protected set; }

        /// <summary> A transform matrix that scales and moves the scenario relative to its internal camera. </summary>
        public Matrix TranslationMatrix => this.Camera.TranslationMatrix;

        protected InputHelper Input { get; }
        protected Texture2D BlankTexture { get; }
        protected Texture2D BackgroundTexture { get; }

        protected Texture2D OperatorSpritesheet { get; set; }
        protected Texture2D HairSpritesheet { get; set; }

        protected CameraHelper Camera { get; set; }
        protected OperatorHelper Operator { get; set; }

        #endregion

        #region Public Methods

        public void Register(DependencyHandler dependency)
        {
            using (new DependencyScope(dependency))
            {
                this.Camera = dependency.Register<CameraHelper>();
                this.Operator = dependency.Register<OperatorHelper>();
            }
        }

        public void Arrange(Rectangle container)
        {
            this.Container = container;

            // the real size of the operation is the max of the map and the containing rectangle
            this.OperationSize = Vector2.Max(this.ContainerSize , this.ContainerSize);

            this.Camera.Arrange(this.OperationSize, this.Container);
            this.Camera.AllowKeyMovement = false;
            this.Camera.AllowKeyZoom = false;
            this.Camera.SetZoom(1.5f);
            this.Camera.CenterOn(this.Operator.ActiveOperator.Position);
        }

        public void Update(GameTime gameTime)
        {
            if (this.Input.KeyPressed(Keys.C))
                this.Camera.Center();

            if (!this.Input.KeysDownAny(Keys.W, Keys.A, Keys.D, Keys.S))
                return;

            if (this.Operator.ActiveOperator != null)
                this.Camera.CenterOn(this.Operator.ActiveOperator.Position);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            var size = this.OperationSize;// + new Vector2(2); // 1px rounding offset on each side
            spriteBatch.DrawTo(this.BackgroundTexture, size.ToRectangle(), Color.White, depth: .01f);
        }

        public void Activate()
        {
            this.IsActive = true;
            this.Camera.Activate();
            this.Operator.Activate();
        }

        public void Deactivate()
        {
            this.IsActive = false;
            this.Camera.Deactivate();
            this.Operator.Deactivate();
        }

        #endregion
    }
}