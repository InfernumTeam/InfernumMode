using InfernumMode.OverridingSystem;
using InfernumMode.Sounds;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.BehaviorOverrides.BossAIs.Prime.PrimeHeadBehaviorOverride;

namespace InfernumMode.BehaviorOverrides.BossAIs.Prime
{
    public class PrimeCannonBehaviorOverride : PrimeHandBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.PrimeCannon;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public override float PredictivenessFactor => 20f;

        public override Color TelegraphColor => Color.Orange;

        public static SoundStyle MissileShootSound => InfernumSoundRegistry.SafeLoadCalamitySound("Sounds/Custom/ExoMechs/ApolloMissileLaunch", SoundID.Item36);

        public override void PerformTelegraphBehaviors(NPC npc, PrimeAttackType attackState, float telegraphIntensity, Vector2 cannonDirection)
        {
            Vector2 endOfCannon = npc.Center + cannonDirection * npc.width * npc.scale * 0.42f;
            for (int i = 0; i < 3; i++)
            {
                if (Main.rand.NextFloat() >= telegraphIntensity)
                    continue;

                Dust fire = Dust.NewDustPerfect(endOfCannon + Main.rand.NextVector2Circular(4f, 4f), Main.rand.NextBool(3) ? 6 : 31);
                fire.noGravity = Main.rand.NextBool();
                fire.velocity = cannonDirection.RotatedByRandom(0.54f) * Main.rand.NextFloat(1f, 4f);
                fire.scale = 1.25f;
            }
        }

        public override void PerformAttackBehaviors(NPC npc, PrimeAttackType attackState, Player target, float attackTimer, bool pissed, Vector2 cannonDirection)
        {
            int shootRate = 26;
            float missileSpeed = 13.5f;

			if (npc.life < npc.lifeMax * Phase2LifeRatio && !pissed)
			{
                shootRate -= 4;
				missileSpeed += 3f;
			}

			if (pissed)
            {
                shootRate -= 9;
                missileSpeed += 7f;
            }

            // Release missiles.
            if (attackTimer % shootRate == shootRate - 1f)
            {
                SoundEngine.PlaySound(MissileShootSound with { Volume = 1.4f }, npc.Center);

                Utilities.CreateFireExplosion(npc.TopLeft + cannonDirection * 60f, npc.Size, cannonDirection * 5f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center + cannonDirection * npc.width * npc.scale * 0.4f, cannonDirection * missileSpeed, ModContent.ProjectileType<PrimeMissile>(), 140, 0f);
            }
        }
    }
}