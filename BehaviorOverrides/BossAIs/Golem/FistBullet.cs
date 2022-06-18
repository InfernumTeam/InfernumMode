using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Golem
{
    public class FistBullet : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Fist Bullet");

        public override void SetDefaults()
        {
            projectile.width = 40;
            projectile.height = 40;
            projectile.ignoreWater = true;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 300;
        }

        public override bool PreAI()
        {
            if (!Collision.SolidCollision(projectile.position, projectile.width, projectile.height))
                Lighting.AddLight(projectile.Center, Vector3.One * projectile.Opacity);
            if (projectile.Infernum().ExtraAI[0] < 60f)
            {
                if (Main.player.IndexInRange((int)projectile.Infernum().ExtraAI[2]))
                {
                    Player target = Main.player[(int)projectile.Infernum().ExtraAI[2]];
                    Vector2 shootDirection = projectile.SafeDirectionTo(target.Center + target.velocity * 4f);
                    float rotation = -(projectile.rotation + MathHelper.Pi - (shootDirection.ToRotation() + MathHelper.Pi));
                    projectile.rotation = MathHelper.WrapAngle(projectile.rotation + MathHelper.Clamp(rotation, -MathHelper.ToRadians(10), MathHelper.ToRadians(10)));

                    // Create a line telegraph.
                    if (Main.netMode != NetmodeID.MultiplayerClient && projectile.localAI[0] == 0f)
                    {
                        Utilities.NewProjectileBetter(projectile.Center, shootDirection, ModContent.ProjectileType<FistBulletTelegraph>(), 0, 0f);
                        projectile.localAI[0] = 1f;
                    }
                }
            }
            else if (projectile.Infernum().ExtraAI[0] == 60f)
            {
                Main.PlaySound(SoundID.DD2_WyvernDiveDown, projectile.Center);
                if (Main.player.IndexInRange((int)projectile.Infernum().ExtraAI[2]))
                {
                    Vector2 target = Main.player[(int)projectile.Infernum().ExtraAI[2]].Center;
                    projectile.rotation = projectile.SafeDirectionTo(target).ToRotation();

                    projectile.velocity = projectile.rotation.ToRotationVector2() * (projectile.Distance(target) / 40f);
                }
            }

            projectile.Infernum().ExtraAI[0]++;
            projectile.direction = MathHelper.WrapAngle(projectile.rotation + MathHelper.PiOver2) >= 0 ? 1 : -1;
            return false;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Rectangle rectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Vector2 origin = rectangle.Size() * .5f;
            Color drawColor = projectile.GetAlpha(lightColor);

            Main.spriteBatch.Draw(texture, projectile.Center - Main.screenPosition, rectangle, drawColor, projectile.rotation, origin, projectile.scale, 0, 0f);
            return false;
        }
    }
}
