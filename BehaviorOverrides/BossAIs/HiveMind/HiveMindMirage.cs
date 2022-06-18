using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.HiveMind
{
    public class HiveMindMirage : ModProjectile
    {
        public const int TeleportRadius = 300;
        public const int LungeTime = 170;
        public const int DashTime = 60;

        public ref float SpinAngle => ref projectile.ai[0];
        public ref float AlphaFadeAngle => ref projectile.localAI[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Hive Mind Mirage");
            Main.projFrames[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = 150;
            projectile.height = 120;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = LungeTime + DashTime;
            projectile.Calamity().canBreakPlayerDefense = true;
            cooldownSlot = 1;
        }
        public override void AI()
        {
            projectile.frameCounter++;
            if (projectile.frameCounter > 4)
            {
                projectile.frame++;
                projectile.frameCounter = 0;
            }
            if (projectile.frame >= 4)
            {
                projectile.frame = 0;
            }
            if (CalamityGlobalNPC.hiveMind == -1)
            {
                projectile.Kill();
                return;
            }
            float lifeRatio = Main.npc[CalamityGlobalNPC.hiveMind].life / (float)Main.npc[CalamityGlobalNPC.hiveMind].lifeMax;
            AlphaFadeAngle += MathHelper.Pi / LungeTime;
            SpinAngle += MathHelper.TwoPi / LungeTime * (lifeRatio < 0.5f ? 1.3f : 1f);
            if (projectile.timeLeft >= DashTime)
            {
                projectile.alpha = (int)Utilities.AngularSmoothstep(AlphaFadeAngle, 70f, 250f);
                projectile.Center = Main.player[projectile.owner].Center + SpinAngle.ToRotationVector2() * TeleportRadius;
            }
            if (projectile.timeLeft == DashTime - 1f)
            {
                Main.PlaySound(SoundID.Roar, (int)projectile.Center.X, (int)projectile.Center.Y, 0);
                projectile.velocity = projectile.SafeDirectionTo(Main.player[Player.FindClosest(projectile.Center, 1, 1)].Center) * 12f;
                if (BossRushEvent.BossRushActive)
                    projectile.velocity *= 1.56f;
            }
            else
                projectile.alpha += 3;
        }
    }
}
