using System.Collections.Generic;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Cutscenes
{
    public class CutsceneManager : ModSystem
    {
        internal static Queue<Cutscene> CutscenesQueue = new();

        /// <summary>
        /// The cutscene that is currently active.
        /// </summary>
        internal static Cutscene ActiveCutscene
        {
            get;
            private set;
        }

        /// <summary>
        /// Queues a cutscene to be played.
        /// </summary>
        /// <param name="cutscene"></param>
        public static void QueueCutscene(Cutscene cutscene)
        {
            if (Main.netMode != NetmodeID.Server)
                CutscenesQueue.Enqueue(cutscene);
        }

        /// <summary>
        /// Returns whether the provided cutscene is active, via checking its name.
        /// </summary>
        /// <param name="cutscene"></param>
        /// <returns></returns>
        public static bool IsActive(Cutscene cutscene)
        {
            if (ActiveCutscene == null)
                return false;

            return ActiveCutscene.Name == cutscene.Name;
        }

        public override void Load() => Main.OnPostDraw += PostDraw;

        public override void Unload() => Main.OnPostDraw -= PostDraw;

        public override void PostUpdateEverything()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            if (ActiveCutscene == null)
            {
                if (CutscenesQueue.TryDequeue(out Cutscene cutscene))
                {
                    ActiveCutscene = cutscene;
                    ActiveCutscene.Timer = 0;
                    ActiveCutscene.IsActive = true;
                    ActiveCutscene.OnBegin();

                    if (ActiveCutscene.GetBlockCondition.HasValue)
                        BlockerSystem.Start(ActiveCutscene.GetBlockCondition.Value);
                }
            }
            
            if (ActiveCutscene != null)
            {
                ActiveCutscene.Update();
                ActiveCutscene.Timer++;

                if (ActiveCutscene.EndAbruptly || ActiveCutscene.Timer > ActiveCutscene.CutsceneLength)
                {
                    ActiveCutscene.OnEnd();
                    ActiveCutscene.Timer = 0;
                    ActiveCutscene.IsActive = false;
                    ActiveCutscene.EndAbruptly = false;
                    ActiveCutscene = null;
                }
            }
        }

        public override void ModifyScreenPosition() => ActiveCutscene?.ModifyScreenPosition();

        public override void ModifyTransformMatrix(ref SpriteViewMatrix Transform) => ActiveCutscene?.ModifyTransformMatrix(ref Transform);

        internal static void DrawToWorld() => ActiveCutscene?.DrawToWorld(Main.spriteBatch);

        internal static RenderTarget2D DrawWorld(RenderTarget2D screen)
        {
            if (ActiveCutscene == null)
                return screen;

            return ActiveCutscene?.DrawWorld(Main.spriteBatch, screen);
        }

        private void PostDraw(GameTime obj)
        {
            ActiveCutscene?.PostDraw(Main.spriteBatch);
        }
    }
}
