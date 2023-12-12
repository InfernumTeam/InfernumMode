using CalamityMod;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.Particles;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
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
    public class ProfanedCirclingRock : ProfanedRock
    {
        public new float Timer => Lifetime - Projectile.timeLeft;

        public float WaitTime;

        public const float ReelbackTime = 20;

        public int Lifetime => (int)(WaitTime + ReelbackTime + 240);

        public float RotationOffset => Projectile.ai[0];

        public override void SetDefaults()
        {
            // These get changed later, but are this by default.
            base.SetDefaults();
            Projectile.Opacity = 0;
            Projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            if (!Owner.active || Owner.type != ModContent.NPCType<ProfanedGuardianDefender>())
            {
                // Client's may not recognize their owner immediately, give it a bit of time.
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    Projectile.timeLeft -= 40;
                    if (Projectile.timeLeft < 50)
                    {
                        Projectile.Kill();
                    }
                    return;
                }

                Projectile.Kill();
                return;
            }

            if (Projectile.localAI[1] == 0f)
            {
                Projectile.localAI[1] = 1f;
                Projectile.timeLeft = Lifetime;
            }
            Player target = Main.player[Owner.target];

            Projectile.Opacity = Clamp(Projectile.Opacity + 0.05f, 0f, 1f);

            if (Timer < WaitTime)
                Projectile.Center = Projectile.Center.MoveTowards(Owner.Center - ((Timer / 15f) + RotationOffset).ToRotationVector2() * 100f, 30f);
            else if (Timer == WaitTime)
            {
                Projectile.velocity = Projectile.Center.DirectionTo(target.Center) * -3.2f;
                SoundEngine.PlaySound(InfernumSoundRegistry.VassalJumpSound with { Pitch = 0.9f, Volume = 0.9f }, target.Center);
            }
            else if (Timer == WaitTime + ReelbackTime)
            {
                SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Pitch = 0.95f, Volume = 0.9f }, target.Center);
                Projectile.velocity = Projectile.Center.DirectionTo(target.Center) * 17f;
                for (int i = 0; i < 20; i++)
                {
                    Vector2 velocity = -Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(Main.rand.NextFloat(-0.15f, 0.15f)) * Main.rand.NextFloat(4f, 6f);
                    Particle rockParticle = new SandyDustParticle(Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 2f, Projectile.height / 2f), velocity,
                        Color.SandyBrown, Main.rand.NextFloat(1.25f, 1.55f), 90);
                    GeneralParticleHandler.SpawnParticle(rockParticle);

                    Particle fire = new HeavySmokeParticle(Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 2f, Projectile.height / 2f), Vector2.Zero,
                        Main.rand.NextBool() ? WayfinderSymbol.Colors[1] : WayfinderSymbol.Colors[2], 30, Main.rand.NextFloat(0.2f, 0.4f), 1f, glowing: true,
                        rotationSpeed: Main.rand.NextFromList(-1, 1) * 0.01f);
                    GeneralParticleHandler.SpawnParticle(fire);
                }
                if (CalamityConfig.Instance.Screenshake)
                    target.Infernum_Camera().CurrentScreenShakePower = 2f;
            }
            if (Timer > WaitTime + ReelbackTime)
            {
                Particle rockParticle = new SandyDustParticle(Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 3f, Projectile.height / 3f), Vector2.Zero,
                    Color.SandyBrown, Main.rand.NextFloat(0.45f, 0.75f), 30);
                GeneralParticleHandler.SpawnParticle(rockParticle);
                Projectile.rotation -= 0.1f;
                if (Main.rand.NextBool() && Main.netMode != NetmodeID.Server)
                    ModContent.GetInstance<ProfanedLavaMetaball>().SpawnParticles(ModContent.Request<Texture2D>(Texture).Value.CreateMetaballsFromTexture(Projectile.Center - Projectile.velocity * 0.5f, 0f, Projectile.scale * 0.8f, 15f, 170, 0.9f));
            }
        }

        public override bool CanHitPlayer(Player target) => Timer >= WaitTime;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;

            if (Timer >= WaitTime && Timer < WaitTime + ReelbackTime)
            {
                Texture2D invis = InfernumTextureRegistry.Invisible.Value;
                float opacity = Sin((Timer - WaitTime) / ReelbackTime * PI);
                Effect laserScopeEffect = Filters.Scene["CalamityMod:PixelatedSightLine"].GetShader().Shader;
                laserScopeEffect.Parameters["sampleTexture2"].SetValue(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/CertifiedCrustyNoise").Value);
                laserScopeEffect.Parameters["noiseOffset"].SetValue(Main.GameUpdateCount * -0.003f);
                laserScopeEffect.Parameters["mainOpacity"].SetValue(Pow(opacity, 0.5f));
                laserScopeEffect.Parameters["Resolution"].SetValue(new Vector2(340f));
                Player target = Main.player[Owner.target];
                laserScopeEffect.Parameters["laserAngle"].SetValue((target.Center - Projectile.Center).ToRotation() * -1f);
                laserScopeEffect.Parameters["laserWidth"].SetValue(0.0025f + Pow(opacity, 5f) * (Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.001f + 0.002f));
                laserScopeEffect.Parameters["laserLightStrenght"].SetValue(3f);
                laserScopeEffect.Parameters["color"].SetValue(Color.Lerp(WayfinderSymbol.Colors[1], Color.OrangeRed, 0.5f).ToVector3());
                laserScopeEffect.Parameters["darkerColor"].SetValue(WayfinderSymbol.Colors[2].ToVector3());
                laserScopeEffect.Parameters["bloomSize"].SetValue(0.06f + (1f - opacity) * 0.1f);
                laserScopeEffect.Parameters["bloomMaxOpacity"].SetValue(0.4f);
                laserScopeEffect.Parameters["bloomFadeStrenght"].SetValue(3f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, laserScopeEffect, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(invis, drawPosition, null, Color.White, 0f, invis.Size() * 0.5f, 1500f, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }

            Color backglowColor = Color.Lerp(WayfinderSymbol.Colors[0], WayfinderSymbol.Colors[1], 0.5f);
            backglowColor.A = 0;
            float backglowAmount = 12;
            for (int i = 0; i < backglowAmount; i++)
            {
                Vector2 backglowOffset = (TwoPi * i / backglowAmount).ToRotationVector2() * 4f;
                Main.EntitySpriteDraw(texture, drawPosition + backglowOffset, null, backglowColor * Projectile.Opacity, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
            }
            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(lightColor) * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            if (Timer >= WaitTime - 30)
            {
                float opacityScalar = (1f + Sin((Timer - WaitTime / 30) / (WaitTime + ReelbackTime) - (Timer - WaitTime / 30) * 2 * PI)) / 2f;
                backglowColor = Color.Lerp(backglowColor, Color.OrangeRed, opacityScalar);
                for (int i = 0; i < 3; i++)
                    Main.EntitySpriteDraw(texture, drawPosition, null, backglowColor * Projectile.Opacity * opacityScalar, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }
            return false;
        }
    }
}
