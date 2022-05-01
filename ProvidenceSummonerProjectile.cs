using CalamityMod;
using CalamityMod.NPCs.Providence;
using CalamityMod.Particles;
using InfernumMode.BehaviorOverrides.BossAIs.Providence;
using InfernumMode.BehaviorOverrides.BossAIs.Yharon;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace InfernumMode
{
    public class ProvidenceSummonerProjectile : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public const int Lifetime = 375;

        public override string Texture => "CalamityMod/Items/SummonItems/ProfanedCoreUnlimited";

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
                            dustSpawnOffset.X += (float)Math.Sin((Projectile.position.Y + verticalOffset) * 0.06f + MathHelper.TwoPi * j / 3f) * 0.5f;
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
            if (Time == 75f)
                SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.Instance, "Sounds/Custom/LeviathanSummonBase"), Projectile.Center);

            if (Time >= 210f)
            {
                float jitterFactor = Utils.Remap(Projectile.velocity.Length(), 2f, 0f, 0.4f, 3f);

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

            // Make the crystal shatter.
            SoundEngine.PlaySound(SoundLoader.CustomSoundType, -1, -1, SoundLoader.GetSoundSlot(InfernumMode.CalamityMod, "Sounds/NPCKilled/ProvidenceDeath"));

            for (int i = 1; i <= 4; i++)
                Gore.NewGore(new InfernumSource(), Projectile.Center, Main.rand.NextVector2Circular(8f, 8f), Utilities.GetGoreID($"ProfanedCoreGore{i}", InfernumMode.Instance), Projectile.scale);

            // Emit fire.
            for (int i = 0; i < 32; i++)
            {
                Dust fire = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(24f, 25f), 6, Vector2.Zero, 0, Color.White * 0.1f, 1.1f);
                fire.velocity.Y = Main.rand.NextFloat(2f);
                fire.fadeIn = 0.6f;
                fire.scale = 1.5f;
                fire.noGravity = true;
            }

            // Create an explosion and summon Providence.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                CalamityUtils.SpawnBossBetter(Projectile.Center - Vector2.UnitY * 325f, ModContent.NPCType<Providence>());
                Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ProvSummonFlameExplosion>(), 0, 0f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

            for (int i = 0; i < 8; i++)
            {
                Color color = Color.Lerp(new(1f, 0.62f, 0f, 0f), Color.White, (float)Math.Pow(Projectile.Opacity, 1.63)) * Projectile.Opacity;
                Vector2 drawOffset = (Time * MathHelper.TwoPi / 67f + MathHelper.TwoPi * i / 8f).ToRotationVector2() * (1f - Projectile.Opacity) * 75f;
                Vector2 drawPosition = Projectile.Center - Main.screenPosition + drawOffset;
                Main.spriteBatch.Draw(texture, drawPosition, null, color, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0f);
            }

            return false;
        }
    }
}
