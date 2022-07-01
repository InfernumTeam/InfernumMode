using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SupremeCalamitasBrotherPortal : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public const int Lifetime = 360;

        public const int PortalCreationDelay = 105;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Dark Portal");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 34;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            Projectile.scale = 0f;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.scale = Utils.GetLerpValue(0f, 24f, Time, true);
            Projectile.Opacity = Projectile.scale;

            // Create a lot of light particles around the portal.
            float particleSpawnChance = Utilities.Remap(Time, 0f, 60f, 0.1f, 0.9f);
            for (int i = 0; i < 3; i++)
            {
                if (Main.rand.NextFloat() > particleSpawnChance)
                    continue;

                float scale = Main.rand.NextFloat(0.5f, 0.66f);
                Color particleColor = Color.Lerp(Color.Red, Color.Yellow, Main.rand.NextFloat(0.1f, 0.9f));
                Vector2 particleSpawnOffset = Main.rand.NextVector2Unit() * Main.rand.NextFloat(0.15f, 1f) * Projectile.scale * 512f;
                Vector2 particleVelocity = particleSpawnOffset * -0.05f;
                SquishyLightParticle light = new(Projectile.Center + particleSpawnOffset, particleVelocity, scale, particleColor, 40, 1f, 7f);
                GeneralParticleHandler.SpawnParticle(light);
            }

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.EnterShaderRegion();

            float fade = Utils.GetLerpValue(0f, 45f, Time, true) * Utils.GetLerpValue(0f, 45f, Projectile.timeLeft, true);
            Texture2D noiseTexture = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/VoronoiShapes").Value;
            Vector2 drawPosition2 = Projectile.Center - Main.screenPosition;
            Vector2 origin = noiseTexture.Size() * 0.5f;
            GameShaders.Misc["CalamityMod:DoGPortal"].UseOpacity(fade);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseColor(Color.Violet);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseSecondaryColor(Color.Red);
            GameShaders.Misc["CalamityMod:DoGPortal"].Apply();

            Main.spriteBatch.Draw(noiseTexture, drawPosition2, null, Color.White, 0f, origin, Projectile.scale * 4f, SpriteEffects.None, 0f);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
