using CalamityMod.Sounds;
using InfernumMode.Assets.Sounds;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Prime.PrimeHeadBehaviorOverride;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Prime
{
    public class PrimeLaserBehaviorOverride : PrimeHandBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.PrimeLaser;

        public override float PredictivenessFactor => 20f;

        public override Color TelegraphColor => Color.Red;

        public static SoundStyle LaserShootSound => InfernumSoundRegistry.SafeLoadCalamitySound("Sounds/Custom/ExoMechs/ExoLaserShoot", CommonCalamitySounds.LaserCannonSound);

        public override void PerformTelegraphBehaviors(NPC npc, PrimeAttackType attackState, float telegraphIntensity, Vector2 cannonDirection)
        {
            Vector2 endOfCannon = npc.Center + cannonDirection * npc.width * npc.scale * 0.42f;
            for (int i = 0; i < 3; i++)
            {
                if (Main.rand.NextFloat() >= telegraphIntensity)
                    continue;

                Dust laser = Dust.NewDustPerfect(endOfCannon + Main.rand.NextVector2Circular(30f, 30f), 182);
                laser.velocity = (endOfCannon - laser.position) * 0.04f;
                laser.scale = 1.25f;
                laser.noGravity = true;
            }
        }

        public override void PerformAttackBehaviors(NPC npc, PrimeAttackType attackState, Player target, float attackTimer, Vector2 cannonDirection)
        {
            int shootRate = 35;
            int burstCount = 1;
            float laserSpeed = 7f;
            float laserSpread = 0.61f;

            if (npc.life < npc.lifeMax * Phase2LifeRatio)
            {
                shootRate -= 7;
                laserSpeed += 2.4f;
            }

            // Release lasers.
            if (attackTimer % shootRate == 0f)
            {
                SoundEngine.PlaySound(LaserShootSound with { Volume = 1.4f }, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < burstCount; i++)
                    {
                        Vector2 laserVelocity = cannonDirection * laserSpeed;
                        if (burstCount >= 2)
                            laserVelocity = laserVelocity.RotatedBy(MathHelper.Lerp(-laserSpread, laserSpread, i / (burstCount - 1f)));

                        Utilities.NewProjectileBetter(npc.Center + cannonDirection * npc.width * npc.scale * 0.4f, laserVelocity, ModContent.ProjectileType<PrimeSmallLaser>(), SmallLaserDamage, 0f);
                    }
                }
            }
        }
    }
}