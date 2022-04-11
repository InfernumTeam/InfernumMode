using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class EnergyPortalBeam : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Portal");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 60;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 60;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90;
        }

        public override void AI()
        {
            Projectile.scale = Utils.GetLerpValue(0f, 30f, Time, true) * Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true);
            Projectile.rotation -= MathHelper.TwoPi / 40f;

            if (Time == 32f)
            {
                SoundEngine.PlaySound(SoundID.Item9, Projectile.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];

                    int energyCount = BossRushEvent.BossRushActive ? 7 : 5;
                    for (int i = 0; i < energyCount; i++)
                    {
                        float shootAngle = MathHelper.Lerp(-0.86f, 0.86f, i / (float)(energyCount - 1f));
                        Vector2 shootVelocity = Projectile.SafeDirectionTo(target.Center).RotatedBy(shootAngle) * 5f;
                        Utilities.NewProjectileBetter(Projectile.Center, shootVelocity, ModContent.ProjectileType<CeaselessEnergy>(), 250, 0f);
                    }
                }
            }

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Texture2D portalTexture = Utilities.ProjTexture(Projectile.type);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = portalTexture.Size() * 0.5f;
            Color baseColor = Color.White;

            // Black portal.
            Color color = Color.Lerp(baseColor, Color.Black, 0.55f) * Projectile.Opacity * 1.8f;
            Main.spriteBatch.Draw(portalTexture, drawPosition, null, color, Projectile.rotation, origin, Projectile.scale * 1.2f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(portalTexture, drawPosition, null, color, -Projectile.rotation, origin, Projectile.scale * 1.2f, SpriteEffects.None, 0f);

            // Cyan portal.
            color = Color.Lerp(baseColor, Color.Cyan, 0.55f) * Projectile.Opacity * 1.6f;
            Main.spriteBatch.Draw(portalTexture, drawPosition, null, color, Projectile.rotation * 0.6f, origin, Projectile.scale * 1.2f, SpriteEffects.None, 0f);

            // Magenta portal.
            color = Color.Lerp(baseColor, Color.Fuchsia, 0.55f) * Projectile.Opacity * 1.6f;
            Main.spriteBatch.Draw(portalTexture, drawPosition, null, color, Projectile.rotation * -0.6f, origin, Projectile.scale * 1.2f, SpriteEffects.None, 0f);

            Main.spriteBatch.ResetBlendState();
            return false;
        }
    }
}
