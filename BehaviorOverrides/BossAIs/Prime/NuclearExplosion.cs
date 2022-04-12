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
            Projectile.width = Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 180;
            Projectile.extraUpdates = 1;
            Projectile.penetrate = -1;
            Projectile.scale = 0.15f;
            Projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            Projectile.scale += 0.06f;
            Projectile.Opacity = Utils.GetLerpValue(300f, 265f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 50f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Projectile.velocity.Length() < 18f)
                Projectile.velocity *= 1.02f;

            if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.Opacity > 0.74f)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 spawnPosition = Projectile.Center + Main.rand.NextVector2Circular(35f, 35f);
                    Vector2 smokeVelocity = -Vector2.UnitY.RotatedByRandom(2.16f) * Main.rand.NextFloat(6f, 29f);
                    Projectile.NewProjectile(new InfernumSource(), spawnPosition, smokeVelocity, ModContent.ProjectileType<NukeSmoke>(), 0, 0f);
                }
            }

            Lighting.AddLight(Projectile.Center, Color.Red.ToVector3());
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Texture2D texture = Utilities.ProjTexture(Projectile.type);
            Color explosionColor = Color.OrangeRed * Projectile.Opacity * 0.65f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            for (int i = 0; i < 2; i++)
                Main.spriteBatch.Draw(texture, drawPosition, null, explosionColor, 0f, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);

            Main.spriteBatch.ResetBlendState();
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Utilities.CircularCollision(Projectile.Center, targetHitbox, Projectile.scale * 135f);
        }

        public override bool? CanDamage() => Projectile.Opacity > 0.45f ? null : false;
    }
}
