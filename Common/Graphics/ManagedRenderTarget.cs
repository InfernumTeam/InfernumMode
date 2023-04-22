using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;

namespace InfernumMode.Common.Graphics
{
    public class ManagedRenderTarget : IDisposable
    {
        internal RenderTargetCreationCondition CreationCondition
        {
            get;
            private set;
        }

        public bool IsDisposed
        {
            get;
            private set;
        }

        public bool ShouldResetUponScreenResize
        {
            get;
            private set;
        }

        public RenderTarget2D Target
        {
            get;
            private set;
        }

        public int Width => Target.Width;

        public int Height => Target.Height;

        public delegate RenderTarget2D RenderTargetCreationCondition(int screenWidth, int screenHeight);

        public ManagedRenderTarget(bool shouldResetUponScreenResize, RenderTargetCreationCondition creationCondition)
        {
            ShouldResetUponScreenResize = shouldResetUponScreenResize;
            CreationCondition = creationCondition;

            // Initialize the render target if possible.
            if (Main.netMode != NetmodeID.Server)
                Main.QueueMainThreadAction(() => Target = CreationCondition(Main.screenWidth, Main.screenHeight));

            RenderTargetManager.ManagedTargets.Add(this);
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            Target?.Dispose();
            GC.SuppressFinalize(this);
        }

        public void Recreate(int screenWidth, int screenHeight)
        {
            Dispose();
            IsDisposed = false;

            Target = CreationCondition(screenWidth, screenHeight);
        }
    }
}