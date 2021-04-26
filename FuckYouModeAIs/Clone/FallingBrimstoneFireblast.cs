using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using CalamityMod;

namespace InfernumMode.FuckYouModeAIs.Clone
{
    public class FallingBrimstoneFireblast : ModProjectile
    {
        public const float Gravity = 0.35f;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brimstone Gigablast");
            Main.projFrames[projectile.type] = 6;
        }

        public override void SetDefaults()
        {
			projectile.width = 50;
            projectile.height = 50;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.penetrate = 1;
			projectile.timeLeft = 120;
			projectile.Opacity = 0f;
			cooldownSlot = 1;
        }

		public override void AI()
		{
			projectile.frameCounter++;
			if (projectile.frameCounter % 5 == 4)
				projectile.frame = (projectile.frame + 1) % Main.projFrames[projectile.type];

            projectile.Opacity = MathHelper.Clamp(1f - ((projectile.timeLeft - 60) / 60f), 0f, 1f);

            Lighting.AddLight(projectile.Center, projectile.Opacity * 0.9f, 0f, 0f);

			projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;

			if (projectile.localAI[0] == 0f)
			{
				projectile.localAI[0] = 1f;
				Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 20);
			}

            projectile.velocity.Y += Gravity;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            int frameHeight = texture.Height / Main.projFrames[projectile.type];
            int drawStart = frameHeight * projectile.frame;
			lightColor.R = (byte)(255 * projectile.Opacity);
			spriteBatch.Draw(texture, projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(new Rectangle(0, drawStart, texture.Width, frameHeight)), projectile.GetAlpha(lightColor), projectile.rotation, new Vector2(texture.Width / 2f, frameHeight / 2f), projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

		public override bool CanHitPlayer(Player target) => projectile.Opacity == 1f;

		public override void OnHitPlayer(Player target, int damage, bool crit)
        {
			if (projectile.Opacity != 1f)
				return;

			target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 180);
        }

        public override void Kill(int timeLeft)
        {
            for (int j = 0; j < 2; j++)
                Dust.NewDust(projectile.position, projectile.width, projectile.height, (int)CalamityDusts.Brimstone, 0f, 0f, 50, default, 1f);

            for (int k = 0; k < 20; k++)
            {
                int redFire = Dust.NewDust(projectile.position, projectile.width, projectile.height, (int)CalamityDusts.Brimstone, 0f, 0f, 0, default, 1.5f);
                Main.dust[redFire].noGravity = true;
                Main.dust[redFire].velocity *= 3f;
                redFire = Dust.NewDust(projectile.position, projectile.width, projectile.height, (int)CalamityDusts.Brimstone, 0f, 0f, 50, default, 1f);
                Main.dust[redFire].velocity *= 2f;
                Main.dust[redFire].noGravity = true;
            }
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)	
        {
			target.Calamity().lastProjectileHit = projectile;
		}
    }
}
