using CalamityMod.NPCs;
using CalamityMod.NPCs.SupremeCalamitas;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
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

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame;

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

            npc.localAI[0] = 150f;
            if (attackDelay < 60f)
            {
                npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;
                npc.localAI[0] = 0f;
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


        public override void FindFrame(NPC npc, int frameHeight)
        {
            int currentFrame = 0;
            float frameUpdateSpeed = npc.ai[0] == (int)SupremeCatastropheBehaviorOverride.SupremeCatastropheAttackState.SliceTarget ? 260f : 130f;
            float punchCounter = Main.GlobalTimeWrappedHourly * frameUpdateSpeed % 120f;
            float punchInterpolant = Utils.GetLerpValue(0f, 120f, punchCounter, true);
            if (npc.localAI[0] < 120f)
            {
                npc.frameCounter += 0.15f;
                if (npc.frameCounter >= 1f)
                    currentFrame = (currentFrame + 1) % 12;
            }
            else
                currentFrame = (int)Math.Round(MathHelper.Lerp(12f, 21f, punchInterpolant));

            int xFrame = currentFrame / Main.npcFrameCount[npc.type];
            int yFrame = currentFrame % Main.npcFrameCount[npc.type];

            npc.frame.Width = 212;
            npc.frame.Height = 208;
            npc.frame.X = xFrame * npc.frame.Width;
            npc.frame.Y = yFrame * npc.frame.Height;
        }
    }
}
