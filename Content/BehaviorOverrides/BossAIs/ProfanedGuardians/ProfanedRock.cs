using CalamityMod;
using CalamityMod.Particles;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Metaballs;
using InfernumMode.Content.Projectiles.Wayfinder;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class ProfanedRock : ModProjectile
    {
        public enum RockType
        {
            Aimed,
            Accelerating,
            Gravity
        }

        public static string[] Textures => new string[4]
        {
            "ProfanedRock",
            "ProfanedRock2",
            "ProfanedRock3",
            "ProfanedRock4",
        };

        public string CurrentVarient = Textures[0];

        public int RedHotGlowTimer = 30;

        public int RockTypeVarient = (int)RockType.Aimed;

        public bool DoNotDrawLine;

        public ref float Timer => ref Projectile.ai[0];

        public NPC Owner => Main.npc[(int)Projectile.ai[1]];

        public override string Texture => "InfernumMode/Content/BehaviorOverrides/BossAIs/ProfanedGuardians/Rocks/" + CurrentVarient;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
        }

        public override void SetDefaults()
        {
            // These get changed later, but are this by default.
            Projectile.width = 42;
            Projectile.height = 36;

            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.Opacity = 1;
            Projectile.timeLeft = 240;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            if (!Owner.active)
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.localAI[0] == 0)
            {
                Projectile.localAI[0] = 1;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int varient = Main.rand.Next(4);
                    switch (varient)
                    {
                        case 0:
                            CurrentVarient = Textures[varient];
                            break;
                        case 1:
                            CurrentVarient = Textures[varient];
                            Projectile.width = 34;
                            Projectile.height = 38;
                            break;
                        case 2:
                            CurrentVarient = Textures[varient];
                            Projectile.width = 36;
                            Projectile.height = 46;
                            break;
                        case 3:
                            CurrentVarient = Textures[varient];
                            Projectile.width = 28;
                            Projectile.height = 36;
                            break;
                    }
                    Projectile.netUpdate = true;
                }

                if ((RockType)RockTypeVarient == RockType.Gravity)
                {
                    RedHotGlowTimer = 120;
                    Projectile.timeLeft = 360;
                    DoNotDrawLine = true;
                }
            }

            Player target = Main.player[Owner.target];

            switch ((RockType)RockTypeVarient)
            {
                case RockType.Aimed:
                    if (Timer == 0)
                    {
                        SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Pitch = 0.95f, Volume = 0.9f }, target.Center);
                        for (int i = 0; i < 20; i++)
                        {
                            Vector2 velocity = -Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(Main.rand.NextFloat(-0.15f, 0.15f)) * Main.rand.NextFloat(4f, 6f);
                            Particle rock = new SandyDustParticle(Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 2f, Projectile.height / 2f), velocity, Color.SandyBrown,
                                Main.rand.NextFloat(1.25f, 1.55f), 90);
                            GeneralParticleHandler.SpawnParticle(rock);

                            Particle fire = new HeavySmokeParticle(Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 2f, Projectile.height / 2f), Vector2.Zero,
                                Main.rand.NextBool() ? WayfinderSymbol.Colors[1] : WayfinderSymbol.Colors[2], 30, Main.rand.NextFloat(0.2f, 0.4f), 1f, glowing: true,
                                rotationSpeed: Main.rand.NextFromList(-1, 1) * 0.01f);
                            GeneralParticleHandler.SpawnParticle(fire);
                        }
                        if (CalamityConfig.Instance.Screenshake)
                            target.Infernum_Camera().CurrentScreenShakePower = 2f;
                    }
                    break;

                case RockType.Accelerating:
                    if (Projectile.velocity.Length() < 30f)
                        Projectile.velocity *= 1.035f;
                    break;

                case RockType.Gravity:
                    if (Projectile.velocity.Y < 16f)
                    {
                        Projectile.velocity.X *= 0.995f;
                        Projectile.velocity.Y += 0.35f;
                    }
                    break;
            }

            Particle rockParticle = new SandyDustParticle(Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 3f, Projectile.height / 3f), Vector2.Zero, Color.SandyBrown,
                Main.rand.NextFloat(0.45f, 0.75f), 30);
            GeneralParticleHandler.SpawnParticle(rockParticle);

            if (Main.rand.NextBool() && Main.netMode != NetmodeID.Server)
            {
                ModContent.GetInstance<ProfanedLavaMetaball>().SpawnParticles(ModContent.Request<Texture2D>(Texture).Value.CreateMetaballsFromTexture(Projectile.Center + Projectile.velocity * 0.5f, 0f, Projectile.scale * 0.8f, 12f, 190, 0.9f));
            }

            Projectile.rotation -= 0.1f;
            Timer++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;

            Color backglowColor = Color.Lerp(WayfinderSymbol.Colors[0], WayfinderSymbol.Colors[1], 0.5f);
            backglowColor.A = 0;

            if (Timer <= RedHotGlowTimer && !DoNotDrawLine)
            {
                Texture2D invis = InfernumTextureRegistry.Invisible.Value;
                float opacity = Sin(Timer / RedHotGlowTimer * PI);
                Effect laserScopeEffect = Filters.Scene["CalamityMod:PixelatedSightLine"].GetShader().Shader;
                laserScopeEffect.Parameters["sampleTexture2"].SetValue(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/CertifiedCrustyNoise").Value);
                laserScopeEffect.Parameters["noiseOffset"].SetValue(Main.GameUpdateCount * -0.003f);
                laserScopeEffect.Parameters["mainOpacity"].SetValue(Pow(opacity, 0.5f));
                laserScopeEffect.Parameters["Resolution"].SetValue(new Vector2(340f));
                laserScopeEffect.Parameters["laserAngle"].SetValue(Projectile.velocity.ToRotation() * -1f);
                laserScopeEffect.Parameters["laserWidth"].SetValue(0.005f + Pow(opacity, 5f) * (Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.002f + 0.002f));
                laserScopeEffect.Parameters["laserLightStrenght"].SetValue(5f);
                laserScopeEffect.Parameters["color"].SetValue(Color.Lerp(WayfinderSymbol.Colors[1], Color.OrangeRed, 0.5f).ToVector3());
                laserScopeEffect.Parameters["darkerColor"].SetValue(WayfinderSymbol.Colors[2].ToVector3());
                laserScopeEffect.Parameters["bloomSize"].SetValue(0.06f + (1f - opacity) * 0.1f);
                laserScopeEffect.Parameters["bloomMaxOpacity"].SetValue(0.4f);
                laserScopeEffect.Parameters["bloomFadeStrenght"].SetValue(3f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, laserScopeEffect, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(invis, drawPosition, null, Color.White, 0f, invis.Size() * 0.5f, 750f, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }

            float backglowAmount = 12;
            for (int i = 0; i < backglowAmount; i++)
            {
                Vector2 backglowOffset = (TwoPi * i / backglowAmount).ToRotationVector2() * 4f;
                Main.EntitySpriteDraw(texture, drawPosition + backglowOffset, null, backglowColor * Projectile.Opacity, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
            }
            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(lightColor) * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            if (Timer <= RedHotGlowTimer)
            {
                float interpolant = Timer / RedHotGlowTimer;
                backglowColor = Color.OrangeRed * (1 - interpolant);
                for (int i = 0; i < 3; i++)
                    Main.EntitySpriteDraw(texture, drawPosition, null, backglowColor * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }
            return false;
        }
    }
}
