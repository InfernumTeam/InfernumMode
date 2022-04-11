using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class VoidBlackHole : ModProjectile
    {
        public ref float Timer => ref Projectile.ai[0];
        public ref float Owner => ref Projectile.ai[1];
        public Player Target => Main.player[Projectile.owner];
        public override string Texture => "InfernumMode/ExtraTextures/WhiteHole";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Void");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 160;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 900000;
            Projectile.scale = 1f;
        }

        public override void AI()
        {
            // Die if the owner is not present or is dead.
            if (!Main.npc.IndexInRange((int)Owner) || !Main.npc[(int)Owner].active || Main.npc[(int)Owner].ai[0] == 2f)
            {
                Projectile.Kill();
                return;
            }

            NPC core = Main.npc[(int)Owner];
            Timer = core.ai[1];
            Projectile.Center = core.Center;
            Projectile.scale = Utilities.UltrasmoothStep(Timer / 120f) * 2.5f + Utilities.UltrasmoothStep(Timer / 32f) * 0.34f;
            Projectile.scale = MathHelper.Lerp(Projectile.scale, 0f, Utils.GetLerpValue(440f, 480f, Timer, true));
            Projectile.Opacity = MathHelper.Clamp(Projectile.scale * 0.87f, 0f, 1f);

            // Begin releasing fireballs.
            if (Timer > 135f && Timer % 4f == 3f)
            {
                SoundEngine.PlaySound(SoundID.Item73, Projectile.Center);
                Vector2 fireballSpawnPosition = Projectile.Center + Main.rand.NextVector2Unit() * new Vector2(3f, 1f) * Main.rand.NextFloat(100f, 170f);
                if (Main.netMode != NetmodeID.MultiplayerClient && !Target.WithinRange(fireballSpawnPosition, 200f))
                {
                    Vector2 fireballShootVelocity = (fireballSpawnPosition - Projectile.Center).SafeNormalize(Vector2.UnitY);
                    fireballShootVelocity = Vector2.Lerp(fireballShootVelocity, (Target.Center - fireballSpawnPosition).SafeNormalize(Vector2.UnitY), 0.35f);
                    fireballShootVelocity = fireballShootVelocity.SafeNormalize(Vector2.UnitY) * 13.5f;
                    Utilities.NewProjectileBetter(fireballSpawnPosition, fireballShootVelocity, ModContent.ProjectileType<LunarFireball>(), 220, 0f);
                }
            }

            // Release asteroids that fly into the black hole.
            if (Timer >= 135f && Timer % 12f == 11f)
            {
                Vector2 asteroidSpawnPosition = Target.Center + Main.rand.NextVector2CircularEdge(700f, 700f);
                Vector2 asteroidShootVelocity = (core.Center - asteroidSpawnPosition).SafeNormalize(Vector2.UnitY) * 11f;
                int asteroid = Utilities.NewProjectileBetter(asteroidSpawnPosition, asteroidShootVelocity, ModContent.ProjectileType<LunarAsteroid>(), 220, 0f);
                if (Main.projectile.IndexInRange(asteroid))
                    Main.projectile[asteroid].ai[0] = core.whoAmI;
            }

            // Explode into a bunch of bolts after enough time has passed.
            if (Main.netMode != NetmodeID.MultiplayerClient && Timer >= 480f)
			{
                for (int i = 0; i < 36; i++)
				{
                    Vector2 boltSpawnPosition = Projectile.Center + Main.rand.NextVector2Circular(40f, 40f);
                    Vector2 boltShootVelocity = (MathHelper.TwoPi * i / 36).ToRotationVector2() * 5f;
                    boltShootVelocity += Main.rand.NextVector2Circular(0.7f, 0.7f);
                    Utilities.NewProjectileBetter(boltSpawnPosition, boltShootVelocity, ProjectileID.PhantasmalBolt, 215, 0f);

                    if (i % 4 == 3)
                        Utilities.NewProjectileBetter(boltSpawnPosition, Vector2.Zero, ModContent.ProjectileType<MoonLordExplosion>(), 0, 0f);
                }
                Projectile.Kill();
			}
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(Projectile.Center, Projectile.scale * 80f, targetHitbox);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D blackHoleTexture = Main.projectileTexture[Projectile.type];
            Texture2D noiseTexture = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/VoronoiShapes").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = noiseTexture.Size() * 0.5f;
            spriteBatch.EnterShaderRegion();

            Vector2 diskScale = Projectile.scale * new Vector2(1.3f, 0.85f);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseOpacity(Projectile.Opacity);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseColor(Color.Green);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseSecondaryColor(Color.Green);
            GameShaders.Misc["CalamityMod:DoGPortal"].Apply();

            spriteBatch.Draw(noiseTexture, drawPosition, null, Color.White, 0f, origin, diskScale, SpriteEffects.None, 0f);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseOpacity(Projectile.Opacity);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseColor(Color.Turquoise);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseSecondaryColor(Color.MediumTurquoise);
            GameShaders.Misc["CalamityMod:DoGPortal"].Apply();

            for (int i = 0; i < 2; i++)
                spriteBatch.Draw(noiseTexture, drawPosition, null, Color.White, 0f, origin, diskScale, SpriteEffects.None, 0f);
            spriteBatch.ExitShaderRegion();

            Vector2 blackHoleScale = Projectile.Size / blackHoleTexture.Size() * Projectile.scale;
            for (int i = 0; i < 3; i++)
                spriteBatch.Draw(blackHoleTexture, drawPosition, null, Color.White, 0f, blackHoleTexture.Size() * 0.5f, blackHoleScale * 1.01f, SpriteEffects.None, 0f);
            for (int i = 0; i < 3; i++)
                spriteBatch.Draw(blackHoleTexture, drawPosition, null, Color.Black, 0f, blackHoleTexture.Size() * 0.5f, blackHoleScale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
