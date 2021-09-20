using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Events;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.BrimstoneElemental
{
    public class BrimstoneSkull : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public ref float StartingRotation => ref projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brimstone Skull");
            Main.projFrames[projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 36;
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
            projectile.frameCounter++;
            if (projectile.frameCounter >= 7)
            {
                projectile.frame = (projectile.frame + 1) % Main.projFrames[projectile.type];
                projectile.frameCounter = 0;
            }

            if (StartingRotation == 0f)
                StartingRotation = projectile.velocity.ToRotation();

            // Wave up and down over time.
            Vector2 moveOffset = (StartingRotation + MathHelper.PiOver2).ToRotationVector2() * (float)Math.Sin(Time / 9f) * 5f;
            projectile.Center += (StartingRotation + MathHelper.PiOver2).ToRotationVector2() * (float)Math.Sin(Time / 9f) * 5f;

            projectile.spriteDirection = (projectile.velocity.X > 0f).ToDirectionInt();
            projectile.rotation = (projectile.velocity + moveOffset).ToRotation();
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.075f, 0f, 1f);
            if (projectile.spriteDirection == -1)
                projectile.rotation += MathHelper.Pi;

            Lighting.AddLight(projectile.Center, projectile.Opacity * 0.9f, 0f, 0f);

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
				target.AddBuff(ModContent.BuffType<AbyssalFlames>(), 180);
			else
				target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 120);
		}

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 20);
            for (int dust = 0; dust < 6; dust++)
                Dust.NewDust(projectile.position + projectile.velocity, projectile.width, projectile.height, (int)CalamityDusts.Brimstone, 0f, 0f);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)	
        {
			target.Calamity().lastProjectileHit = projectile;
		}
    }
}
