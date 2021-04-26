using CalamityMod.NPCs;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;

namespace InfernumMode.FuckYouModeAIs.Prime
{
	public class PrimeAIClass
    {
        internal static bool CanArmCurrentlyBeActive(NPC arm)
		{
            NPC head = Main.npc[(int)arm.ai[0]];
            ref float activeArmCycle = ref head.ai[1];

            List<int> armCycleList = new List<int>();
            if (CalamityGlobalNPC.primeCannon != -1)
                armCycleList.Add(NPCID.PrimeCannon);
            if (CalamityGlobalNPC.primeLaser != -1)
                armCycleList.Add(NPCID.PrimeLaser);
            if (CalamityGlobalNPC.primeSaw != -1)
                armCycleList.Add(NPCID.PrimeSaw);
            if (CalamityGlobalNPC.primeVice != -1)
                armCycleList.Add(NPCID.PrimeVice);

            // If only two remaing arms can be active, all of them should be.
            if (armCycleList.Count <= 2)
                return true;

            int lastType = armCycleList.Last();

            // Go through the cycle by taking the last index and re-adding it to the front of the list.
            for (int i = 0; i < activeArmCycle; i++)
            {
                armCycleList.Remove(lastType);
                armCycleList.Insert(0, lastType);
            }

            int armTypeIndex = armCycleList.IndexOf(arm.type);

            // Otherwise, if there are three or more arms, check if the type of the npc is in the first 2 indices, after cycling.
            return armTypeIndex != -1 && armTypeIndex < 2;
        }

        internal static void DoArmFlyMovement(NPC npc, Vector2 destination, Vector2 acceleration, Vector2 maxVelocity)
        {
            if (npc.position.Y > destination.Y)
            {
                if (npc.velocity.Y > 0f)
                    npc.velocity.Y *= 0.96f;

                npc.velocity.Y -= acceleration.Y;
            }
            else if (npc.position.Y < destination.Y)
            {
                if (npc.velocity.Y < 0f)
                    npc.velocity.Y *= 0.96f;

                npc.velocity.Y += acceleration.Y;
            }

            if (npc.Center.X > destination.X)
            {
                if (npc.velocity.X > 0f)
                    npc.velocity.X *= 0.96f;

                npc.velocity.X -= acceleration.X;
            }
            if (npc.Center.X < destination.X)
            {
                if (npc.velocity.X < 0f)
                    npc.velocity.X *= 0.96f;

                npc.velocity.X += acceleration.X;
            }

            npc.velocity = Vector2.Clamp(npc.velocity, -maxVelocity, maxVelocity);
        }

        [OverrideAppliesTo(NPCID.SkeletronPrime, typeof(PrimeAIClass), "SkeletronPrimeAI", EntityOverrideContext.NPCAI, true)]
        public static bool SkeletronPrimeAI(NPC npc)
        {
            // Percent life remaining
            float lifeRatio = npc.life / (float)npc.lifeMax;

            ref float spawnedArms01Flag = ref npc.ai[0];
            ref float activeArmCycle = ref npc.ai[1];
            ref float activeArmCycleTimer = ref npc.ai[2];
            ref float damageDelay = ref npc.ai[3];

            // Spawn arms.
            if (Main.netMode != NetmodeID.MultiplayerClient && spawnedArms01Flag == 0f)
            {
                npc.TargetClosest();
                spawnedArms01Flag = 1f;

                int arm = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.PrimeCannon, npc.whoAmI);
                Main.npc[arm].ai[0] = npc.whoAmI;
                Main.npc[arm].target = npc.target;
                Main.npc[arm].netUpdate = true;

                arm = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.PrimeSaw, npc.whoAmI);
                Main.npc[arm].ai[0] = npc.whoAmI;
                Main.npc[arm].target = npc.target;
                Main.npc[arm].netUpdate = true;

                arm = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.PrimeVice, npc.whoAmI);
                Main.npc[arm].ai[0] = npc.whoAmI;
                Main.npc[arm].target = npc.target;
                Main.npc[arm].netUpdate = true;

