using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.Drawers.SceneDrawers
{
    /// <summary>
    /// Used to create a scene texture in the form of a render target.
    /// </summary>
    public abstract class BaseSceneDrawSystem : ModType
    {
        #region Instance Members
        public ManagedRenderTarget MainTarget
        {
            get;
            private set;
        }

        public List<BaseSceneObject> Objects
        {
            get;
            private set;
        }

        protected sealed override void Register()
        {
            ModTypeLookup<BaseSceneDrawSystem>.Register(this);

            if (!DrawerManager.SceneDrawers.Contains(this))
                DrawerManager.SceneDrawers.Add(this);

            Objects = new();

            if (Main.netMode == NetmodeID.Server)
                return;

            Main.QueueMainThreadAction(() =>
            {
                MainTarget = new(true, TargetCreationCondition, true);
            });
        }

        public sealed override void SetupContent() => SetStaticDefaults();

        public void Update()
        {
            ExtraUpdate();
            foreach (var obj in Objects)
                obj.Update();

            Objects.RemoveAll(obj => obj.ShouldKill);
        }
        #endregion

        #region Virtuals
        public virtual ManagedRenderTarget.RenderTargetCreationCondition TargetCreationCondition => RenderTargetManager.CreateScreenSizedTarget;

        /// <summary>
        /// Whether the scene should update and draw this frame.
        /// </summary>
        public virtual bool ShouldDrawThisFrame => true;

        /// <summary>
        /// Manage adding new objects etc in here.
        /// </summary>
        public virtual void ExtraUpdate()
        {

        }

        /// <summary>
        /// Draw the objects here. By default, calls <see cref="DrawObjectListToMainTarget(SpriteBatch, List{BaseSceneObject})"/> with <see cref="Objects"/> passed through.
        /// </summary>
        /// <param name="spriteBatch"></param>
        public virtual void DrawObjectsToMainTarget(SpriteBatch spriteBatch)
        {
            DrawObjectListToMainTarget(spriteBatch, Objects);
        }

        /// <summary>
        /// Draws the provided list of objects, ordering them by <see cref="BaseSceneObject.Depth"/>.
        /// </summary>
        /// <param name="spriteBatch"></param>
        /// <param name="objectsToDraw"></param>
        public static void DrawObjectListToMainTarget(SpriteBatch spriteBatch, List<BaseSceneObject> objectsToDraw)
        {
            Vector2 screenCenter = Main.screenPosition + new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);
            Rectangle rectangle = new(-1000, -1000, Main.screenWidth + 1000, Main.screenHeight + 1000);

            foreach (var obj in objectsToDraw.OrderBy(x => x.Depth))
            {
                Vector2 scale = new Vector2(1f / obj.Depth, 1f / obj.Depth) * obj.Scale;
                Vector2 position = (obj.Position - screenCenter) * scale + screenCenter - Main.screenPosition;
                if (rectangle.Contains(position.ToPoint()))
                    obj.Draw(spriteBatch, position, screenCenter, scale);
            }
        }

        /// <summary>
        /// Prepare <see cref="MainTarget"/> here. Called just before <see cref="DrawObjectsToMainTarget(SpriteBatch)"/>
        /// </summary>
        public abstract void DrawToMainTarget(SpriteBatch spriteBatch);
        #endregion
    }
}
