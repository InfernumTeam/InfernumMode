using InfernumMode.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class EmpressPrism : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public ref float AimDirection => ref Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Prism");
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
            Projectile.timeLeft = 510;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (Time < -20f)
                AimDirection = Projectile.AngleTo(Main.player[(int)Projectile.ai[1]].Center);

            if (Time == 0f)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.WyrmChargeSound, Projectile.Center);
                SoundEngine.PlaySound(SoundID.Item163, Projectile.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int laserbeam = Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<PrismLaserbeam>(), EmpressOfLightBehaviorOverride.LaserbeamDamage, 0f);
                    if (Main.projectile.IndexInRange(laserbeam))
                        Main.projectile[laserbeam].ai[0] = Projectile.identity;
                }
            }
            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            Vector2 origin = texture.Size() * 0.5f;

            // Draw telegraphs.
            float telegraphInterpolant = Utils.GetLerpValue(-72f, -30f, Time, true);
            if (telegraphInterpolant > 0f && Time < 0f)
            {
                float maxOffsetAngle = MathHelper.Lerp(0.24f, 0.0012f, telegraphInterpolant);
                float telegraphWidth = MathHelper.Lerp(3f, 10f, telegraphInterpolant);
                for (int i = -3; i <= 3; i++)
                {
                    // Don't draw the middle beam. Doing so would lead to a single line being drawn when others should converge.
                    if (i == 0)
                        continue;

                    Color telegraphColor = Main.hslToRgb(((i + 3f) / 6f + Main.GlobalTimeWrappedHourly * 0.2f) % 1f, 1f, 0.5f) * (float)Math.Sqrt(telegraphInterpolant) * 0.5f;
                    telegraphColor.A = 0;

                    Vector2 aimDirection = (AimDirection + maxOffsetAngle * i / 3f).ToRotationVector2();
                    Vector2 start = Projectile.Center;
                    Vector2 end = start + aimDirection * 3600f;
                    Main.spriteBatch.DrawLineBetter(start, end, telegraphColor, telegraphWidth);
                }
            }

            float fadeInInterpolant = Utils.GetLerpValue(900f, 855f, Projectile.timeLeft, true);
            float fadeOffset = MathHelper.Lerp(45f, 6f, fadeInInterpolant);
            for (int i = 0; i < 8; i++)
            {
                Color color = Main.hslToRgb((i / 8f + Main.GlobalTimeWrappedHourly * 0.5f) % 1f, 1f, 0.5f) * (float)Math.Sqrt(fadeInInterpolant);
                if (EmpressOfLightBehaviorOverride.ShouldBeEnraged)
                    color = Main.OurFavoriteColor;
                color *= Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true);
                color.A = 0;

                Vector2 drawOffset = (MathHelper.TwoPi * i / 8f + fadeInInterpolant * MathHelper.TwoPi + Main.GlobalTimeWrappedHourly * 1.5f).ToRotationVector2() * fadeOffset;
                Main.spriteBatch.Draw(texture, drawPosition + drawOffset, null, color, 0f, origin, Projectile.scale, 0, 0f);
            }

            return false;
        }
    }
}
