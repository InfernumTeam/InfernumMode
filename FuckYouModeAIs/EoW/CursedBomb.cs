using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.EoW
{
	public class CursedBomb : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Cursed Fire Bomb");

        public override void SetDefaults()
        {
            projectile.scale = 1.3f;
            projectile.width = projectile.height = 16;
            projectile.hostile = true;
			projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 210;
		}

        public override void AI()
        {
            projectile.tileCollide = projectile.timeLeft < 90;
            projectile.rotation += (projectile.velocity.X > 0f).ToDirectionInt() * 0.3f;

            Lighting.AddLight(projectile.Center, Vector3.One);
            if (Main.dedServ)
                return;

            for (int i = 0; i < 4; i++)
            {
                Dust cursedFlame = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(projectile.width, projectile.height), 267);
                cursedFlame.velocity = Vector2.UnitY.RotatedBy(projectile.velocity.ToRotation()) * Main.rand.NextFloat(2f, 3.5f);
                cursedFlame.color = Color.Lerp(Color.Green, Color.GreenYellow, Main.rand.NextFloat(0.7f));
                cursedFlame.scale = Main.rand.NextFloat(1.3f, 1.55f);
                cursedFlame.noGravity = true;
            }
        }

        public override void Kill(int timeLeft)
        {
			Main.PlaySound(SoundID.DD2_BetsyFireballShot, (int)projectile.position.X, (int)projectile.position.Y);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            float baseAngleOffset = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < 8; i++)
            {
                Vector2 shootVelocity = (MathHelper.TwoPi * i / 8f + baseAngleOffset).ToRotationVector2() * 4f;
                Utilities.NewProjectileBetter(projectile.Center + shootVelocity, shootVelocity, ModContent.ProjectileType<CursedBullet>(), 65, 0f);
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 drawPosition = projectile.Center - Main.screenPosition;

            spriteBatch.Draw(texture, drawPosition, null, Color.White, projectile.rotation, texture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
