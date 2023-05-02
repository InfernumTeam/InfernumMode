using CalamityMod;
using CalamityMod.NPCs.Providence;
using CalamityMod.Particles;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Providence;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace InfernumMode.Content.Projectiles.Generic
{
    public class ProvidenceSummonerProjectile : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public const int Lifetime = 375;

        public override string Texture => "CalamityMod/Items/SummonItems/ProfanedCore";

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.aiStyle = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = Lifetime;
            Projectile.Opacity = 0f;
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            // Fade in.
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.015f, 0f, 1f);

            // Rise upward and create a spiral of fire around the core.
            if (Time is >= 70f and < 210f)
            {
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, -Vector2.UnitY * 1.75f, 0.025f);
                for (int i = 0; i < Math.Abs(Projectile.velocity.Y) * 1.6f + 1; i++)
                {
                    if (Main.rand.NextBool(2))
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            float verticalOffset = Main.rand.NextFloat() * -Projectile.velocity.Y;
                            Vector2 dustSpawnOffset = Vector2.UnitX * Main.rand.NextFloatDirection() * 0.05f;
                            dustSpawnOffset.X += MathF.Sin((Projectile.position.Y + verticalOffset) * 0.06f + MathHelper.TwoPi * j / 3f) * 0.5f;
                            dustSpawnOffset.X = MathHelper.Lerp(Main.rand.NextFloat() - 0.5f, dustSpawnOffset.X, MathHelper.Clamp(-Projectile.velocity.Y, 0f, 1f));
                            dustSpawnOffset.Y = -Math.Abs(dustSpawnOffset.X) * 0.25f;
                            dustSpawnOffset *= Utils.GetLerpValue(210f, 180f, Time, true) * new Vector2(40f, 50f);
                            dustSpawnOffset.Y += verticalOffset;

                            Dust fire = Dust.NewDustPerfect(Projectile.Center + dustSpawnOffset, 6, Vector2.Zero, 0, Color.White * 0.1f, 1.1f);
                            fire.velocity.Y = Main.rand.NextFloat(2f);
                            fire.fadeIn = 0.6f;
                            fire.noGravity = true;
                        }
                    }
                }
            }

            // Play a rumble sound.
            if (Time == 115f)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.LeviathanRumbleSound, Projectile.Center);
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceSpawnSuspenseSound);
            }

            if (Time >= 210f)
            {
                float jitterFactor = MathHelper.Lerp(0.4f, 3f, Utils.GetLerpValue(0f, 2f, Projectile.velocity.Length(), true));

                Projectile.velocity *= 0.96f;
                Projectile.Center += Main.rand.NextVector2Circular(jitterFactor, jitterFactor);

                // Create screen shake effects.
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = Utils.GetLerpValue(2300f, 1300f, Main.LocalPlayer.Distance(Projectile.Center), true) * jitterFactor * 2f;

                // Create falling rock particles.
                if (Main.rand.NextBool(10))
                {
                    Vector2 rockSpawnPosition = Projectile.Center + Vector2.UnitX * Main.rand.NextFloatDirection() * 900f;
                    rockSpawnPosition = Utilities.GetGroundPositionFrom(rockSpawnPosition, Searches.Chain(new Searches.Up(9000), new Conditions.IsSolid()));
                    StoneDebrisParticle2 rock = new(rockSpawnPosition, Vector2.UnitY * 16f, Color.Brown, Main.rand.NextFloat(1f, 1.4f), 90);
                    GeneralParticleHandler.SpawnParticle(rock);
                }
            }

            Time++;
        }

        public override void Kill(int timeLeft)
        {
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Utils.GetLerpValue(2300f, 1300f, Main.LocalPlayer.Distance(Projectile.Center), true) * 16f;

            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 1; i <= 4; i++)
                    Gore.NewGore(Projectile.GetSource_Death(), Projectile.Center, Main.rand.NextVector2Circular(8f, 8f), Mod.Find<ModGore>($"ProfanedCoreGore{i}").Type, Projectile.scale);
            }

            // Emit fire.
            for (int i = 0; i < 32; i++)
            {
                Dust fire = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(24f, 25f), 6, Vector2.Zero, 0, Color.White * 0.1f, 1.1f);
                fire.velocity.Y = Main.rand.NextFloat(2f);
                fire.fadeIn = 0.6f;
                fire.scale = 1.5f;
                fire.noGravity = true;
            }

            SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceScreamSound);

            // Create an explosion and summon Providence.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                CalamityUtils.SpawnBossBetter(Projectile.Center + Vector2.UnitY * 160f, ModContent.NPCType<Providence>());
                Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ProvSummonFlameExplosion>(), 0, 0f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

            for (int i = 0; i < 8; i++)
            {
                Color color = Color.Lerp(new Color(1f, 0.62f, 0f, 0f), Color.White, (float)Math.Pow(Projectile.Opacity, 2.7f)) * MathF.Pow(Projectile.Opacity, 2f);
                Vector2 drawOffset = (Time * MathHelper.TwoPi / 67f + MathHelper.TwoPi * i / 8f).ToRotationVector2() * (1f - Projectile.Opacity) * 75f;
                Vector2 drawPosition = Projectile.Center - Main.screenPosition + drawOffset;
                Main.spriteBatch.Draw(texture, drawPosition, null, color, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0f);
            }

            // Create a glimmer right before the core explodes.
            float glimmerInterpolant = Utils.GetLerpValue(40f, 20f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 12f, Projectile.timeLeft, true) * 0.56f;

            if (glimmerInterpolant > 0f)
            {
                Main.spriteBatch.SetBlendState(BlendState.Additive);

                texture = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/LargeStar").Value;
                Vector2 glimmerDrawPosition = Projectile.Center - Main.screenPosition;

                for (float scale = 1f; scale > 0.3f; scale -= 0.1f)
                {
                    Color c = Color.Lerp(Projectile.GetAlpha(Color.Lerp(Color.Yellow, Color.Wheat, 0.45f)), Color.White, 1f - scale);
                    Main.spriteBatch.Draw(texture, glimmerDrawPosition, null, c, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * scale * glimmerInterpolant, 0, 0f);
                    Main.spriteBatch.Draw(texture, glimmerDrawPosition, null, c, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * new Vector2(4f, 0.2f) * scale * glimmerInterpolant, 0, 0f);
                }
                Main.spriteBatch.ResetBlendState();
            }

            return false;
        }
    }
}
