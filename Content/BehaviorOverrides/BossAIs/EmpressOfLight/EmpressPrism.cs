using InfernumMode.Assets.Effects;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class EmpressPrism : ModProjectile
    {
        public PrimitiveTrailCopy LightRayDrawer
        {
            get;
            set;
        }

        public ref float Time => ref Projectile.ai[0];

        public static float MaxLaserbeamCoverage => 0.21f;

        public static int Lifetime => 120;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Prism");
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 48;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            if (Time == 0f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        float laserOffsetAngle = PiOver2 + Lerp(-MaxLaserbeamCoverage, MaxLaserbeamCoverage, i / 4f);
                        Utilities.NewProjectileBetter(Projectile.Center, laserOffsetAngle.ToRotationVector2(), ModContent.ProjectileType<PrismLaserbeam>(), EmpressOfLightBehaviorOverride.SmallLaserbeamDamage, 0f, -1, Projectile.identity, i / 4f);
                    }
                }
            }

            // Release light sparkles.
            if (Main.rand.NextBool(3) && Time >= -20f)
            {
                Vector2 sparkleSpawnPosition = Projectile.Center + new Vector2(Main.rand.NextFloatDirection() * Projectile.width * 0.5f, -Main.rand.NextFloat(500f));
                Dust light = Dust.NewDustPerfect(sparkleSpawnPosition, 264);
                light.color = Main.hslToRgb(Main.rand.NextFloat(), 1f, Main.rand.NextFloat(0.8f, 1f));
                light.noGravity = true;
                light.velocity = -Vector2.UnitY;
                light.noLight = true;
            }

            Time++;
        }

        public float LightRayWidthFunction(float _) => Projectile.width * 0.95f;

        public Color LightRayColorFunction(float completionRatio)
        {
            float endFadeOpacity = Utils.GetLerpValue(0f, 0.2f, completionRatio, true) * Utils.GetLerpValue(1f, 0.8f, completionRatio, true);
            float glowOpacity = Utils.GetLerpValue(Lifetime, Lifetime - 20f, Projectile.timeLeft, true) * Utils.GetLerpValue(6f, 30f, Projectile.timeLeft, true);
            return Color.LightCyan * endFadeOpacity * glowOpacity * 0.2f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            Vector2 origin = texture.Size() * 0.5f;

            // Draw telegraphs.
            float telegraphInterpolant = Utils.GetLerpValue(-40f, -8f, Time, true);
            if (telegraphInterpolant > 0f && Time < 0f)
            {
                float maxOffsetAngle = Lerp(0.0012f, MaxLaserbeamCoverage, telegraphInterpolant);
                float telegraphWidth = Lerp(2f, 9f, telegraphInterpolant);
                for (int i = 0; i < 5; i++)
                {
                    Color telegraphColor = Main.hslToRgb(i / 4f, 1f, 0.5f) * Sqrt(telegraphInterpolant) * 0.5f;
                    telegraphColor.A = 0;

                    Vector2 aimDirection = (PiOver2 + Lerp(-maxOffsetAngle, maxOffsetAngle, i / 5f)).ToRotationVector2();
                    Vector2 start = Projectile.Center;
                    Vector2 end = start + aimDirection * 2500f;
                    Main.spriteBatch.DrawLineBetter(start, end, telegraphColor, telegraphWidth);
                    Main.spriteBatch.DrawLineBetter(start, end, Color.Lerp(telegraphColor, Color.White with { A = 0 }, 0.4f), telegraphWidth * 0.5f);
                    Main.spriteBatch.DrawLineBetter(start, end, Color.Lerp(telegraphColor, Color.White with { A = 0 }, 0.8f), telegraphWidth * 0.25f);
                }
            }

            // Draw the light ray line. This is done to indicate that the laser rays are from the light being "split".
            Vector2 telegraphStart = Projectile.Center;
            Vector2 telegraphEnd = Projectile.Center - Vector2.UnitY * 1000f;
            Vector2[] telegraphPoints =
            [
                telegraphStart,
                Vector2.Lerp(telegraphStart, telegraphEnd, 0.25f),
                Vector2.Lerp(telegraphStart, telegraphEnd, 0.5f),
                Vector2.Lerp(telegraphStart, telegraphEnd, 0.75f),
                telegraphEnd
            ];
            LightRayDrawer ??= new(LightRayWidthFunction, LightRayColorFunction, null, true, InfernumEffectsRegistry.SideStreakVertexShader);
            LightRayDrawer.Draw(telegraphPoints, -Main.screenPosition, 40);

            // Draw the prism itself. It will converge as a bunch of transparent "copies" before becoming the complete prism.
            float fadeInInterpolant = Utils.GetLerpValue(Lifetime, Lifetime - 20f, Projectile.timeLeft, true);
            float fadeOffset = Lerp(45f, 6f, fadeInInterpolant);
            for (int i = 0; i < 8; i++)
            {
                float hue = (i / 8f + Main.GlobalTimeWrappedHourly * 0.5f) % 1f;
                Color color = Main.hslToRgb(hue, 1f, 0.5f);
                if (EmpressOfLightBehaviorOverride.ShouldBeEnraged)
                    color = EmpressOfLightBehaviorOverride.GetDaytimeColor(hue);

                color *= Utils.GetLerpValue(0f, 30f, Lifetime, true) * Sqrt(fadeInInterpolant);
                color.A = 0;

                Vector2 drawOffset = (TwoPi * i / 8f + fadeInInterpolant * TwoPi + Main.GlobalTimeWrappedHourly * 1.5f).ToRotationVector2() * fadeOffset;
                Main.spriteBatch.Draw(texture, drawPosition + drawOffset, null, color, 0f, origin, Projectile.scale, 0, 0f);
            }

            return false;
        }
    }
}
