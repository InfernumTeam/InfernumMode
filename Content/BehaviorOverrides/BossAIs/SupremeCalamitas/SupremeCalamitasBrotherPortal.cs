using CalamityMod;
using CalamityMod.Particles;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SupremeCalamitasBrotherPortal : ModProjectile
    {
        public int NPCIDToSpawn => (int)Projectile.ai[0];

        public ref float Time => ref Projectile.ai[1];

        public const int Lifetime = 150;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Dark Portal");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 34;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.scale = 0f;
            
        }

        public override void AI()
        {
            Projectile.scale = Utils.GetLerpValue(0f, Lifetime * 0.4f, Time, true) * Utils.GetLerpValue(1f, Lifetime * 0.9f, Time, true);
            Projectile.Opacity = Projectile.scale;

            // Create a lot of light particles around the portal.
            float particleSpawnChance = Utils.Remap(Time, 0f, 60f, 0.1f, 0.9f);
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

            // Summon the brother and create a massive explosion before having the portal close.
            if (Time == (int)(Lifetime * 0.8f))
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.WyrmChargeSound with { Volume = 1.2f }, Projectile.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int explosion = Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<DemonicExplosion>(), 0, 0f);
                    if (Main.projectile.IndexInRange(explosion))
                        Main.projectile[explosion].ModProjectile<DemonicExplosion>().MaxRadius = 800f;

                    NPC.NewNPC(Projectile.GetSource_FromAI(), (int)Projectile.Center.X, (int)Projectile.Center.Y, NPCIDToSpawn);
                }
            }

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.EnterShaderRegion();

            float fade = Utils.GetLerpValue(0f, 45f, Time, true) * Utils.GetLerpValue(0f, 45f, Projectile.timeLeft, true);
            Texture2D noiseTexture = InfernumTextureRegistry.VoronoiShapes.Value;
            Vector2 drawPosition2 = Projectile.Center - Main.screenPosition;
            Vector2 origin = noiseTexture.Size() * 0.5f;
            GameShaders.Misc["CalamityMod:DoGPortal"].UseOpacity(fade);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseColor(Color.Violet);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseSecondaryColor(Color.Red);
            GameShaders.Misc["CalamityMod:DoGPortal"].Apply();

            Main.spriteBatch.Draw(noiseTexture, drawPosition2, null, Color.White, 0f, origin, new Vector2(Projectile.scale * 0.6f, 1f) * 4f, SpriteEffects.None, 0f);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
