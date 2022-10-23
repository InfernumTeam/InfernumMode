using CalamityMod;
using CalamityMod.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark
{
    public class BereftVassalSpear : ModProjectile
    {
        public const float Gravity = 0.56f;

        public const int Lifetime = 148;

        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Myrndael");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = 54;
            Projectile.height = 54;
            Projectile.hostile = true;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.penetrate = -1;
            Projectile.scale = 0.8f;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Color.Blue.ToVector3() * Projectile.Opacity * 0.64f);

            // Handle fade effects.
            Projectile.Opacity = Utils.GetLerpValue(0f, 60f, Projectile.timeLeft, true);

            // Fall.
            if (Projectile.localAI[0] == 0f)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            if (Time >= 70f)
                Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + Gravity, -34f, 14f);

            // Release lightning before dying.
            if ((Projectile.timeLeft is 2 or 15 or 27) && Projectile.localAI[0] == 1f)
            {
                if (Projectile.timeLeft >= 26f)
                    Main.LocalPlayer.Calamity().GeneralScreenShakePower = 6f;

                SoundEngine.PlaySound(CommonCalamitySounds.LargeWeaponFireSound with { Volume = 0.3f }, Projectile.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 lightningSpawnPosition = Projectile.Center + new Vector2(Main.rand.NextFloatDirection() * 15f, -1000f);
                    int lightning = Utilities.NewProjectileBetter(lightningSpawnPosition, Vector2.UnitY * 8f, ModContent.ProjectileType<VassalLightning>(), 210, 0f);
                    if (Main.projectile.IndexInRange(lightning))
                    {
                        Main.projectile[lightning].ai[0] = Main.projectile[lightning].velocity.ToRotation();
                        Main.projectile[lightning].ai[1] = Main.rand.Next(100);
                    }
                }
            }

            Time++;
        }

        public override bool? CanDamage() => Projectile.Opacity > 0.75f ? null : false;

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        // Ensure that the spear does not die when it touches tiles.
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity *= 0.7f;
            Projectile.localAI[0] = 1f;
            return false;
        }

        public static void DrawSpearInstance(Vector2 drawPosition, Color lightColor, float opacity, float rotation, float scale, bool outline)
        {
            Texture2D spearTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/GreatSandShark/BereftVassalSpear").Value;
            if (opacity < 0.95f)
            {
                Color spearAfterimageColor = new Color(0.23f, 0.93f, 0.96f, 0f) * opacity;
                for (int i = 0; i < 12; i++)
                {
                    Vector2 spearOffset = (MathHelper.TwoPi * i / 12f + Main.GlobalTimeWrappedHourly * 2.2f).ToRotationVector2() * (1f - opacity) * 12f;
                    Main.EntitySpriteDraw(spearTexture, drawPosition + spearOffset, null, spearAfterimageColor, rotation, spearTexture.Size() * 0.5f, scale, 0, 0);
                }
            }

            if (outline)
            {
                Color spearAfterimageColor = new Color(1f, 1f, 1f, 0f) * opacity;
                for (int i = 0; i < 8; i++)
                {
                    Vector2 spearOffset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 4f;
                    Main.EntitySpriteDraw(spearTexture, drawPosition + spearOffset, null, spearAfterimageColor, rotation, spearTexture.Size() * 0.5f, scale, 0, 0);
                }
            }
            Main.EntitySpriteDraw(spearTexture, drawPosition, null, lightColor * opacity, rotation, spearTexture.Size() * 0.5f, scale, 0, 0);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrawSpearInstance(Projectile.Center - Main.screenPosition, Projectile.GetAlpha(lightColor), Projectile.Opacity, Projectile.rotation, Projectile.scale, true);
            return false;
        }
    }
}
