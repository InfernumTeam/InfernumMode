using CalamityMod;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasShadow
{
    public class BrimstoneSlash : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Brimstone Slash");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 3;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 34;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 300;
            Projectile.Opacity = 0f;
            Projectile.Calamity().DealsDefenseDamage = true;
            
        }

        public override void AI()
        {
            CreateVisuals();
            PerformMovement();
        }

        public void CreateVisuals()
        {
            if (Main.dedServ)
                return;

            // Emit crimson light.
            Lighting.AddLight(Projectile.Center, 0.25f, 0f, 0f);

            // Fade in quickly.
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.075f, 0f, 1f);

            // Determine rotation.
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;

            // Emit small magic particles.
            Vector2 magicSpawnPosition = Projectile.Center + Vector2.UnitY.RotatedBy(Projectile.rotation + PiOver2) * Main.rand.NextFloat(4f, 45f) * Main.rand.NextFromList(-1f, 1f);
            Dust magic = Dust.NewDustPerfect(magicSpawnPosition, 261);
            magic.position += Main.rand.NextVector2Circular(10f, 10f);
            magic.velocity = Projectile.velocity * Main.rand.NextFloat(-0.3f, 0.08f);
            magic.color = Color.Lerp(Color.Red, Color.Cyan, Main.rand.NextFloat());
            magic.noGravity = true;
        }

        public void PerformMovement()
        {
            float maxSpeed = 18f;
            if (BossRushEvent.BossRushActive)
                maxSpeed = 24.5f;
            if (Projectile.velocity.Length() < maxSpeed)
                Projectile.velocity *= 1.01f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color drawColor = Color.Lerp(Color.Cyan, Color.Red, 0.35f) * Projectile.Opacity * 0.45f;
            drawColor.A = 0;
            Texture2D slashTexture = ModContent.Request<Texture2D>(Texture).Value;
            for (int i = 0; i < 4; i++)
            {
                Vector2 drawOffset = (Projectile.rotation - PiOver2).ToRotationVector2() * new Vector2(6f, 3f);
                Vector2 drawPosition = Projectile.Center + drawOffset - Main.screenPosition;
                Main.spriteBatch.Draw(slashTexture, drawPosition, null, drawColor, Projectile.rotation, slashTexture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            }
            LumUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], Projectile.GetAlpha(Color.DeepSkyBlue), 1);
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Projectile.RotatingHitboxCollision(targetHitbox.TopLeft(), targetHitbox.Size());
    }
}
