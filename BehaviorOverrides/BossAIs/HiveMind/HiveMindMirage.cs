using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.HiveMind
{
    public class HiveMindMirage : ModProjectile
    {
        public const int TeleportRadius = 300;
        public const int LungeTime = 170;
        public const int DashTime = 60;

        public ref float SpinAngle => ref Projectile.ai[0];
        public ref float AlphaFadeAngle => ref Projectile.localAI[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Hive Mind Mirage");
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 150;
            Projectile.height = 120;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = LungeTime + DashTime;
            Projectile.Calamity().canBreakPlayerDefense = true;
            CooldownSlot = 1;
        }
        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 4)
            {
                Projectile.frame = 0;
            }
            if (CalamityGlobalNPC.hiveMind == -1)
            {
                Projectile.Kill();
                return;
            }
            float lifeRatio = Main.npc[CalamityGlobalNPC.hiveMind].life / (float)Main.npc[CalamityGlobalNPC.hiveMind].lifeMax;
            AlphaFadeAngle += MathHelper.Pi / LungeTime;
            SpinAngle += MathHelper.TwoPi / LungeTime * (lifeRatio < 0.5f ? 1.3f : 1f);
            if (Projectile.timeLeft >= DashTime)
            {
                Projectile.alpha = (int)Utilities.AngularSmoothstep(AlphaFadeAngle, 70f, 250f);
                Projectile.Center = Main.player[Projectile.owner].Center + SpinAngle.ToRotationVector2() * TeleportRadius;
            }
            if (Projectile.timeLeft == DashTime - 1f)
            {
                SoundEngine.PlaySound(SoundID.Roar, (int)Projectile.Center.X, (int)Projectile.Center.Y, 0);
                Projectile.velocity = Projectile.SafeDirectionTo(Main.player[Player.FindClosest(Projectile.Center, 1, 1)].Center) * 12f;
                if (BossRushEvent.BossRushActive)
                    Projectile.velocity *= 1.56f;
            }
            else
                Projectile.alpha += 3;
        }
    }
}
