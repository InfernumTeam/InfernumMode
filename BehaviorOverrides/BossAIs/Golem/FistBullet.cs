using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.Golem
{
    public class FistBullet : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Fist Bullet");

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
        }

        public override bool PreAI()
        {
            if (!Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
                Lighting.AddLight(Projectile.Center, Vector3.One * Projectile.Opacity);
            if (Projectile.Infernum().ExtraAI[0] < 60f)
            {
                if (Main.player.IndexInRange((int)Projectile.Infernum().ExtraAI[2]))
                {
                    Player target = Main.player[(int)Projectile.Infernum().ExtraAI[2]];
                    Vector2 shootDirection = Projectile.SafeDirectionTo(target.Center + target.velocity * 4f);
                    float rotation = -(Projectile.rotation + MathHelper.Pi - (shootDirection.ToRotation() + MathHelper.Pi));
                    Projectile.rotation = MathHelper.WrapAngle(Projectile.rotation + MathHelper.Clamp(rotation, -MathHelper.ToRadians(10), MathHelper.ToRadians(10)));

                    // Create a line telegraph.
                    if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.localAI[0] == 0f)
                    {
                        Utilities.NewProjectileBetter(Projectile.Center, shootDirection, ModContent.ProjectileType<FistBulletTelegraph>(), 0, 0f);
                        Projectile.localAI[0] = 1f;
                    }
                }
            }
            else if (Projectile.Infernum().ExtraAI[0] == 60f)
            {
                SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown, Projectile.Center);
                if (Main.player.IndexInRange((int)Projectile.Infernum().ExtraAI[2]))
                {
                    Vector2 target = Main.player[(int)Projectile.Infernum().ExtraAI[2]].Center;
                    Projectile.rotation = Projectile.SafeDirectionTo(target).ToRotation();

                    Projectile.velocity = Projectile.rotation.ToRotationVector2() * (Projectile.Distance(target) / 40f);
                }
            }

            Projectile.Infernum().ExtraAI[0]++;
            Projectile.direction = MathHelper.WrapAngle(Projectile.rotation + MathHelper.PiOver2) >= 0 ? 1 : -1;
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Utilities.ProjTexture(Projectile.type);
            Rectangle rectangle = new(0, 0, texture.Width, texture.Height);
            Vector2 origin = rectangle.Size() * .5f;
            Color drawColor = Projectile.GetAlpha(lightColor);

            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, rectangle, drawColor, Projectile.rotation, origin, Projectile.scale, 0, 0f);
            return false;
        }
    }
}
