using CalamityMod;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Cultist
{
    public class LightBeam : ModProjectile, IPixelPrimitiveDrawer
    {
        internal PrimitiveTrailCopy BeamDrawer;

        public const int Lifetime = 60;

        public ref float Time => ref Projectile.ai[0];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Beam");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.alpha = 255;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            if (!NPC.AnyNPCs(NPCID.CultistBoss))
            {
                Projectile.Kill();
                return;
            }

            // Spin around slowly.
            Projectile.velocity = Projectile.velocity.RotatedBy(Pi / 90f * (Projectile.identity % 2f == 0f).ToDirectionInt());

            // Fade effects.
            float cyclicFade = CalamityUtils.Convert01To010(Time / Lifetime);
            Projectile.Opacity = cyclicFade * 1.8f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;

            Projectile.scale = cyclicFade * 1.5f;
            if (Projectile.scale > 1f)
                Projectile.scale = 1f;

            // Create some light.
            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 1.4f);

            Time++;
        }

        public float WidthFunction(float completionRatio) => Lerp(1f, 8f, completionRatio) * Clamp(Projectile.scale, 0.04f, 1f);

        public Color ColorFunction(float completionRatio) => Color.White * Projectile.Opacity * Utils.GetLerpValue(0.95f, 0.725f, completionRatio, true);

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            BeamDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true);

            float length = 40f;
            length += Lerp(0f, 16f, Projectile.identity % 7f / 7f);
            List<Vector2> points = new();
            for (int i = 0; i <= 12; i++)
                points.Add(Vector2.Lerp(Projectile.Center, Projectile.Center + Projectile.velocity * length, i / 12f));

            BeamDrawer.DrawPixelated(points, Projectile.Size * 0.5f - Main.screenPosition, 47);
        }

        public override bool ShouldUpdatePosition() => false;

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI, List<int> overWiresUI)
        {
            drawCacheProjsBehindNPCsAndTiles.Add(index);
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Time >= 10f;
    }
}
