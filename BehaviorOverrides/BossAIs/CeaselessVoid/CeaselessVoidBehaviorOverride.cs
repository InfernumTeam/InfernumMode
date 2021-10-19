using CalamityMod.NPCs;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

using CeaselessVoidBoss = CalamityMod.NPCs.CeaselessVoid.CeaselessVoid;

namespace InfernumMode.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class CeaselessVoidBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<CeaselessVoidBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        #region Enumerations
        public enum CeaselessVoidAttackType
        {
            ReleaseRealityTearPortals
        }
        #endregion

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Do targeting.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            CalamityGlobalNPC.voidBoss = npc.whoAmI;

            if (!target.active || target.dead || !npc.WithinRange(target.Center, 7200f) || CalamityGlobalNPC.calamitas == -1 || !Main.npc[CalamityGlobalNPC.calamitas].active)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * 18f, 0.08f);
                if (!npc.WithinRange(target.Center, 1450f) || CalamityGlobalNPC.calamitas == -1 || !Main.npc[CalamityGlobalNPC.calamitas].active)
                {
                    npc.life = 0;
                    npc.active = false;
                    npc.netUpdate = true;
                }
                return false;
            }

            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];

            // Reset things.
            npc.damage = npc.defDamage;
            npc.dontTakeDamage = target.Center.Y < Main.worldSurface * 16f;

            switch ((CeaselessVoidAttackType)(int)attackType)
            {
                case CeaselessVoidAttackType.ReleaseRealityTearPortals:
                    DoBehavior_ReleaseTearPortals(npc, target, lifeRatio, ref attackTimer);
                    break;
            }

            attackTimer++;
            return false;
        }

        public static void DoBehavior_ReleaseTearPortals(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            float hoverSpeed = 23f;
            Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * 450f;

            // Fly to the side of the target.
            if (!npc.WithinRange(hoverDestination, 150f) || npc.WithinRange(target.Center, 200f))
			{
                Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * hoverSpeed;
                npc.SimpleFlyMovement(idealVelocity, hoverSpeed / 35f);
			}

            // Create rifts around the void.
        }

        public static void SelectNewAttack(NPC npc)
        {
            List<CeaselessVoidAttackType> possibleAttacks = new List<CeaselessVoidAttackType>
            {
                CeaselessVoidAttackType.ReleaseRealityTearPortals,
            };

            if (possibleAttacks.Count > 1)
                possibleAttacks.Remove((CeaselessVoidAttackType)(int)npc.ai[0]);

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.ai[0] = (int)Main.rand.Next(possibleAttacks);
            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }
        #endregion AI
    }
}
