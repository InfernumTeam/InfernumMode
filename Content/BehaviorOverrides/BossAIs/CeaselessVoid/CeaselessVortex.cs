using CalamityMod.NPCs;
using CalamityMod.Particles;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class CeaselessVortex : ModProjectile, ISpecializedDrawRegion
    {
        public static NPC CeaselessVoid => Main.npc[CalamityGlobalNPC.voidBoss];

        public bool AimDirectlyAtTarget
        {
            get => Projectile.localAI[0] == 1f;
            set => Projectile.localAI[0] = value.ToInt();
        }

        public ref float Time => ref Projectile.ai[0];

        public ref float TelegraphInterpolant => ref Projectile.ai[1];

        public override string Texture => InfernumTextureRegistry.InvisPath;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ceaseless Vortex");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 18;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 300;
            Projectile.MaxUpdates = 2;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(AimDirectlyAtTarget);
            writer.Write(Projectile.MaxUpdates);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            AimDirectlyAtTarget = reader.ReadBoolean();
            Projectile.MaxUpdates = reader.ReadInt32();
        }

        public override void AI()
        {
            // Disappear if the Ceaseless Void is not present.
            if (CalamityGlobalNPC.voidBoss == -1)
            {
                Projectile.Kill();
                return;
            }

            float fadeOutFactor = Utils.GetLerpValue(60f, 0f, Projectile.timeLeft, true);
            Projectile.Opacity = Utils.GetLerpValue(0f, 30f, Time, true) * (1f - fadeOutFactor);
            Projectile.scale = Utils.GetLerpValue(0f, 45f, Time, true) * (4f * fadeOutFactor + 1f);

            // Cast the telegraph.
            TelegraphInterpolant = Utils.GetLerpValue(0f, 45f, Time, true) * Utils.GetLerpValue(28f, 36f, Projectile.timeLeft, true);

            if (Projectile.timeLeft == 36f)
            {
                float tearShootSpeed = 24f;
                Vector2 tearSpawnPosition = Projectile.Center;

                // Ensure that the tear fires a preset distance away from the player if this vortex is directed to aim directly at the target.
                if (AimDirectlyAtTarget)
                {
                    float distanceToTarget = tearSpawnPosition.Distance(Main.player[CeaselessVoid.target].Center);
                    float tearTravelDistance = distanceToTarget + 840f;

                    // Calculate the base speed assuming that there is no acceleration.
                    tearShootSpeed = tearTravelDistance / CeaselessVortexTear.Lifetime;

                    // Factor acceleration into the speed calculation.
                    tearShootSpeed /= MathF.Pow(CeaselessVortexTear.Acceleration, CeaselessVortexTear.Lifetime) / 4;

                    SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidStrikeSound, Projectile.Center);
                }

                for (int i = 0; i < 40; i++)
                {
                    int gasLifetime = Main.rand.Next(20, 24);
                    float scale = 1.9f;
                    Vector2 gasSpawnPosition = Projectile.Center + Projectile.velocity.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloatDirection() * 72f;
                    Vector2 gasVelocity = Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(10f, 90f);
                    Color gasColor = Color.Lerp(Color.HotPink, Color.Blue, Main.rand.NextFloat(0.6f));
                    Particle gas = new HeavySmokeParticle(gasSpawnPosition, gasVelocity, gasColor, gasLifetime, scale, 1f, 0f, true);
                    if (Main.rand.NextBool(3))
                        gas = new MediumMistParticle(gasSpawnPosition, gasVelocity, gasColor, Color.Black, 0.67f * scale, 255f);

                    GeneralParticleHandler.SpawnParticle(gas);
                }

                Vector2 tearVelocity = Projectile.velocity * tearShootSpeed;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(tearSpawnPosition + 2f * tearVelocity, tearVelocity, ModContent.ProjectileType<CeaselessVortexTear>(), CeaselessVoidBehaviorOverride.VortexTearDamage, 0f, -1, AimDirectlyAtTarget.ToInt());

                // Tell the Ceaseless Void to play the sound.
                CeaselessVoid.Infernum().ExtraAI[0] = 1f;
                CeaselessVoid.netUpdate = true;
            }

            // Emit a bunch of light from the portal.
            if (Main.rand.NextBool(8))
            {
                int lightLifetime = Main.rand.Next(20, 24);
                float squishFactor = 2f;
                float scale = 0.56f;
                Vector2 lightSpawnPosition = Projectile.Center + Projectile.velocity.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloatDirection() * 72f;
                Vector2 lightVelocity = Projectile.velocity * Main.rand.NextFloat(10f, 20f);
                Color lightColor = Color.Lerp(Color.MediumPurple, Color.DarkBlue, Main.rand.NextFloat(0f, 0.5f));
                if (Main.rand.NextBool())
                    lightColor = Color.Lerp(Color.Purple, Color.Black, 0.6f);

                SquishyLightParticle light = new(lightSpawnPosition, lightVelocity, scale, lightColor, lightLifetime, 1f, squishFactor, squishFactor * 10f);
                GeneralParticleHandler.SpawnParticle(light);
            }

            Time++;
        }

        public override bool? CanDamage() => false;

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool ShouldUpdatePosition() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            // Draw the laser telegraph.
            if (TelegraphInterpolant > 0f)
            {
                Main.spriteBatch.SetBlendState(BlendState.Additive);

                float telegraphColorInterpolant = MathF.Cos(MathHelper.TwoPi * Projectile.identity / 11f + Main.GlobalTimeWrappedHourly * 13f) * 0.5f + 0.5f;
                float telegraphBaseWidth = MathHelper.Lerp(20f, 36f, telegraphColorInterpolant);
                Vector2 start = Projectile.Center;

                // Calculate the telegraph length.
                float telegraphDistance = AimDirectlyAtTarget ? start.Distance(Main.player[CeaselessVoid.target].Center) : 3000f;
                Vector2 end = start + telegraphDistance * Projectile.velocity;
                Color baseTelegraphColor = Color.Lerp(Color.Purple, Color.DarkBlue, 0.415f);
                Main.spriteBatch.DrawBloomLine(start, end, baseTelegraphColor * (1f + telegraphColorInterpolant * 0.6f), telegraphBaseWidth * TelegraphInterpolant);
            }
            return false;
        }

        public void SpecialDraw(SpriteBatch spriteBatch)
        {
            var portalShader = GameShaders.Misc["CalamityMod:DoGPortal"];
            float portalColorInterpolant = MathF.Cos(MathHelper.TwoPi * Projectile.identity / 11f + Main.GlobalTimeWrappedHourly * 5f) * 0.5f + 0.5f;
            Texture2D noiseTexture = InfernumTextureRegistry.WavyNeuronsNoise.Value;
            Vector2 origin = noiseTexture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            portalShader.UseOpacity(Projectile.Opacity);
            portalShader.UseColor(Color.Purple);
            portalShader.UseSecondaryColor(Color.Lerp(Color.HotPink, Color.DarkBlue, portalColorInterpolant));
            portalShader.Apply();
            spriteBatch.Draw(noiseTexture, drawPosition, null, Color.White, Projectile.velocity.ToRotation(), origin, new Vector2(0.5f, 1f) * Projectile.scale, 0, 0f);
        }

        public void PrepareSpriteBatch(SpriteBatch spriteBatch)
        {
            spriteBatch.EnterShaderRegion();
        }
    }
}
