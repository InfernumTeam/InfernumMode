using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public class RealityBreakPortalBeam : ModProjectile
    {
        public Vector2 AimDestination;
        public override void SetStaticDefaults() => DisplayName.SetDefault("Portal");

        public override void SetDefaults()
        {
            Projectile.width = 90;
            Projectile.height = 90;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 60;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
        }

        public override void AI()
        {
            Projectile.ai[0] += 1f;
            if (Projectile.ai[0] == 60f)
            {
                SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Item/LaserCannon"), Projectile.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 rayDirection = Projectile.SafeDirectionTo(AimDestination);
                    Utilities.NewProjectileBetter(Projectile.Center, rayDirection, ModContent.ProjectileType<DoGDeathray>(), 600, 0f, Main.myPlayer, 0f, Projectile.whoAmI);
                }
            }
            else if (Projectile.ai[0] <= 45f)
            {
                Player closest = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                AimDestination = closest.Center + closest.velocity * 37.5f;
            }

            Projectile.rotation -= MathHelper.TwoPi / 100f;
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (Projectile.ai[0] <= 60f)
                spriteBatch.DrawLineBetter(Projectile.Center, Projectile.Center + Projectile.AngleTo(AimDestination).ToRotationVector2() * 5000f, Color.Cyan, 3f);

            spriteBatch.SetBlendState(BlendState.Additive);

            Texture2D portalTexture = Main.projectileTexture[Projectile.type];
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = portalTexture.Size() * 0.5f;
            Color baseColor = Color.White;

            // Black portal.
            Color color = Color.Lerp(baseColor, Color.Black, 0.55f) * Projectile.Opacity * 1.8f;
            spriteBatch.Draw(portalTexture, drawPosition, null, color, Projectile.rotation, origin, Projectile.scale * 1.2f, SpriteEffects.None, 0f);
            spriteBatch.Draw(portalTexture, drawPosition, null, color, -Projectile.rotation, origin, Projectile.scale * 1.2f, SpriteEffects.None, 0f);

            // Cyan portal.
            color = Color.Lerp(baseColor, Color.Cyan, 0.55f) * Projectile.Opacity * 1.6f;
            spriteBatch.Draw(portalTexture, drawPosition, null, color, Projectile.rotation * 0.6f, origin, Projectile.scale * 1.2f, SpriteEffects.None, 0f);

            // Magenta portal.
            color = Color.Lerp(baseColor, Color.Fuchsia, 0.55f) * Projectile.Opacity * 1.6f;
            spriteBatch.Draw(portalTexture, drawPosition, null, color, Projectile.rotation * -0.6f, origin, Projectile.scale * 1.2f, SpriteEffects.None, 0f);

            spriteBatch.ResetBlendState();
            return false;
        }
    }
}
