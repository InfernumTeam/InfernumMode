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

namespace InfernumMode.FuckYouModeAIs.BrimstoneElemental
{
    public class BrimstonePetal : ModProjectile
    {
        public Vector2 StartingVelocity;
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brimstone Petal");
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 14;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 420;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(0f, 25f, Time, true) * Utils.InverseLerp(0f, 25f, projectile.timeLeft, true);
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            projectile.velocity *= 1.0115f;

            Lighting.AddLight(projectile.Center, projectile.Opacity * 0.9f, 0f, 0f);

            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];

            for (int i = 0; i < 6; i++)
            {
                Color magicAfterimageColor = Color.Red * projectile.Opacity * 0.22f;
                magicAfterimageColor.A = 0;

                Vector2 drawPosition = projectile.Center - Main.screenPosition + (MathHelper.TwoPi * i / 6f).ToRotationVector2() * projectile.Opacity * 4f;
                spriteBatch.Draw(texture, drawPosition, null, magicAfterimageColor, projectile.rotation, texture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);
            }

            spriteBatch.Draw(texture, projectile.Center - Main.screenPosition, null, projectile.GetAlpha(lightColor), projectile.rotation, texture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

		public override void OnHitPlayer(Player target, int damage, bool crit)
        {
			if (CalamityWorld.downedProvidence || BossRushEvent.BossRushActive)
				target.AddBuff(ModContent.BuffType<AbyssalFlames>(), 120);
			else
				target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 120);
		}

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)	
        {
			target.Calamity().lastProjectileHit = projectile;
		}
    }
}