                arm = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.PrimeLaser, npc.whoAmI);
                Main.npc[arm].ai[0] = npc.whoAmI;
                Main.npc[arm].target = npc.target;
                Main.npc[arm].netUpdate = true;
            }

            Player target = Main.player[npc.target];

            // Shift the active arms.
            activeArmCycleTimer++;
            if (activeArmCycleTimer >= 900)
			{
                activeArmCycle = (activeArmCycle + 1) % 4;
                activeArmCycleTimer = 0f;
                npc.netUpdate = true;
            }

            // Check if arms are alive.
            bool cannonAlive = false;
            bool laserAlive = false;
            bool viceAlive = false;
            bool sawAlive = false;
            if (CalamityGlobalNPC.primeCannon != -1)
            {
                if (Main.npc[CalamityGlobalNPC.primeCannon].active)
                    cannonAlive = true;
            }
            if (CalamityGlobalNPC.primeLaser != -1)
            {
                if (Main.npc[CalamityGlobalNPC.primeLaser].active)
                    laserAlive = true;
            }
            if (CalamityGlobalNPC.primeVice != -1)
            {
                if (Main.npc[CalamityGlobalNPC.primeVice].active)
                    viceAlive = true;
            }
            if (CalamityGlobalNPC.primeSaw != -1)
            {
                if (Main.npc[CalamityGlobalNPC.primeSaw].active)
                    sawAlive = true;
            }
            bool allArmsDead = !cannonAlive && !laserAlive && !viceAlive && !sawAlive;
            npc.chaseable = allArmsDead;

            // Inflict 0 damage for 3 seconds after spawning.
            if (damageDelay < 180f)
            {
                damageDelay++;
                npc.damage = 0;
            }

            // Set stats
            if (npc.ai[1] == 5f)
                npc.damage = 0;
            else if (allArmsDead)
                npc.damage = npc.defDamage;

            Vector2 destination = target.Center - Vector2.UnitY * 400f;
            npc.SimpleFlyMovement(npc.DirectionTo(destination) * new Vector2(11f, 17f), 0.4f);
            npc.rotation = npc.velocity.X / 15f;

            return false;
		}

        [OverrideAppliesTo(NPCID.PrimeLaser, typeof(PrimeAIClass), "PrimeLaserAI", EntityOverrideContext.NPCAI)]
        public static bool PrimeLaserAI(NPC npc)
        {
            // Despawn if head is gone.
            if (!Main.npc.IndexInRange((int)npc.ai[0]) || !Main.npc[(int)npc.ai[0]].active || Main.npc[(int)npc.ai[0]].type != NPCID.SkeletronPrime)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    npc.life = -1;
                    npc.HitEffect();
                    npc.active = false;
                    npc.netUpdate = true;
                }
            }

            npc.spriteDirection = -1;
            NPC primeHead = Main.npc[(int)npc.ai[0]];
            npc.target = primeHead.target;
            npc.damage = primeHead.damage == 0 ? 0 : npc.defDamage;

            bool inactive = !CanArmCurrentlyBeActive(npc);
            Player target = Main.player[npc.target];

            Vector2 destination = primeHead.Center + new Vector2(180f, -100f);

            if (!inactive && !primeHead.WithinRange(target.Center, 480f))
                destination += primeHead.DirectionTo(target.Center) * (npc.Distance(target.Center) - 480f);

            DoArmFlyMovement(npc, destination, new Vector2(0.15f), new Vector2(8f));

            // Idly dangle around if inactive.
            if (inactive && true)
            {
                float idealRotation = -npc.velocity.X / 6f;
                npc.rotation = npc.rotation.AngleLerp(idealRotation - MathHelper.PiOver2, 0.125f);
                npc.rotation = MathHelper.Clamp(npc.rotation, MathHelper.ToRadians(-72f), MathHelper.ToRadians(72f));
                return false;
            }

            return false;
		}
    }
}
