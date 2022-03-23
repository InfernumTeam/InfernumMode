using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Cultist
{
    public class LightBeam : ModProjectile
    {
        internal PrimitiveTrailCopy BeamDrawer;
        public ref float Time => ref projectile.ai[0];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults() => DisplayName.SetDefault("Beam");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 2;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.hide = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 60;
            projectile.alpha = 255;
        }

        public override void AI()
        {
            if (!NPC.AnyNPCs(NPCID.CultistBoss))
            {
                projectile.Kill();
                return;
            }

            // Spin around slowly.
            projectile.velocity = projectile.velocity.RotatedBy(MathHelper.Pi / 90f * (projectile.identity % 2f == 0f).ToDirectionInt());

            // Fade effects.
            float cyclicFade = (float)Math.Sin(MathHelper.Pi * Time / 60f);
            projectile.Opacity = cyclicFade * 1.8f;
            if (projectile.Opacity > 1f)
                projectile.Opacity = 1f;

            projectile.scale = cyclicFade * 1.5f;
            if (projectile.scale > 1f)
                projectile.scale = 1f;

            // Create some light.
            Lighting.AddLight(projectile.Center, Color.White.ToVector3() * 1.4f);

            Time++;
        }

        public float WidthFunction(float completionRatio) => MathHelper.Lerp(1f, 8f, completionRatio) * MathHelper.Clamp(projectile.scale, 0.04f, 1f);

        public Color ColorFunction(float completionRatio) => Color.White * projectile.Opacity * Utils.InverseLerp(0.95f, 0.725f, completionRatio, true);

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (BeamDrawer is null)
                BeamDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true);

            float length = 40f;
            length += MathHelper.Lerp(0f, 16f, projectile.identity % 7f / 7f);
            List<Vector2> points = new List<Vector2>();
            for (int i = 0; i <= 12; i++)
                points.Add(Vector2.Lerp(projectile.Center, projectile.Center + projectile.velocity * length, i / 12f));

            BeamDrawer.Draw(points, projectile.Size * 0.5f - Main.screenPosition, 47);
            return false;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI)
        {
            drawCacheProjsBehindNPCsAndTiles.Add(index);
        }

        public override bool CanDamage() => Time >= 10f;
    }
}
