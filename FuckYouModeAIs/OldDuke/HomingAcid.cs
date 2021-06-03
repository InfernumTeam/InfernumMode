using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Projectiles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.OldDuke
{
    public class HomingAcid : ModProjectile
    {
        public bool HasHitPlayer
        {
            get => projectile.ai[0] == 1f;
            set => projectile.ai[0] = value.ToInt();
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Acid");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 16;
            projectile.hostile = true;
			projectile.ignoreWater = true;
			projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.timeLeft = 240;
            projectile.tileCollide = false;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            if (projectile.localAI[0] == 0f)
            {
                Main.PlaySound(SoundID.Item13, projectile.position);
                projectile.localAI[0] = 1f;
            }

            Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
            projectile.velocity = (projectile.velocity * 20f + projectile.DirectionTo(target.Center) * 14f) / 21f;

            float pushForce = 0.1f;
            for (int k = 0; k < Main.maxProjectiles; k++)
            {
                Projectile otherProj = Main.projectile[k];

                // Short circuits to make the loop as fast as possible
                if (!otherProj.active || otherProj.type != projectile.type || k == projectile.whoAmI)
                    continue;

                // If the other projectile is too close, nudge them away.
                bool sameProjType = otherProj.type == projectile.type;
                float taxicabDist = Math.Abs(projectile.position.X - otherProj.position.X) + Math.Abs(projectile.position.Y - otherProj.position.Y);
                if (sameProjType && taxicabDist < projectile.width)
                {
                    if (projectile.position.X < otherProj.position.X)
                        projectile.velocity.X -= pushForce;
                    else
                        projectile.velocity.X += pushForce;

                    if (projectile.position.Y < otherProj.position.Y)
                        projectile.velocity.Y -= pushForce;
                    else
                        projectile.velocity.Y += pushForce;
                }
            }

            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255, 127);
        }

        public override void Kill(int timeLeft)
        {
            CalamityGlobalProjectile.ExpandHitboxBy(projectile, 60);
            if (!HasHitPlayer)
                projectile.Damage();

            for (int i = 0; i < 3; i++)
            {
                Dust dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, (int)CalamityMod.Dusts.CalamityDusts.SulfurousSeaAcid, 0f, 0f, 100, default, 1.2f);
                dust.velocity *= 3f;
                dust.noGravity = true;
                if (Main.rand.NextBool(2))
                {
                    dust.scale = 0.5f;
                    dust.fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
            }
            for (int i = 0; i < 5; i++)
            {
                Dust dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, (int)CalamityMod.Dusts.CalamityDusts.SulfurousSeaAcid, 0f, 0f, 100, default, 1.7f);
                dust.noGravity = true;
                dust.velocity *= 5f;

                dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, (int)CalamityMod.Dusts.CalamityDusts.SulfurousSeaAcid, 0f, 0f, 100, default, 1f);
                dust.velocity *= 2f;
            }
        }

		public override void OnHitPlayer(Player target, int damage, bool crit)
		{
			target.AddBuff(ModContent.BuffType<Irradiated>(), 180);
            HasHitPlayer = true;
            projectile.Kill();
		}

		public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)	
        {
			target.Calamity().lastProjectileHit = projectile;
		}
    }
}
