using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Golem
{
    public class GolemLaser : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Heat Laser");
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 8;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.Opacity = 0f;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Create a puff of energy when spawned.
            if (Projectile.localAI[0] == 0f)
            {
                for (int i = 0; i < 8; i++)
                {
                    Vector2 fireSpawnPosition = Projectile.Center + Main.rand.NextVector2Circular(16f, 16f);
                    Vector2 fireVelocity = (TwoPi * i / 8f).ToRotationVector2() * Main.rand.NextFloat(1.5f, 2f);
                    Particle fire = new MediumMistParticle(fireSpawnPosition, fireVelocity, Color.Orange, Color.Gray, Main.rand.NextFloat(0.7f, 0.9f), 236f, Main.rand.NextFloat(-0.04f, 0.04f));
                    GeneralParticleHandler.SpawnParticle(fire);
                }
                Projectile.localAI[0] = 1f;
            }

            // Fade in.
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.1f, 0f, 1f);

            // Decide rotation.
            Projectile.rotation = Projectile.velocity.ToRotation();

            // Decide frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Type];

            // Accelerate.
            if (Projectile.velocity.Length() < 13f)
                Projectile.velocity *= 1.015f;

            Lighting.AddLight(Projectile.Center, Color.Red.ToVector3());
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;

            Main.spriteBatch.Draw(texture, drawPosition, frame, Projectile.GetAlpha(Color.White), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
