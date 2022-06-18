using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Prime
{
    public class NuclearExplosion : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Explosion");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 14;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 180;
            projectile.extraUpdates = 1;
            projectile.penetrate = -1;
            projectile.scale = 0.15f;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            projectile.scale += 0.06f;
            projectile.Opacity = Utils.InverseLerp(300f, 265f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 50f, projectile.timeLeft, true);
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (projectile.velocity.Length() < 18f)
                projectile.velocity *= 1.02f;

            if (Main.netMode != NetmodeID.MultiplayerClient && projectile.Opacity > 0.74f)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 spawnPosition = projectile.Center + Main.rand.NextVector2Circular(35f, 35f);
                    Vector2 smokeVelocity = -Vector2.UnitY.RotatedByRandom(2.16f) * Main.rand.NextFloat(6f, 29f);
                    Projectile.NewProjectile(spawnPosition, smokeVelocity, ModContent.ProjectileType<NukeSmoke>(), 0, 0f);
                }
            }

            Lighting.AddLight(projectile.Center, Color.Red.ToVector3());
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.SetBlendState(BlendState.Additive);

            Texture2D texture = Main.projectileTexture[projectile.type];
            Color explosionColor = Color.OrangeRed * projectile.Opacity * 0.65f;
            Vector2 drawPosition = projectile.Center - Main.screenPosition;

            for (int i = 0; i < 2; i++)
                spriteBatch.Draw(texture, drawPosition, null, explosionColor, 0f, texture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);

            spriteBatch.ResetBlendState();
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Utilities.CircularCollision(projectile.Center, targetHitbox, projectile.scale * 135f);
        }

        public override bool CanDamage() => projectile.Opacity > 0.45f;
    }
}
