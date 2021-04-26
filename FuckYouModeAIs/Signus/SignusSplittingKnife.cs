using CalamityMod.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Signus
{
	public class SignusSplittingKnife : ModProjectile
    {
        public bool Charging => projectile.ai[0] == 1f;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Knife");
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = 32;
            projectile.height = 32;
            projectile.timeLeft = 180;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.penetrate = -1;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            if (projectile.localAI[0] == 0f)
            {
                projectile.localAI[0] = 1f;
                Main.PlaySound(SoundID.Item73, projectile.position);
            }

            if (projectile.timeLeft % 5 == 4 && projectile.owner == Main.myPlayer)
			{
                Vector2 shootDirection = projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2 + 0.4f);
                Utilities.NewProjectileBetter(projectile.Center, shootDirection * 12f, ModContent.ProjectileType<SignusIdleScythe>(), 285, 0f);

                shootDirection = projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2 + MathHelper.Pi + 0.4f);
                Utilities.NewProjectileBetter(projectile.Center, shootDirection * 12f, ModContent.ProjectileType<SignusIdleScythe>(), 285, 0f);
            }
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver4;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            CalamityGlobalProjectile.DrawCenteredAndAfterimage(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1);
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(InfernumMode.CalamityMod.BuffType("GodSlayerInferno"), 240);
        }
    }
}
