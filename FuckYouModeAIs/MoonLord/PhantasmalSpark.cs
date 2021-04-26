using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace InfernumMode.FuckYouModeAIs.MoonLord
{
    public class PhantasmalSpark : ModProjectile
    {
        const int maxTimeLeft = 300;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Phantasmal Spark");
            Main.projFrames[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = 14;
            projectile.height = 18;
            projectile.hostile = true;
            projectile.tileCollide = true;
            projectile.timeLeft = maxTimeLeft;
            cooldownSlot = 2;
        }
        public override void AI()
        {
            if (!NPC.AnyNPCs(NPCID.MoonLordCore))
            {
                projectile.active = false;
                return;
            }
            NPC core = Main.npc[NPC.FindFirstNPC(NPCID.MoonLordCore)];
            projectile.tileCollide = projectile.Hitbox.Intersects(core.GetGlobalNPC<MainAI.FuckYouModeAIsGlobal>().arenaRectangle);
            if (projectile.velocity.X != projectile.velocity.X)
            {
                projectile.velocity.X = projectile.velocity.X * -0.1f;
            }
            if (projectile.velocity.X != projectile.velocity.X)
            {
                projectile.velocity.X = projectile.velocity.X * -0.5f;
            }
            if (projectile.velocity.Y != projectile.velocity.Y && projectile.velocity.Y > 1f)
            {
                projectile.velocity.Y = projectile.velocity.Y * -0.5f;
            }
            projectile.ai[0] += 1f;
            if (projectile.ai[0] > 5f)
            {
                projectile.ai[0] = 5f;
                if (projectile.velocity.Y == 0f && projectile.velocity.X != 0f)
                {
                    projectile.velocity.X = projectile.velocity.X * 0.97f;
                    if (projectile.velocity.X > -0.01 && projectile.velocity.X < 0.01)
                    {
                        projectile.velocity.X = 0f;
                        projectile.netUpdate = true;
                    }
                }
                if (projectile.velocity.Y < 6.5f)
                {
                    projectile.velocity.Y = projectile.velocity.Y + 0.2f;
                }
            }
            projectile.frameCounter++;
            if (projectile.frameCounter > 5)
            {
                projectile.frame++;
                projectile.frameCounter = 0;
            }
            if (projectile.frame > 3)
            {
                projectile.frame = 0;
            }
            projectile.rotation += projectile.velocity.X * 0.1f;
            int dustIndex = Dust.NewDust(projectile.position, projectile.width, projectile.height, 229, 0f, 0f, 100, default, 1f);
            Dust dust = Main.dust[dustIndex];
            dust.position.X -= 2f;
            dust = Main.dust[dustIndex];
            dust.position.Y += 2f;
            Main.dust[dustIndex].scale += Main.rand.Next(50) * 0.01f;
            Main.dust[dustIndex].noGravity = true;
            dust = Main.dust[dustIndex];
            dust.velocity.Y -= 2f;
            if (Main.rand.NextBool(2))
            {
                int dustIndex2 = Dust.NewDust(projectile.position, projectile.width, projectile.height, 229, 0f, 0f, 100, default, 1f);
                dust = Main.dust[dustIndex2];
                dust.position.X -= 2f;
                dust = Main.dust[dustIndex2];
                dust.position.Y += 2f;
                Main.dust[dustIndex2].scale += 0.3f + Main.rand.Next(50) * 0.01f;
                Main.dust[dustIndex2].noGravity = true;
                Main.dust[dustIndex2].velocity *= 0.1f;
            }
            if (projectile.velocity.Y < 0.25 && projectile.velocity.Y > 0.15)
            {
                projectile.velocity.X = projectile.velocity.X * 0.8f;
            }
            projectile.rotation = -projectile.velocity.X * 0.05f;
            if (projectile.velocity.Y > 16f)
            {
                projectile.velocity.Y = 16f;
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity) => false;
        public override void Kill(int timeLeft)
        {
            for (int k = 0; k < 25; k++)
            {
                Vector2 velocity = (MathHelper.TwoPi / 25f * k).ToRotationVector2() * Main.rand.NextFloat(8f, 15f);
                velocity = velocity.RotatedByRandom(MathHelper.ToRadians(7f));
                Dust.NewDust(projectile.position, projectile.width, projectile.height, 229, velocity.X, velocity.Y, 0, default, 1f);
            }
        }
        public override bool CanHitPlayer(Player target) => projectile.timeLeft < maxTimeLeft - 30;
    }
}
