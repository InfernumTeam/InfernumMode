using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Guardians
{
    public class FireCastRitual : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ritual");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 408;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.light = 1f;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            if (CalamityGlobalNPC.doughnutBoss < 0 || !Main.npc[CalamityGlobalNPC.doughnutBoss].active)
            {
                projectile.active = false;
                projectile.netUpdate = true;
                return;
            }

            if (Time > 60f)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (Main.rand.NextBool(5))
                    {
                        Dust fire = Dust.NewDustDirect(projectile.Center + Main.rand.NextFloat(0f, MathHelper.Pi).ToRotationVector2() * -projectile.width * 0.5f, 1, 1, DustID.Fire, 0f, 0f);
                        fire.velocity.X = 0f;
                        fire.velocity.Y = -Math.Abs(fire.velocity.Y - i + projectile.velocity.Y - 4f) * 1f;
                        fire.noGravity = true;
                        fire.fadeIn = 1f;
                        fire.scale = 1f + Main.rand.NextFloat() + i * 0.3f;
                    }
                }
            }

            Time++;
            projectile.Center = Main.npc[CalamityGlobalNPC.doughnutBoss].Center;

            if (projectile.timeLeft < 15)
                projectile.alpha = Utils.Clamp(projectile.alpha + 17, 0, 255);
            else
                projectile.alpha = Utils.Clamp(projectile.alpha - 13, 0, 255);
            projectile.rotation += MathHelper.ToRadians(5f);
		}
    }
}
