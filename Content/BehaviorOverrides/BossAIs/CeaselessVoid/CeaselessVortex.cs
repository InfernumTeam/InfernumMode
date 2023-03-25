using CalamityMod.NPCs;
using CalamityMod.Particles;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class CeaselessVortex : ModProjectile, ISpecializedDrawRegion
    {
        public static NPC CeaselessVoid => Main.npc[CalamityGlobalNPC.voidBoss];

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
            Projectile.MaxUpdates = 3;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
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

            if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.timeLeft == 36f)
            {
                Vector2 tearSpawnPosition = Projectile.Center;
                Vector2 tearVelocity = Projectile.velocity * 24f;
                Utilities.NewProjectileBetter(tearSpawnPosition + 2f * tearVelocity, tearVelocity, ModContent.ProjectileType<CeaselessVortexTear>(), 250, 0f);

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
                Vector2 lightSpawnPosition = Projectile.Center + Projectile.velocity.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloatDirection() * 45f;
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
                Vector2 end = start + 3000f * Projectile.velocity;
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
