using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.EyeOfCthulhu
{
    public class EoCTooth : ModProjectile
    {
        public Player Target => Main.player[(int)projectile.ai[0]];
        public bool HasTouchedGroundYet
		{
            get => projectile.ai[1] == 1f;
            set => projectile.ai[1] = value.ToInt();
		}
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tooth");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 22;
            projectile.hostile = true;
			projectile.ignoreWater = true;
			projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.tileCollide = true;
            projectile.timeLeft = 720;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            if (projectile.velocity.Y < 14f)
                projectile.velocity.Y += 0.3f;
            projectile.alpha = Utils.Clamp(projectile.alpha - 72, 0, 255);

            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver4;
            if (!HasTouchedGroundYet)
                projectile.tileCollide = projectile.Bottom.Y > Target.Top.Y + 30;
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (!HasTouchedGroundYet)
			{
                HasTouchedGroundYet = true;
                projectile.velocity.X = 0f;
                projectile.netUpdate = true;
            }
            return false;
        }

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;

            for (int i = 0; i < 6; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 4f;
                spriteBatch.Draw(texture, drawPosition + drawOffset, null, projectile.GetAlpha(Color.Red) * 0.65f, projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
            }
            spriteBatch.Draw(texture, drawPosition, null, projectile.GetAlpha(lightColor), projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
            return false;
		}

		public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough)
        {
            fallThrough = false;
            return base.TileCollideStyle(ref width, ref height, ref fallThrough);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)	
        {
			target.Calamity().lastProjectileHit = projectile;
		}
    }
}
