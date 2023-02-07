using Hex.Models.Operators;
using Hex.Phases;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mogi.Helpers;
using Mogi.Inversion;
using System.Collections.Generic;
using System.Linq;

namespace Hex.Helpers
{
    public class OperatorHelper : IUpdate<NormalUpdate>, IDraw<ForegroundDraw>, IActivate
    {
        #region Constructors

        public OperatorHelper(InputHelper input, ContentManager content)
        {
            this.Input = input;
            this.Content = content;
            this.Operators = new List<Operator>();

            this.Add(new Vector2(500, 500));
            this.ActiveOperator = this.Operators.First();
        }

        #endregion

        #region Properties

        public bool IsActive { get; protected set; }

        public List<Operator> Operators { get; }

        public Operator ActiveOperator { get; protected set; }

        protected InputHelper Input { get; }
        protected ContentManager Content { get; }

        #endregion

        #region Methods

        public void Reset()
        {
            this.Operators.Clear();
        }

        public void Add(Vector2 position)
        {
            var op = new Operator(this.Input, this.Content);
            op.MoveTo(position);
            this.Operators.Add(op);
        }

        public void Update(GameTime gameTime)
        {
            if (this.ActiveOperator != null)
            {
                var movementKeyDown = this.Input.KeysDownAny(Keys.W, Keys.A, Keys.D, Keys.S);
                if (movementKeyDown && !this.ActiveOperator.IsWalking)
                    this.ActiveOperator.StartWalking();
                else if (!movementKeyDown && this.ActiveOperator.IsWalking)
                    this.ActiveOperator.StopWalking();

                var movement = Vector2.Zero;
                if (this.Input.KeyDown(Keys.W))
                    movement.Y = -1;
                if (this.Input.KeyDown(Keys.A))
                    movement.X = -1;
                if (this.Input.KeyDown(Keys.D))
                    movement.X = +1;
                if (this.Input.KeyDown(Keys.S))
                    movement.Y = +1;
                this.ActiveOperator.Move(movement);
            }

            this.Operators.ForEach(x => x.Update(gameTime));
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            this.Operators.ForEach(x => x.Draw(spriteBatch));
        }

        public void Activate()
        {
            this.IsActive = true;
        }

        public void Deactivate()
        {
            this.IsActive = false;
        }

        #endregion
    }
}