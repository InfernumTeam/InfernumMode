using CalamityMod;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.Particles;
using InfernumMode.Sounds;
using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class AresPlasmaCannonBehaviorOverride : AresCannonBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<AresPlasmaFlamethrower>();

        public override string GlowmaskTexturePath => "CalamityMod/NPCs/ExoMechs/Ares/AresPlasmaFlamethrowerGlow";

        public override float AimPredictiveness
        {
            get
            {
                float aimPredictiveness = 20f;

                if (ExoMechManagement.CurrentAresPhase >= 5)
                    aimPredictiveness += 6f;

                if (ExoMechManagement.CurrentAresPhase >= 6)
                    aimPredictiveness -= 2f;

                return aimPredictiveness;
            }
        }

        public override int ShootTime
        {
            get
            {
                int shootTime = 150;
                
                if (ExoMechManagement.CurrentAresPhase >= 5)
                    shootTime += 60;

                if (ExoMechManagement.CurrentAresPhase >= 6)
                    shootTime -= 30;

                if (AresBodyBehaviorOverride.Enraged)
                    shootTime /= 3;

                return shootTime;
            }
        }

        public static int TotalFlamesPerBurst
        {
            get
            {
                int flamesPerBurst = 2;

                if (ExoMechManagement.CurrentAresPhase >= 5)
                    flamesPerBurst += 2;

                if (ExoMechManagement.CurrentAresPhase >= 6)
                    flamesPerBurst += 2;

                if (AresBodyBehaviorOverride.Enraged)
                    flamesPerBurst += 7;
                
                return flamesPerBurst;
            }
        }

        public override int ShootRate => ShootTime / TotalFlamesPerBurst;

        public override SoundStyle ShootSound => InfernumSoundRegistry.SafeLoadCalamitySound("Sounds/Custom/ExoMechs/ExoPlasmaShoot", PlasmaCaster.FireSound);

        public override SoundStyle FireTelegraphSound => AresPlasmaFlamethrower.TelSound;

        public override Color TelegraphBackglowColor => Color.Lime;

        public override void CreateDustTelegraphs(NPC npc, Vector2 endOfCannon)
        {
            Vector2 dustSpawnPosition = endOfCannon + Main.rand.NextVector2Circular(45f, 45f);
            Dust plasma = Dust.NewDustPerfect(dustSpawnPosition, 107);
            plasma.velocity = (endOfCannon - plasma.position) * 0.04f;
            plasma.scale = 1.25f;
            plasma.noGravity = true;
        }

        public override void ShootProjectiles(NPC npc, Vector2 endOfCannon, Vector2 aimDirection)
        {
            int plasmaFireballCount = 1;
            int plasmaDamage = AresBodyBehaviorOverride.ProjectileDamageBoost + DraedonBehaviorOverride.StrongerNormalShotDamage;
            bool gasExplosionVariant = ExoMechManagement.CurrentAresPhase >= 2;
            float plasmaShootSpeed = 10.6f;

            // Make things in general stronger based on Ares' current phase.
            if (ExoMechManagement.CurrentAresPhase >= 3)
            {
                plasmaFireballCount++;
                plasmaShootSpeed *= 1.2f;
            }

            // Fire the plasma.
            for (int i = 0; i < plasmaFireballCount; i++)
            {
                Vector2 flameShootVelocity = aimDirection * plasmaShootSpeed;
                int fireballType = ModContent.ProjectileType<AresPlasmaFireball>();

                // Add some randomness to the shoot velocity if multiple fireballs are about to be shot.
                if (plasmaFireballCount >= 2)
                    flameShootVelocity = flameShootVelocity.RotatedByRandom(0.34f);
                if (i >= 1)
                    flameShootVelocity *= Main.rand.NextFloat(0.6f, 0.9f);

                int plasmaFireball = Utilities.NewProjectileBetter(endOfCannon, flameShootVelocity, fireballType, plasmaDamage, 0f);
                if (Main.projectile.IndexInRange(plasmaFireball))
                    Main.projectile[plasmaFireball].ModProjectile<AresPlasmaFireball>().GasExplosionVariant = gasExplosionVariant;
            }
        }

        public override ref SlotId GetSoundSlot(NPC npc) => ref npc.ModNPC<AresPlasmaFlamethrower>().TelegraphSoundSlot;

        public override Vector2 GetHoverOffset(NPC npc, bool performingCharge)
        {
            if (performingCharge)
                return new(250f, 150f);

            return new(375f, 100f);
        }

        public override AresCannonChargeParticleSet GetEnergyDrawer(NPC npc) => npc.ModNPC<AresPlasmaFlamethrower>().EnergyDrawer;

        public override Vector2 GetCoreSpritePosition(NPC npc) => npc.ModNPC<AresPlasmaFlamethrower>().CoreSpritePosition;
    }
}
