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
    public class AresTeslaCannonBehaviorOverride : AresCannonBehaviorOverride
    {
        public const int OrbCounterIndex = 2;

        public override int NPCOverrideType => ModContent.NPCType<AresTeslaCannon>();

        public override string GlowmaskTexturePath => "CalamityMod/NPCs/ExoMechs/Ares/AresTeslaCannonGlow";

        public override float AimPredictiveness
        {
            get
            {
                float aimPredictiveness = 28f;

                if (ExoMechManagement.CurrentAresPhase >= 5)
                    aimPredictiveness += 6.5f;

                return aimPredictiveness;
            }
        }

        public override int ShootTime
        {
            get
            {
                int shootTime = 135;

                if (ExoMechManagement.CurrentAresPhase >= 5)
                    shootTime += 35;

                if (ExoMechManagement.CurrentAresPhase >= 6)
                    shootTime -= 45;

                if (AresBodyBehaviorOverride.Enraged)
                    shootTime /= 2;

                return shootTime;
            }
        }

        public static int TotalTeslaOrbsPerBurst
        {
            get
            {
                int orbsPerBurst = 5;

                if (ExoMechManagement.CurrentAresPhase >= 7)
                    orbsPerBurst += 2;

                if (AresBodyBehaviorOverride.Enraged)
                    orbsPerBurst += 3;

                return orbsPerBurst;
            }
        }

        public override int ShootRate => ShootTime / TotalTeslaOrbsPerBurst;

        public override SoundStyle ShootSound => InfernumSoundRegistry.SafeLoadCalamitySound("Sounds/Custom/ExoMechs/TeslaShoot1", CommonCalamitySounds.PlasmaBoltSound);

        public override SoundStyle FireTelegraphSound => AresTeslaCannon.TelSound;

        public override Color TelegraphBackglowColor => Color.Cyan;

        public override void CreateDustTelegraphs(NPC npc, Vector2 endOfCannon)
        {
            Vector2 dustSpawnPosition = endOfCannon + Main.rand.NextVector2Circular(45f, 45f);
            Dust electricity = Dust.NewDustPerfect(dustSpawnPosition, 229);
            electricity.velocity = (endOfCannon - electricity.position) * 0.04f;
            electricity.scale = 1.25f;
            electricity.noGravity = true;
        }

        public override void ShootProjectiles(NPC npc, Vector2 endOfCannon, Vector2 aimDirection)
        {
            int teslaOrbDamage = AresBodyBehaviorOverride.ProjectileDamageBoost + DraedonBehaviorOverride.StrongerNormalShotDamage;
            float orbShootSpeed = 8.5f;
            ref float orbCounter = ref npc.ai[OrbCounterIndex];

            // Shoot slower if pointing downward.
            orbShootSpeed *= MathHelper.Lerp(1f, 0.66f, Utils.GetLerpValue(0.61f, 0.24f, aimDirection.AngleBetween(Vector2.UnitY), true));

            // Make things in general stronger based on Ares' current phase.
            if (ExoMechManagement.CurrentAresPhase >= 5)
                orbShootSpeed *= 1.15f;

            // Fire the tesla orb.
            Utilities.NewProjectileBetter(endOfCannon, aimDirection * orbShootSpeed, ModContent.ProjectileType<AresTeslaOrb>(), teslaOrbDamage, 0f, -1, orbCounter);

            orbCounter++;
        }

        public override Vector2 GetHoverOffset(NPC npc, bool performingCharge)
        {
            if (performingCharge)
                return new(-250f, 150f);

            return new(-375f, 100f);
        }

        public override AresCannonChargeParticleSet GetEnergyDrawer(NPC npc) => npc.ModNPC<AresTeslaCannon>().EnergyDrawer;

        public override Vector2 GetCoreSpritePosition(NPC npc) => npc.ModNPC<AresTeslaCannon>().CoreSpritePosition;
    }
}
