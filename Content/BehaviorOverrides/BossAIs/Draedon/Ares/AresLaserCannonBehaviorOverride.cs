using CalamityMod;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.Particles;
using CalamityMod.Sounds;
using InfernumMode.Assets.Sounds;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class AresLaserCannonBehaviorOverride : AresCannonBehaviorOverride
    {
        public const int LaserCounterIndex = 2;

        public override int NPCOverrideType => ModContent.NPCType<AresLaserCannon>();

        public override string GlowmaskTexturePath => "CalamityMod/NPCs/ExoMechs/Ares/AresLaserCannonGlow";

        public override float AimPredictiveness
        {
            get
            {
                if (ExoMechManagement.CurrentAresPhase >= 5)
                    return 30.5f;

                return 25f;
            }
        }

        public override int ShootTime
        {
            get
            {
                int shootTime = 480;
                if (ExoMechManagement.CurrentAresPhase >= 5)
                    shootTime += 105;

                if (AresBodyBehaviorOverride.Enraged)
                    shootTime /= 3;

                return shootTime;
            }
        }

        public static int TotalLasersPerBurst
        {
            get
            {
                int lasersPerBurst = 12;

                if (Ares.ai[0] == (int)AresBodyBehaviorOverride.AresBodyAttackType.PhotonRipperSlashes)
                    lasersPerBurst = 5;

                return lasersPerBurst;
            }
        }

        public override int ShootRate => ShootTime / TotalLasersPerBurst;

        public override SoundStyle ShootSound => InfernumSoundRegistry.SafeLoadCalamitySound("Sounds/Custom/ExoMechs/ExoLaserShoot", CommonCalamitySounds.LaserCannonSound);

        public override SoundStyle FireTelegraphSound => AresLaserCannon.TelSound;

        public override Color TelegraphBackglowColor => Color.Red;

        public override void ResetAttackCycleEffects(NPC npc) => npc.ai[LaserCounterIndex] = 0f;

        public override void CreateDustTelegraphs(NPC npc, Vector2 endOfCannon)
        {
            Vector2 dustSpawnPosition = endOfCannon + Main.rand.NextVector2Circular(45f, 45f);
            Dust laser = Dust.NewDustPerfect(dustSpawnPosition, 182);
            laser.velocity = (endOfCannon - laser.position) * 0.04f;
            laser.scale = 1.25f;
            laser.noGravity = true;
        }

        public override void ShootProjectiles(NPC npc, Vector2 endOfCannon, Vector2 aimDirection)
        {
            int totalLasersPerBurst = 1;
            int laserDamage = AresBodyBehaviorOverride.ProjectileDamageBoost + DraedonBehaviorOverride.StrongerNormalShotDamage;
            bool photonRipperAttack = Ares.ai[0] == (int)AresBodyBehaviorOverride.AresBodyAttackType.PhotonRipperSlashes;
            float laserShootSpeed = 10.6f;
            ref float laserShootCounter = ref npc.ai[LaserCounterIndex];

            // Every third shot releases more lasers than usual.
            if (laserShootCounter % 3f == 2f)
                totalLasersPerBurst += 2;

            // Make things in general stronger based on Ares' current phase.
            if (ExoMechManagement.CurrentAresPhase >= 3)
            {
                laserShootSpeed *= 0.9f;

                if (totalLasersPerBurst >= 3)
                    totalLasersPerBurst += 2;
            }

            // Make things a bit less chaotic during the photon ripper attack.
            if (photonRipperAttack)
            {
                totalLasersPerBurst = 5;
                laserShootSpeed -= 2.3f;
            }

            // Fire the lasers.
            for (int i = 0; i < totalLasersPerBurst; i++)
            {
                Vector2 laserShootVelocity = aimDirection * laserShootSpeed;
                if (totalLasersPerBurst > 1)
                    laserShootVelocity = laserShootVelocity.RotatedBy(MathHelper.Lerp(-0.52f, 0.52f, i / (float)(totalLasersPerBurst - 1f)));

                // Add a small amount of randomness to laser directions.
                laserShootVelocity = laserShootVelocity.RotatedByRandom(0.07f);
                Utilities.NewProjectileBetter(endOfCannon, laserShootVelocity, ModContent.ProjectileType<AresCannonLaser>(), laserDamage, 0f, -1, 0f, npc.whoAmI);
            }

            laserShootCounter++;
        }

        public override Vector2 GetHoverOffset(NPC npc, bool performingCharge)
        {
            float backArmDirection = (Ares.Infernum().ExtraAI[ExoMechManagement.Ares_BackArmsAreSwappedIndex] == 1f).ToDirectionInt();

            if (performingCharge)
                return new(backArmDirection * 380f, 150f);

            return new(backArmDirection * 575f, 0f);
        }

        public override AresCannonChargeParticleSet GetEnergyDrawer(NPC npc) => npc.ModNPC<AresLaserCannon>().EnergyDrawer;

        public override Vector2 GetCoreSpritePosition(NPC npc) => npc.ModNPC<AresLaserCannon>().CoreSpritePosition;
    }
}
