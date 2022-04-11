using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public class RealityBreakPortalLaserWall : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Portal");

        public override void SetDefaults()
        {
            Projectile.width = 90;
            Projectile.height = 90;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 100;
        }

        public override void AI()
        {
            Projectile.rotation += 0.325f;

            Time++;
            if (Time == 60f)
            {
                SoundEngine.PlaySound(SoundID.Item12, Projectile.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Player closest = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];

                    float shootInterpolant = Utils.GetLerpValue(600f, 1450f, Projectile.Distance(closest.Center), true);
                    int shootCount = (int)MathHelper.Lerp(5f, 12f, shootInterpolant);
                    float shootSpeed = MathHelper.Lerp(15f, 25f, shootInterpolant);
                    for (int i = 0; i < shootCount; i++)
                    {
                        Vector2 shootVelocity = Projectile.SafeDirectionTo(closest.Center).RotatedBy(MathHelper.Lerp(-0.6f, 0.6f, i / (float)(shootCount - 1f))) * shootSpeed;
                        Projectile.NewProjectile(Projectile.Center, shootVelocity, InfernumMode.CalamityMod.Find<ModProjectile>("DoGDeath").Type, 85, 0f, Projectile.owner);
                    }
                }
            }

            Projectile.Opacity = Utils.GetLerpValue(0f, 50f, Time, true) * Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
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
