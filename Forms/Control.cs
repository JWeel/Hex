﻿using Forms.Effects;
using Microsoft.Xna.Framework;
using System;

namespace Forms
{
    // should be generic of TSelf so can use that in the event handlers
    public abstract class Control
    {
        public bool Enabled { get; set; }
        public bool IsVisible { get; set; }
        public Vector2 Location { get; set; }
        public Vector2 Size { get; set; }
        public Color BackgroundColor { get; set; }
        public string FontName { get; set; }
        public Rectangle HitBox => new Rectangle((int) Location.X, (int) Location.Y, (int) Size.X, (int) Size.Y);
        public int ZIndex { get; set; }

        public Effect HoverEffect { get; set; }

        public event EventHandler Clicked;
        public event EventHandler MouseDown;
        public event EventHandler MouseUp;
        public event EventHandler MouseLeave;
        public event EventHandler MouseEnter;

        protected bool IsPressed;
        protected bool IsHovering;
        protected float Zoom = 1.0f;

        private bool _wasHovering;

        protected Control()
        {
            FontName = "defaultFont";
            IsVisible = true;
        }

        public virtual bool Contains(Point point)
        {
            return HitBox.Contains(point);
        }

        internal abstract void Draw(DrawHelper helper, Vector2 offset);

        internal virtual void Draw(DrawHelper helper)
        {
            if (IsVisible)
                Draw(helper, Vector2.Zero);
        }

        internal virtual void Update(GameTime gameTime)
        {
            Zoom = 1.0f;

            if (IsHovering && HoverEffect != null)
            {
                if (!_wasHovering)
                    HoverEffect.Reset();
                HoverEffect.Update(gameTime);
                Zoom = HoverEffect.Zoom;
            }

            _wasHovering = IsHovering;
        }

        internal virtual void LoadContent(DrawHelper helper)
        {
            if (!string.IsNullOrEmpty(FontName))
                helper.LoadFont(FontName);
        }

        internal virtual void OnMouseDown()
        {
            IsPressed = true;
            MouseDown?.Invoke(this, new EventArgs());
        }

        internal virtual void OnMouseUp()
        {
            IsPressed = false;
            MouseUp?.Invoke(this, new EventArgs());
        }

        internal virtual void OnMouseEnter()
        {
            IsHovering = true;
            MouseEnter?.Invoke(this, new EventArgs());
        }

        internal virtual void OnMouseLeave()
        {
            IsHovering = false;
            MouseLeave?.Invoke(this, new EventArgs());
        }

        internal virtual void OnClicked()
        {
            Clicked?.Invoke(this, new EventArgs());
        }
    }
}