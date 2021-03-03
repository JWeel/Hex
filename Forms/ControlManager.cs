using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Forms
{
    // inspired by https://github.com/csharpskolan/MonoGame.UI.Forms
    public abstract class ControlManager
    {
        private SpriteBatch _spriteBatch;
        private DrawHelper _drawHelper;
        private Control _selectedControl;
        private Control _lastHoveredControl;

        private Vector2 _dragPositionOffset;
        private bool _isDragging;

        private MouseState _prevMouseState;
        private KeyboardState _prevKeyboardState;

        public List<Control> Controls { get; private set; }

        #region Constructor

        public ControlManager(GraphicsDevice graphicsDevice, Action<Action<ContentManager>> subscribeToLoad, Action<Action<GameTime>> subscribeToUpdate, Action<Action<SpriteBatch>> subscribeToDraw)
        {
            graphicsDevice1 = graphicsDevice;

            _spriteBatch = new SpriteBatch(graphicsDevice1);
            subscribeToLoad(this.LoadContent);
            subscribeToUpdate(this.Update);
            subscribeToDraw(this.Draw);

            Controls = new List<Control>();
        }

        #endregion

        public abstract void InitializeComponent();

        #region DrawableGameComponent

        GraphicsDevice graphicsDevice1;
        protected void LoadContent(ContentManager content) =>
            this.LoadContent(graphicsDevice1, content);

        protected void LoadContent(GraphicsDevice graphicsDevice, ContentManager content)
        {
            _drawHelper = new DrawHelper(_spriteBatch, content, graphicsDevice);

            InitializeComponent();
            _drawHelper.ReloadResources(Controls);
        }

        public void Update(GameTime gameTime)
        {
            var mouseState = Mouse.GetState();

            var hoverControl = FindControlAt(mouseState.Position, Controls);
            if (_lastHoveredControl != hoverControl)
            {
                _lastHoveredControl?.OnMouseLeave();
                hoverControl?.OnMouseEnter();
            }


            _lastHoveredControl = hoverControl;

            if (mouseState.LeftButton == ButtonState.Pressed
                && _prevMouseState.LeftButton == ButtonState.Released)
            {
                if (_lastHoveredControl != null)
                {
                    _selectedControl = _lastHoveredControl;
                    _selectedControl.OnMouseDown();

                    if (_selectedControl is Form)
                    {
                        var form = _selectedControl as Form;
                        _selectedControl.ZIndex = Controls.Last().ZIndex + 1;
                        if (form.IsMovable)
                        {
                            _dragPositionOffset = mouseState.Position.ToVector2() - _selectedControl.Location;
                            _isDragging = true;
                        }
                    }
                }
            }

            if (mouseState.LeftButton == ButtonState.Released
                && _prevMouseState.LeftButton == ButtonState.Pressed)
            {
                _isDragging = false;
                if (_selectedControl != null)
                {
                    _selectedControl.OnMouseUp();

                    if (_selectedControl == FindControlAt(mouseState.Position, Controls))
                        _selectedControl.OnClicked();
                }
            }

            if (_isDragging)
            {
                _selectedControl.Location = mouseState.Position.ToVector2() - _dragPositionOffset;
            }

            Controls = Controls.OrderBy(c => c.ZIndex).ToList();
            _prevMouseState = mouseState;

            foreach (var control in Controls)
            {
                control.Update(gameTime);
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            _spriteBatch.Begin();

            foreach (var control in Controls)
            {
                control.Draw(_drawHelper);
            }

            _spriteBatch.End();
        }

        #endregion

        private Control FindControlAt(Point position, IEnumerable<Control> controls)
        {
            var control = controls.LastOrDefault(c => c.Contains(position));
            if (control is IControls)
                return ((IControls) control).FindControlAt(position);
            else
                return control;

        }
    }
}