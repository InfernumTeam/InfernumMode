using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.NPCs;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Clone
{
    public class LifeSeekerProj : ModProjectile
    {
        public Player Target => Main.player[projectile.owner];
        public ref float FireCountdown => ref projectile.ai[0];
        public ref float OffsetAngle => ref projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Life Seeker");
            Main.projFrames[projectile.type] = 6;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 30;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 600;
            projectile.alpha = 255;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.alpha = Utils.Clamp(projectile.alpha - 30, 0, 255);
            Lighting.AddLight(projectile.Center, Color.Red.ToVector3() * 0.825f);

            if (!Main.npc.IndexInRange(CalamityGlobalNPC.calamitas) || !Main.npc[CalamityGlobalNPC.calamitas].active)
			{
                projectile.Kill();
                return;
			}

            if (FireCountdown > 0f)
            {
                projectile.rotation = projectile.AngleTo(Target.Center);
                projectile.Center = Target.Center - Vector2.UnitY.RotatedBy(OffsetAngle) * 480f;
            }
            else
                projectile.rotation = projectile.velocity.ToRotation();

            if (FireCountdown >= 0f && FireCountdown < 20f)
                projectile.velocity = projectile.SafeDirectionTo(Target.Center) * MathHelper.Lerp(-70f, 6f, (float)Math.Sin(MathHelper.Pi * FireCountdown / 20f));
            if (FireCountdown == 0f)
            {
                projectile.velocity = projectile.SafeDirectionTo(Target.Center) * 18.5f;
                if (Main.myPlayer == Target.whoAmI)
                    Utilities.NewProjectileBetter(projectile.Center, projectile.velocity * 1.2f, ModContent.ProjectileType<BrimstoneBarrage>(), 95, 0f, Target.whoAmI);
            }
            if (FireCountdown < -15f)
            {
                projectile.velocity = projectile.velocity.RotateTowards(projectile.AngleTo(Target.Center), 0.017f);
                if (projectile.velocity.Length() > 10f)
                    projectile.velocity *= 0.997f;
                projectile.tileCollide = true;
            }

            projectile.frameCounter++;
            if (projectile.frameCounter % 6 == 5)
                projectile.frame = (projectile.frame + 1) % Main.projFrames[projectile.type];
            FireCountdown--;
        }

		public override void OnHitPlayer(Player target, int damage, bool crit)
        {
			if (projectile.Opacity != 1f)
				return;

            target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 120);
        }

        public override void Kill(int timeLeft)
        {
            for (int i = 0; i < 20; i++)
                Dust.NewDust(projectile.position, projectile.width, projectile.height, (int)CalamityDusts.Brimstone);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;
    }
}
