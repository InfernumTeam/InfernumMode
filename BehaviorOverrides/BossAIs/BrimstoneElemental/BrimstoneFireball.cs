using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Events;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.BrimstoneElemental
{
    public class BrimstoneFireball : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public ref float InitialSpeed => ref projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brimstone Flame");
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 16;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 420;
            projectile.alpha = 225;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.rotation += (projectile.velocity.X > 0f).ToDirectionInt();
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.04f, 0f, 1f);

            if (InitialSpeed == 0f)
                InitialSpeed = projectile.velocity.Length();

            bool horizontalVariant = projectile.identity % 2 == 1;
            if (Time < 60f)
            {
                Vector2 idealVelocity = Vector2.UnitX * (projectile.velocity.X > 0f).ToDirectionInt() * InitialSpeed;
                if (!horizontalVariant)
                    idealVelocity = Vector2.UnitY * (projectile.velocity.Y > 0f).ToDirectionInt() * InitialSpeed;
                projectile.velocity = Vector2.Lerp(projectile.velocity, idealVelocity, Time / 300f);
            }
            else if (Time > 90f && projectile.velocity.Length() < 36f)
            {
                if (horizontalVariant)
                    projectile.velocity *= new Vector2(1.01f, 0.98f);
                else
                    projectile.velocity *= new Vector2(0.98f, 1.01f);
            }

            Lighting.AddLight(projectile.Center, projectile.Opacity * 0.6f, 0f, 0f);

            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
			lightColor.R = (byte)(255 * projectile.Opacity);
			Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1);
            return false;
        }

		public override void OnHitPlayer(Player target, int damage, bool crit)
        {
			if (CalamityWorld.downedProvidence || BossRushEvent.BossRushActive)
				target.AddBuff(ModContent.BuffType<AbyssalFlames>(), 120);
			else
				target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 60);
		}

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 20);
            for (int dust = 0; dust < 4; dust++)
                Dust.NewDust(projectile.position + projectile.velocity, projectile.width, projectile.height, (int)CalamityDusts.Brimstone, 0f, 0f);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)	
        {
			target.Calamity().lastProjectileHit = projectile;
		}
    }
}
