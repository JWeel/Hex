using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Helpers;
using Mogi.Inversion;

namespace Hex.Helpers
{
    public class StageHelper : IRoot, IRegister
    {
        #region Constructors

        public StageHelper(InputHelper input)
        {
            this.Camera = new CameraHelper(() => this.CameraBounds, () => this.ContainerSize, input);
        }

        #endregion

        #region Data Members

        public PhasedEvent<GameTime> OnUpdate { get; set; }
        public PhasedEvent<SpriteBatch> OnDraw { get; set; }

        protected CameraHelper Camera { get; }
        protected TilemapHelper Tilemap{ get; set; }

        protected Vector2 CameraBounds { get; set; }
        protected Vector2 ContainerSize { get; set; }

        #endregion

        #region Methods

        public void Register(DependencyMap dependencyMap)
        {
            var dependency = DependencyHelper.Create(this, dependencyMap);

            this.Tilemap = dependency.RegisterWith<TilemapHelper>(this.Camera);
        }

        public void Arrange(string placeholder)
        {
            // TODO load stage content -> tilemap, textures, actors, etc
            var containerSize = new Vector2(1280, 720);
            this.Tilemap.Arrange(containerSize);
        }

        #endregion
    }
}