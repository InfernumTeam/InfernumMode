using CalamityMod;
using InfernumMode.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class FlameOverloadBeam : ModProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy RayDrawer = null;

        public NPC Owner => Main.npc[(int)Projectile.ai[0]];

        public ref float LaserLength => ref Projectile.ai[1];

        public ref float Time => ref Projectile.localAI[1];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public const float MaxLaserLength = 3950f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brimstone Flame Overload Ray");
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.netImportant = true;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        public override void AI()
        {
            // Die if the owner is no longer present.
            if (!Owner.active)
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.localAI[0] == 0f)
            {
                Projectile.scale = 0.05f;
                Projectile.localAI[0] = 1f;
            }

            // Grow bigger up to a point.
            float maxScale = MathHelper.Lerp(2f, 0.051f, Owner.Infernum().ExtraAI[1]);
            Projectile.scale = MathHelper.Clamp(Projectile.scale + 0.04f, 0.05f, maxScale);

            // Die after sufficiently shrunk.
            if (Owner.Infernum().ExtraAI[1] >= 1f)
                Projectile.Kill();

            // Update the laser length.
            LaserLength = MaxLaserLength;

            // Spin.
            float spinInterpolant = Utils.GetLerpValue(16f, 150f, Time, true);
            float angularVelocity = MathHelper.Lerp(0.006f, 0.0174f, (float)Math.Pow(spinInterpolant, 1.75));
            Projectile.velocity = Projectile.velocity.RotatedBy(angularVelocity);

            // Make the beam cast light along its length. The brightness of the light is reliant on the scale of the beam.
            DelegateMethods.v3_1 = Color.Orange.ToVector3() * Projectile.scale * 0.6f;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, Projectile.width * Projectile.scale, DelegateMethods.CastLight);
            Time++;
        }

        internal float PrimitiveWidthFunction(float completionRatio) => Projectile.scale * 60f;

        internal Color PrimitiveColorFunction(float completionRatio)
        {
            float opacity = Projectile.Opacity * Utils.GetLerpValue(0.97f, 0.9f, completionRatio, true) * 
                Utils.GetLerpValue(0f, MathHelper.Clamp(15f / LaserLength, 0f, 0.5f), completionRatio, true) *
                (float)Math.Pow(Utils.GetLerpValue(60f, 270f, LaserLength, true), 3D);
            float flameInterpolant = (float)Math.Sin(completionRatio * 3f + Main.GlobalTimeWrappedHourly * 0.5f + Projectile.identity * 0.3156f) * 0.5f + 0.5f;
            Color c = Color.Lerp(Color.White, Color.Orange, MathHelper.Lerp(0.5f, 0.8f, flameInterpolant)) * opacity;
            c.A = 0;

            return c;
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            RayDrawer ??= new(PrimitiveWidthFunction, PrimitiveColorFunction, specialShader: InfernumEffectsRegistry.PrismaticRayVertexShader);

            Vector2 overallOffset = -Main.screenPosition;
            Vector2[] basePoints = new Vector2[24];
            for (int i = 0; i < basePoints.Length; i++)
                basePoints[i] = Projectile.Center + Projectile.velocity * i / (basePoints.Length - 1f) * LaserLength;

            Projectile.scale *= 0.8f;
            InfernumEffectsRegistry.PrismaticRayVertexShader.UseImage1("Images/Misc/Perlin");
            Main.instance.GraphicsDevice.Textures[2] = InfernumTextureRegistry.StreakFire.Value;
            Projectile.scale /= 0.8f;

            RayDrawer.DrawPixelated(basePoints, overallOffset, 42);

            Projectile.scale *= 1.5f;
            InfernumEffectsRegistry.PrismaticRayVertexShader.SetShaderTexture(InfernumTextureRegistry.CultistRayMap);
            Main.instance.GraphicsDevice.Textures[2] = InfernumTextureRegistry.StreakFire.Value;
            RayDrawer.DrawPixelated(basePoints, overallOffset, 42);
            Projectile.scale /= 1.5f;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + Projectile.velocity * (LaserLength - 50f), Projectile.scale * 60f, ref _);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overWiresUI.Add(index);
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
