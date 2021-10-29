using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Events;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.CalamitasClone
{
    public class BrimstoneMeteor : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brimstone Meteor");
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
            projectile.timeLeft = 360;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 5 % Main.projFrames[projectile.type];

            projectile.Opacity = Utils.InverseLerp(30f, 42f, Time, true) * Utils.InverseLerp(0f, 12f, projectile.timeLeft, true);
            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;

            if (Time == 30f)
                Main.PlaySound(SoundID.DD2_BetsyFireballShot, projectile.Center);

            Time++;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * projectile.Opacity;

        public override bool CanDamage() => projectile.Opacity >= 1f;

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if ((CalamityWorld.downedProvidence || BossRushEvent.BossRushActive) && CalamitasCloneBehaviorOverride.ReadyToUseBuffedAI)
                target.AddBuff(ModContent.BuffType<AbyssalFlames>(), 120);
			else
				target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 120);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (Time < 30f)
            {
                Vector2 lineDirection = projectile.velocity.SafeNormalize(Vector2.UnitY);
                float lineWidth = (float)Math.Sin(MathHelper.Pi * Time / 30f) * 4f + 1f;
                spriteBatch.DrawLineBetter(projectile.Center - lineDirection * 3400f, projectile.Center + lineDirection * 3400f, Color.Red, lineWidth);
                return false;
            }

            lightColor.R = (byte)(255 * projectile.Opacity);
            Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1);
            return false;
        }

        public override bool ShouldUpdatePosition() => Time >= 30f;

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)	
        {
			target.Calamity().lastProjectileHit = projectile;
		}
    }
}
