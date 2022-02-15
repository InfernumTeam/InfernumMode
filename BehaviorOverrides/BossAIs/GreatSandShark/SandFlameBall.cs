using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark
{
    public class SandFlameBall : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sand Ball");
            Main.projFrames[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 26;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.timeLeft = 100;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
        }

        public override void AI()
        {
            // Play a wind sound.
            if (projectile.localAI[0] == 0f)
            {
                Main.PlaySound(SoundID.DD2_BookStaffCast, projectile.Center);
                projectile.localAI[0] = 1f;
            }

            // Fade in.
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.075f, 0f, 1f);

            // Determine frames.
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 5 % Main.projFrames[projectile.type];

            Time++;
        }

        public override void Kill(int timeLeft)
        {
            Utilities.CreateGenericDustExplosion(projectile.Center, 32, 15, 8f, 1.2f);
            Utilities.CreateGenericDustExplosion(projectile.Center, 65, 8, 9f, 1.35f);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Explode into desert flames.
            Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
            for (int i = 0; i < 3; i++)
            {
                Vector2 shootVelocity = projectile.SafeDirectionTo(target.Center).RotatedBy(MathHelper.Lerp(-0.65f, 0.65f, i / 2f)) * 8f;
                int fuck = Projectile.NewProjectile(projectile.Center, shootVelocity, ProjectileID.DesertDjinnCurse, projectile.damage, 0f);
                Main.projectile[fuck].ai[0] = target.whoAmI;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;

            for (int i = 0; i < 6; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 3f;
                spriteBatch.Draw(texture, drawPosition + drawOffset, frame, projectile.GetAlpha(new Color(0.84f, 0.19f, 0.87f, 0f)) * 0.65f, projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
            }
            spriteBatch.Draw(texture, drawPosition, frame, projectile.GetAlpha(lightColor), projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override bool CanDamage() => projectile.Opacity > 0.9f;
    }
}
