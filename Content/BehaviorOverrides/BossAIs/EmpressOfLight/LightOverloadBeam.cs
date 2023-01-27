using CalamityMod;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class LightOverloadBeam : ModProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy RayDrawer = null;

        public NPC Owner => Main.npc[(int)Projectile.ai[0]];

        public ref float Time => ref Projectile.ai[1];

        public static int TelegraphTime => 36;

        public const int FadeInTime = 12;

        public const int FadeOutTime = 12;

        public static int Lifetime => TelegraphTime + FadeInTime + FadeOutTime + 45;

        public const float MaxLaserLength = 5250f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Prismatic Overload Ray");
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 48;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.netImportant = true;
            Projectile.timeLeft = Lifetime;
            Projectile.Opacity = 0f;
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
            Projectile.scale = Utils.GetLerpValue(TelegraphTime, TelegraphTime + FadeInTime, Time, true) * Utils.GetLerpValue(0f, -FadeOutTime, Time - Lifetime, true);
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.1f, 0f, 1f);

            // Make the beam cast light along its length. The brightness of the light is reliant on the scale of the beam.
            DelegateMethods.v3_1 = Color.White.ToVector3() * Projectile.scale * 0.6f;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * MaxLaserLength, Projectile.width * Projectile.scale, DelegateMethods.CastLight);

            Time++;
        }

        internal float PrimitiveWidthFunction(float completionRatio)
        {
            return Projectile.scale * Projectile.width;
        }

        internal Color PrimitiveColorFunction(float completionRatio)
        {
            float opacity = Projectile.Opacity * Utils.GetLerpValue(1f, 0.95f, completionRatio, true);
            Color c = Main.hslToRgb((completionRatio * 12f + Main.GlobalTimeWrappedHourly * 0.3f + Projectile.identity * 0.3156f) % 1f, 1f, 0.7f) * opacity;
            return c;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Time >= TelegraphTime)
                return false;

            Color telegraphColor = Main.hslToRgb((Main.GlobalTimeWrappedHourly * 0.1f + Projectile.identity * 0.3156f) % 1f, 1f, 0.7f);
            Vector2 end = Projectile.Center + Projectile.velocity * MaxLaserLength;
            Main.spriteBatch.DrawLineBetter(Projectile.Center, end, telegraphColor, Projectile.Opacity * 4f);

            return false;
        }

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            if (Time < TelegraphTime)
                return;

            RayDrawer ??= new(PrimitiveWidthFunction, PrimitiveColorFunction, specialShader: InfernumEffectsRegistry.ArtemisLaserVertexShader);

            Vector2 overallOffset = -Main.screenPosition;
            Vector2[] basePoints = new Vector2[6];
            for (int i = 0; i < basePoints.Length; i++)
                basePoints[i] = Projectile.Center + Projectile.velocity * i / (basePoints.Length - 1f) * MaxLaserLength;

            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseSaturation(0.3f);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseColor(Color.HotPink * Projectile.Opacity);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakThickGlow);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseImage2("Images/Misc/Perlin");
            RayDrawer.DrawPixelated(basePoints, overallOffset, 32);
        }

        public override bool? CanDamage() => Projectile.Opacity >= 0.9f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + Projectile.velocity * (MaxLaserLength - 50f), Projectile.width, ref _);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overWiresUI.Add(index);
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
