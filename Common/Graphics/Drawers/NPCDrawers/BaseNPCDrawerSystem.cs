using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.Drawers.NPCDrawers
{
    // This only works for one instance of the NPC. Not the most ideal, but it will suffice for these use cases.
    public abstract class BaseNPCDrawerSystem: ModType
    {
        public ManagedRenderTarget MainTarget
        {
            get;
            private set;
        }

        public NPC AssosiatedNPC
        {
            get
            {
                if (!NPC.AnyNPCs(AssosiatedNPCType))
                    return null;

                return Main.npc[NPC.FindFirstNPC(AssosiatedNPCType)];
            }
        }

        protected sealed override void Register()
        {
            ModTypeLookup<BaseNPCDrawerSystem>.Register(this);

            if(!DrawerManager.NPCDrawers.Contains(this))
                DrawerManager.NPCDrawers.Add(this);

            if (Main.netMode == NetmodeID.Server)
                return;

            Main.QueueMainThreadAction(() =>
            {
                MainTarget = new(true, TargetCreationCondition, true);
            });
        }

        public sealed override void SetupContent() => SetStaticDefaults();

        #region Virtuals
        public virtual ManagedRenderTarget.RenderTargetCreationCondition TargetCreationCondition => RenderTargetManager.CreateScreenSizedTarget;

        public virtual bool ShouldDrawThisFrame => true;

        public abstract int AssosiatedNPCType { get; }

        /// <summary>
        /// If using <see cref="MainTarget"/>, draw to it here. The target is already set, and a spritebatch is prepared with no matrix.
        /// </summary>
        public virtual void DrawToMainTarget(SpriteBatch spriteBatch)
        {

        }

        /// <summary>
        /// Use this to draw <see cref="MainTarget"/>, or the drawcode directly if not being used. A spritebatch is prepared with the game matrix.
        /// </summary>
        public abstract void DrawMainTargetContents(SpriteBatch spriteBatch);
        #endregion
    }
}
