using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Threading;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class EmpressPrism : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];

        public ref float AimDirection => ref projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Prism");
        }

        public override void SetDefaults()
        {
            projectile.width = 22;
            projectile.height = 48;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.timeLeft = 510;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (Time < -20f)
                AimDirection = projectile.AngleTo(Main.player[(int)projectile.ai[1]].Center);

            if (Time == 0f)
            {
                SoundEngine.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/WyrmElectricCharge"), projectile.Center);
                SoundEngine.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/EmpressOfLightMagicCast"), projectile.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int laserbeam = Utilities.NewProjectileBetter(projectile.Center, Vector2.Zero, ModContent.ProjectileType<PrismLaserbeam>(), EmpressOfLightNPC.LaserbeamDamage, 0f);
                    if (Main.projectile.IndexInRange(laserbeam))
                        Main.projectile[laserbeam].ai[0] = projectile.identity;
                }
            }
            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 drawPosition = projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY);
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

                    Color telegraphColor = Main.hslToRgb(((i + 3f) / 6f + Main.GlobalTime * 0.2f) % 1f, 1f, 0.5f) * (float)Math.Sqrt(telegraphInterpolant) * 0.5f;
                    telegraphColor.A = 0;

                    Vector2 aimDirection = (AimDirection + maxOffsetAngle * i / 3f).ToRotationVector2();
                    Vector2 start = projectile.Center;
                    Vector2 end = start + aimDirection * 3600f;
                    spriteBatch.DrawLineBetter(start, end, telegraphColor, telegraphWidth);
                }
            }

            float fadeInInterpolant = Utils.GetLerpValue(900f, 855f, projectile.timeLeft, true);
            float fadeOffset = MathHelper.Lerp(45f, 6f, fadeInInterpolant);
            for (int i = 0; i < 8; i++)
            {
                Color color = Main.hslToRgb((i / 8f + Main.GlobalTime * 0.5f) % 1f, 1f, 0.5f) * (float)Math.Sqrt(fadeInInterpolant);
                if (EmpressOfLightNPC.ShouldBeEnraged)
                    color = Main.OurFavoriteColor;
                color *= Utils.GetLerpValue(0f, 30f, projectile.timeLeft, true);
                color.A = 0;

                Vector2 drawOffset = (MathHelper.TwoPi * i / 8f + fadeInInterpolant * MathHelper.TwoPi + Main.GlobalTime * 1.5f).ToRotationVector2() * fadeOffset;
                spriteBatch.Draw(texture, drawPosition + drawOffset, null, color, 0f, origin, projectile.scale, 0, 0f);
            }

            return false;
        }
    }
}
