using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class EnergyPortalBeam : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Portal");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 60;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.alpha = 60;
            projectile.penetrate = -1;
            projectile.timeLeft = 90;
        }

        public override void AI()
        {
            projectile.scale = Utils.InverseLerp(0f, 30f, Time, true) * Utils.InverseLerp(0f, 30f, projectile.timeLeft, true);
            projectile.rotation -= MathHelper.TwoPi / 40f;

            if (Time == 32f)
            {
                Main.PlaySound(SoundID.Item9, projectile.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];

                    int energyCount = BossRushEvent.BossRushActive ? 7 : 5;
                    for (int i = 0; i < energyCount; i++)
                    {
                        float shootAngle = MathHelper.Lerp(-0.86f, 0.86f, i / (float)(energyCount - 1f));
                        Vector2 shootVelocity = projectile.SafeDirectionTo(target.Center).RotatedBy(shootAngle) * 5f;
                        Utilities.NewProjectileBetter(projectile.Center, shootVelocity, ModContent.ProjectileType<CeaselessEnergy>(), 250, 0f);
                    }
                }
            }

            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.SetBlendState(BlendState.Additive);

            Texture2D portalTexture = Main.projectileTexture[projectile.type];
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Vector2 origin = portalTexture.Size() * 0.5f;
            Color baseColor = Color.White;

            // Black portal.
            Color color = Color.Lerp(baseColor, Color.Black, 0.55f) * projectile.Opacity * 1.8f;
            spriteBatch.Draw(portalTexture, drawPosition, null, color, projectile.rotation, origin, projectile.scale * 1.2f, SpriteEffects.None, 0f);
            spriteBatch.Draw(portalTexture, drawPosition, null, color, -projectile.rotation, origin, projectile.scale * 1.2f, SpriteEffects.None, 0f);

            // Cyan portal.
            color = Color.Lerp(baseColor, Color.Cyan, 0.55f) * projectile.Opacity * 1.6f;
            spriteBatch.Draw(portalTexture, drawPosition, null, color, projectile.rotation * 0.6f, origin, projectile.scale * 1.2f, SpriteEffects.None, 0f);

            // Magenta portal.
            color = Color.Lerp(baseColor, Color.Fuchsia, 0.55f) * projectile.Opacity * 1.6f;
            spriteBatch.Draw(portalTexture, drawPosition, null, color, projectile.rotation * -0.6f, origin, projectile.scale * 1.2f, SpriteEffects.None, 0f);

            spriteBatch.ResetBlendState();
            return false;
        }
    }
}
