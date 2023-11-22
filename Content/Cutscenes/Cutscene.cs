using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Graphics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Cutscenes
{
    public abstract class Cutscene : ModType
    {
        public int Timer
        {
            get;
            set;
        }

        public bool IsActive
        {
            get;
            internal set;
        }

        public bool EndAbruptly
        {
            get;
            internal set;
        }

        public float LifetimeRatio => (float)Timer / CutsceneLength;

        public abstract int CutsceneLength { get; }

        public virtual BlockerSystem.BlockCondition? GetBlockCondition => null;

        protected sealed override void Register() => ModTypeLookup<Cutscene>.Register(this);

        public sealed override void SetupContent() => SetStaticDefaults();

        public virtual void OnBegin()
        {

        }

        public virtual void OnEnd()
        {

        }

        public virtual void Update()
        {

        }

        public virtual void ModifyScreenPosition()
        {

        }

        public virtual void ModifyTransformMatrix(ref SpriteViewMatrix Transform)
        {

        }

        /// <summary>
        /// Happens after NPC drawing.
        /// </summary>
        /// <param name="spriteBatch"></param>
        public virtual void DrawToWorld(SpriteBatch spriteBatch)
        {

        }

        /// <summary>
        /// Happens in a EndCapture detour. Draw to screen last and return it.
        /// </summary>
        /// <param name="spriteBatch"></param>
        /// <param name="screen"></param>
        /// <returns></returns>
        public virtual RenderTarget2D DrawWorld(SpriteBatch spriteBatch, RenderTarget2D screen) => screen;

        /// <summary>
        /// Happens on PostDraw
        /// </summary>
        /// <param name="spriteBatch"></param>
        public virtual void PostDraw(SpriteBatch spriteBatch)
        {

        }
    }
}
