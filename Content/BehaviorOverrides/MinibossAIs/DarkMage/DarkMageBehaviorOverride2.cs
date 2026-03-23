using System;
using CalamityMod;
using InfernumMode.Content.Items.Relics;
using InfernumMode.Core.OverridingSystem;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.MinibossAIs.DarkMage
{
    public class DarkMageBehaviorOverride2 : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.DD2DarkMageT3;

        public override bool PreAI(NPC npc) => DarkMageBehaviorOverride.DoAI(npc);

        #region Death Effects

        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<DarkMageRelic>());
        }

        #endregion Death Effects

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frame.Width = 80;
            npc.frame.Height = 80;
            npc.frame.Y = (int)Math.Round(npc.localAI[0]);
        }
    }
}
