using CalamityMod;
using CalamityMod.NPCs.AstrumDeus;
using CalamityMod.Particles;
using CalamityMod.Sounds;
using InfernumMode.Particles;
using InfernumMode.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumDeus
{
    public class AstralVortex : ModProjectile
    {
        public int FlameSpawnRate;

        public bool Cyan => Projectile.localAI[0] == 1f;

        public ref float Timer => ref Projectile.ai[0];

        public ref float OtherVortexIndex => ref Projectile.ai[1];

        public Player Target => Main.player[Projectile.owner];

        public const int ScaleFadeinTime = 95;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Vortex");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 160;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 1080;
            Projectile.scale = 1f;
        }

        public override void AI()
        {
            // Die if Astrum Deus is not present or is dead.
            if (!NPC.AnyNPCs(ModContent.NPCType<AstrumDeusHead>()))
            {
                Projectile.Kill();
                return;
            }

            Projectile otherVortex = Main.projectile[(int)OtherVortexIndex];
            Projectile.scale = Utilities.UltrasmoothStep(Timer / ScaleFadeinTime) * 2f + Utilities.UltrasmoothStep(Timer / ScaleFadeinTime * 3.2f) * 0.34f;
            Projectile.scale = MathHelper.Lerp(Projectile.scale, 0f, Utils.GetLerpValue(91690f, 91720f, Timer, true));
            Projectile.Opacity = MathHelper.Clamp(Projectile.scale * 0.87f, 0f, 1f);

            // Move towards the nearest player and try to stay near the other 
            if (Projectile.velocity.Length() > 0.001f)
            {
                float flyTogetherInterpolant = Utils.GetLerpValue(ScaleFadeinTime + 180f, ScaleFadeinTime + 225f, Timer, true);
                if (!Projectile.WithinRange(otherVortex.Center, MathHelper.Clamp(1100f - Timer * 2f, 100f, 1100f)))
                    Projectile.velocity += Projectile.SafeDirectionTo(otherVortex.Center) * 1.2f;

                if (Projectile.velocity.Length() < 14f)
                {
                    Vector2 vortexOffset = otherVortex.Center - Projectile.Center;
                    if (Math.Abs(vortexOffset.X) < 0.01f)
                        vortexOffset.X = 0.01f;
                    if (Math.Abs(vortexOffset.Y) < 0.01f)
                        vortexOffset.Y = 0.01f;

                    float minPushSpeed = MathHelper.Lerp(0.02f, 0.08f, flyTogetherInterpolant);
                    Vector2 force = (Vector2.One * (flyTogetherInterpolant * 3f + 1f) * 0.4f / vortexOffset + Projectile.SafeDirectionTo(otherVortex.Center) * minPushSpeed * 0.25f).ClampMagnitude(minPushSpeed, 20f);
                    Projectile.velocity += force + Projectile.SafeDirectionTo(Target.Center) * 0.24f;
                }
                else
                    Projectile.velocity *= 0.9f;

                // Idly create debris crystals.
                if (Timer % FlameSpawnRate == FlameSpawnRate - 1)
                {
                    Vector2 crystalSpawnPosition = Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(50f, 200f) * Projectile.scale;
                    if (!Main.player[Player.FindClosest(crystalSpawnPosition, 1, 1)].WithinRange(crystalSpawnPosition, 300f))
                    {
                        SoundEngine.PlaySound(SoundID.Item92, Projectile.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 crystalVelocity = (crystalSpawnPosition - Projectile.Center).SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2) * 9f;
                            Utilities.NewProjectileBetter(crystalSpawnPosition, crystalVelocity, ModContent.ProjectileType<AstralFlame2>(), 200, 0f);
                        }
                    }
                }

                // Explode if very close and merging.
                if (Projectile.WithinRange(otherVortex.Center, 125f) && flyTogetherInterpolant >= 0.75f)
                {
                    SoundEngine.PlaySound(CommonCalamitySounds.FlareSound, Projectile.Center);
                    SoundEngine.PlaySound(InfernumSoundRegistry.WyrmChargeSound, Projectile.Center);

                    // Create a bunch of sparkles, along with a circular spread of astral flames.
                    Vector2 impactPoint = (Projectile.Center + otherVortex.Center) * 0.5f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 75; i++)
                        {
                            Vector2 sparkleVelocity = Main.rand.NextVector2Circular(67f, 67f);
                            Utilities.NewProjectileBetter(impactPoint, sparkleVelocity, ModContent.ProjectileType<AstralSparkle>(), 0, 0f);
                        }

                        for (int i = 0; i < 9; i++)
                        {
                            Vector2 flameVelocity = (MathHelper.TwoPi * i / 9f).ToRotationVector2() * 10f;
                            Utilities.NewProjectileBetter(impactPoint, flameVelocity, ModContent.ProjectileType<AstralFlame2>(), 200, 0f);
                        }
                    }
                    Color[] explosionColors = new Color[]
                    {
                        new(250, 90, 74, 127),
                        new(76, 255, 194, 127)
                    };
                    GeneralParticleHandler.SpawnParticle(new ElectricExplosionRing(impactPoint, Vector2.Zero, explosionColors, 3f, 180, 1.4f));

                    Projectile.Kill();
                    otherVortex.Kill();
                }
            }

            Timer++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(Projectile.Center, Projectile.scale * 80f, targetHitbox);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D noiseTexture = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/VoronoiShapes").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = noiseTexture.Size() * 0.5f;
            Main.spriteBatch.EnterShaderRegion();

            Color fadedColor = Cyan ? Color.LightCyan : Color.Orange;
            Color primaryColor = Cyan ? new(109, 242, 196) : new(237, 93, 83);

            Vector2 diskScale = Projectile.scale * new Vector2(1f, 0.85f);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseOpacity(Projectile.Opacity);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseColor(fadedColor);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseSecondaryColor(fadedColor);
            GameShaders.Misc["CalamityMod:DoGPortal"].Apply();

            Main.spriteBatch.Draw(noiseTexture, drawPosition, null, Color.White, 0f, origin, diskScale, SpriteEffects.None, 0f);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseOpacity(Projectile.Opacity);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseColor(primaryColor);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseSecondaryColor(primaryColor);
            GameShaders.Misc["CalamityMod:DoGPortal"].Apply();

            for (int i = 0; i < 3; i++)
                Main.spriteBatch.Draw(noiseTexture, drawPosition, null, Color.White, 0f, origin, diskScale, SpriteEffects.None, 0f);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
