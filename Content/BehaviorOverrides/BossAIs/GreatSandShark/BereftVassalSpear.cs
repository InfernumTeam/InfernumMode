using CalamityMod;
using CalamityMod.Sounds;
using InfernumMode.Assets.Sounds;
using InfernumMode.Content.BehaviorOverrides.BossAIs.StormWeaver;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.GreatSandShark
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
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
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
            if (Projectile.timeLeft is 2 or 15 or 27 && Projectile.localAI[0] == 1f)
            {
                if (Projectile.timeLeft >= 26f)
                    Main.LocalPlayer.Calamity().GeneralScreenShakePower = 6f;

                SoundEngine.PlaySound(CommonCalamitySounds.LargeWeaponFireSound with { Volume = 0.3f }, Projectile.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 lightningSpawnPosition = Projectile.Center + new Vector2(Main.rand.NextFloatDirection() * 15f, -1000f);
                    Utilities.NewProjectileBetter(lightningSpawnPosition, Vector2.UnitY * 8f, ModContent.ProjectileType<VassalLightning>(), 210, 0f, -1, MathHelper.PiOver2, Main.rand.Next(100));
                }
            }

            // Explode if touching a dust devil.
            int dustDevilID = ModContent.ProjectileType<DustDevil>();
            foreach (Projectile dustDevil in Utilities.AllProjectilesByID(dustDevilID))
            {
                if (Projectile.Hitbox.Intersects(dustDevil.Hitbox))
                {
                    SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, Projectile.Center);

                    // Release sand and electricity dust.
                    for (int i = 0; i < 20; i++)
                    {
                        Dust electricity = Dust.NewDustPerfect(Projectile.Center, 226);
                        electricity.velocity = Main.rand.NextVector2Circular(7.2f, 7.2f);
                        electricity.scale = Main.rand.NextFloat(0.9f, 1.5f);
                        electricity.noGravity = true;

                        Dust sand = Dust.NewDustPerfect(dustDevil.Center, 32);
                        sand.velocity = Main.rand.NextVector2Circular(14f, 14f) - Vector2.UnitY * 6f;
                        sand.scale = Main.rand.NextFloat(0.9f, 1.5f);
                        sand.noGravity = true;
                    }

                    // Release a burst of sparks.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            Vector2 sparkVelocity = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 8f;
                            Utilities.NewProjectileBetter(Projectile.Center, sparkVelocity, ModContent.ProjectileType<WeaverSpark>(), 190, 0f);
                            Utilities.NewProjectileBetter(Projectile.Center, sparkVelocity * 0.01f, ModContent.ProjectileType<SparkTelegraphLine>(), 0, 0f, -1, 0f, 35f);
                        }
                    }

                    Projectile.Kill();
                    dustDevil.Kill();
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

            if (Projectile.localAI[0] == 0f)
            {
                if (oldVelocity.Y < 5f)
                    Projectile.rotation = new Vector2(oldVelocity.X, 5f).ToRotation();
                SoundEngine.PlaySound(InfernumSoundRegistry.MyrindaelHitSound, Projectile.Center);
                Projectile.localAI[0] = 1f;
            }

            return false;
        }

        public static void DrawSpearInstance(Vector2 drawPosition, Color lightColor, float opacity, float rotation, float scale, bool outline)
        {
            Texture2D spearTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/GreatSandShark/BereftVassalSpear").Value;
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
