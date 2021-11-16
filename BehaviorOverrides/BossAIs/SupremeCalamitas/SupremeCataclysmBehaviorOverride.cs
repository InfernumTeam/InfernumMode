using CalamityMod.NPCs;
using CalamityMod.NPCs.SupremeCalamitas;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SupremeCataclysmBehaviorOverride : NPCBehaviorOverride
    {
        public enum SupremeCataclysmAttackState
        {
            PunchTarget,
            FlameBlasts,
            SinusoidalDarkMagicFlames
        }

        public override int NPCOverrideType => ModContent.NPCType<SupremeCataclysm>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            // Disappear if Supreme Calamitas is not present.
            if (CalamityGlobalNPC.SCal == -1)
            {
                npc.active = false;
                return false;
            }

            npc.target = Main.npc[CalamityGlobalNPC.SCal].target;
            npc.defDamage = 600;
            Player target = Main.player[npc.target];
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float attackDelay = ref npc.Infernum().ExtraAI[0];

            if (attackDelay < 60f)
            {
                npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;
                attackDelay++;
            }

            CalamityGlobalNPC.SCalCataclysm = npc.whoAmI;

            bool alone = !NPC.AnyNPCs(ModContent.NPCType<SupremeCatastrophe>());
            switch ((SupremeCataclysmAttackState)attackState)
            {
                case SupremeCataclysmAttackState.PunchTarget:
                    SupremeCatastropheBehaviorOverride.DoBehavior_SliceTarget(npc, target, alone, ref attackTimer);
                    break;
                case SupremeCataclysmAttackState.FlameBlasts:
                    SupremeCatastropheBehaviorOverride.DoBehavior_FlameBlasts(npc, target, alone, ref attackTimer);
                    break;
                case SupremeCataclysmAttackState.SinusoidalDarkMagicFlames:
                    SupremeCatastropheBehaviorOverride.DoBehavior_SinusoidalDarkMagicFlames(npc, target, alone, ref attackTimer);
                    break;
            }
            attackTimer++;
            return false;
        }
    }
}
