using InfernumMode.Assets.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;

namespace InfernumMode.Common.Graphics.Metaballs
{
    public abstract class BaseMetaballCollection
    {
        public abstract Color DrawColor { get; }

        public virtual Color EdgeColor => Color.Black;

        public virtual float ShrinkRate => 0.95f;

        public virtual bool UseLighting => true;

        public ManagedRenderTarget MainTarget
        {
            get;
            private set;
        }

        public ManagedRenderTarget LightingTarget
        {
            get;
            private set;
        }

        public List<Metaball> Metaballs
        {
            get;
            set;
        } = new();

        public void Load()
        {
            MainTarget = new(true, RenderTargetManager.CreateScreenSizedTarget);
            LightingTarget = new(true, RenderTargetManager.CreateScreenSizedTarget);
        }

        public void UpdateMetaballs()
        {
            foreach (var metaball in Metaballs)
            {
                metaball.Velocity.Y += metaball.GravityStrength;
                bool tileCollision = metaball.CollidingWithTiles();

                if (metaball.IgnoreTiles || !tileCollision)
                    metaball.Center += metaball.Velocity;
                else if (tileCollision)
                    metaball.Velocity = Vector2.Zero;

                metaball.Velocity *= 0.99f;

                ExtraUpdate(metaball);

                float shrinkRate = ShrinkRate;
                if (shrinkRate >= 1)
                    shrinkRate = 0.95f;

                metaball.Size *= shrinkRate;
            }
        }

        public virtual void ExtraUpdate(Metaball metaball)
        {

        }

        public void DrawToTarget(SpriteBatch spriteBatch)
        {
            if (!Metaballs.Any())
                return;

            MainTarget.SwapToRenderTarget();

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.AnisotropicWrap, DepthStencilState.None, Main.Rasterizer, null);
            Rectangle screenArea = new((int)(Main.screenPosition.X - 150), (int)(Main.screenPosition.Y - 150), Main.screenWidth + 300, Main.screenHeight + 300);
            List<Metaball> balls = Metaballs.Where(m => screenArea.Contains(m.Center.ToPoint())).ToList();

            DrawToTargetExtra(spriteBatch, balls);

            foreach (var ball in balls)
                ball.DrawNormal(spriteBatch, DrawColor);

            spriteBatch.End();

            LightingTarget.SwapToRenderTarget();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            foreach (var ball in balls)
                ball.DrawForLighting(spriteBatch, UseLighting ? Color.White : null);

            spriteBatch.End();

            Main.instance.GraphicsDevice.SetRenderTarget(null);
        }

        /// <summary>
        /// Draw things in here to have the metaball shader applied to them. Does nothing by default. Ensure the spritebatch leaves in additve blending.
        /// </summary>
        /// <param name="spriteBatch"></param>
        public virtual void DrawToTargetExtra(SpriteBatch spriteBatch, List<Metaball> onScreenBalls)
        {

        }

        public void DrawTarget(SpriteBatch spriteBatch)
        {

            if (!Metaballs.Any())
                return;

            Effect metaballShader = PrepareEdgeShader();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, metaballShader, Main.GameViewMatrix.TransformationMatrix);
            spriteBatch.Draw(MainTarget.Target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            spriteBatch.End();
        }

        public virtual Effect PrepareEdgeShader()
        {
            Effect edgeShader = InfernumEffectsRegistry.BaseMetaballEdgeShader.GetShader().Shader;
            edgeShader.Parameters["threshold"].SetValue(0.6f);
            edgeShader.Parameters["rtSize"].SetValue(new Vector2(MainTarget.Width, MainTarget.Height));
            edgeShader.Parameters["mainColor"].SetValue(DrawColor.ToVector3());
            edgeShader.Parameters["edgeColor"].SetValue(EdgeColor.ToVector3());
            Utilities.SetTexture1(LightingTarget.Target);
            return edgeShader;
        }

        public void SpawnMetaball(Metaball metaball)
        {
            if (Main.netMode is NetmodeID.Server)
                return;

            Metaballs.Add(metaball); 
        }
    }
}
