using Hex.Models;
using Microsoft.Xna.Framework;
using Mogi.Helpers;
using Mogi.Inversion;

namespace Hex.Helpers
{
    public class StageHelper : IRegister
    {
        #region Constructors

        public StageHelper(InputHelper input)
        {
            this.Camera = new CameraHelper(() => this.CameraBounds, () => this.ContainerSize, input);
        }

        #endregion

        #region Data Members

        protected CameraHelper Camera { get; }
        protected TilemapHelper Tilemap { get; set; }
        protected ActorHelper Actor { get; set; }

        public Vector2 ContainerSize { get; protected set; }
        public Vector2 CameraBounds { get; protected set; }

        public Hexagon CursorHexagon { get; protected set; }
        public Hexagon SourceHexagon { get; protected set; }

        public Matrix TranslationMatrix => this.Camera.TranslationMatrix;

        public int TileCount => this.Tilemap.HexagonMap.Count;

        #endregion

        #region Methods

        public void Register(DependencyHandler dependency)
        {
            dependency.Register(this.Camera);
            this.Tilemap = dependency.Register<TilemapHelper>();
            this.Actor = dependency.Register<ActorHelper>();
        }

        public void Arrange(Rectangle container, string placeholder)
        {
            // TODO load stage content -> tilemap, textures, actors, etc
            this.Tilemap.Arrange(container.Size.ToVector2());
            
            this.ContainerSize = this.Tilemap.ContainerSize;
            this.CameraBounds = this.Tilemap.CameraBounds;
            
            // can remove this when appropriate tilemap methods moved to this class
            this.Camera.Center();
        }

        #endregion
    }
}