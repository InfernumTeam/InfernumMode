using CalamityMod.NPCs;
using CalamityMod.NPCs.Cryogen;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.Leviathan;
using CalamityMod.NPCs.Signus;
using CalamityMod.NPCs.Yharon;
using InfernumMode.BehaviorOverrides.BossAIs.MoonLord;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.GlobalInstances
{
    public class GlobalNPCDrawEffects : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        #region Get Alpha
        public override Color? GetAlpha(NPC npc, Color drawColor)
        {
            // Give a dark tint to the moon lord.
            if (npc.type == NPCID.MoonLordHand || npc.type == NPCID.MoonLordHead || npc.type == NPCID.MoonLordCore)
            {
                if (InfernumMode.CanUseCustomAIs)
                    return MoonLordCoreBehaviorOverride.OverallTint;
            }
            return base.GetAlpha(npc, drawColor);
        }
        #endregion

        #region Map Icon Manipulation
        public override void BossHeadSlot(NPC npc, ref int index)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return;

            bool isDoG = npc.type == ModContent.NPCType<DevourerofGodsHead>() || npc.type == ModContent.NPCType<DevourerofGodsBody>() || npc.type == ModContent.NPCType<DevourerofGodsTail>();
            if (isDoG)
            {
                NPC head = CalamityGlobalNPC.DoGHead >= 0 ? Main.npc[CalamityGlobalNPC.DoGHead] : null;
                if (npc.Opacity < 0.1f || (head != null && head.Infernum().ExtraAI[2] >= 6f && head.Infernum().ExtraAI[33] >= 1f))
                    index = -1;
            }

            // Make Anahita completely invisible on the map when sufficiently faded out.
            if (npc.type == ModContent.NPCType<Siren>() && npc.Opacity < 0.1f)
                index = -1;

            // Make Signus completely invisible on the map.
            if (npc.type == ModContent.NPCType<Signus>())
                index = -1;

            // Prevent Yharon from showing himself amongst his illusions in Subphase 10.
            if (npc.type == ModContent.NPCType<Yharon>())
            {
                if (npc.life / (float)npc.lifeMax <= 0.05f && npc.Infernum().ExtraAI[2] == 1f)
                    index = -1;
            }

            // Have Cryogen use a custom map icon.
            if (npc.type == ModContent.NPCType<Cryogen>())
                index = ModContent.GetModBossHeadSlot("InfernumMode/BehaviorOverrides/BossAIs/Cryogen/CryogenMapIcon");
        }
        #endregion

        #region Manual Drawing
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color drawColor)
        {
            if (InfernumMode.CanUseCustomAIs)
            {
                bool isDoG = npc.type == ModContent.NPCType<DevourerofGodsHead>() || npc.type == ModContent.NPCType<DevourerofGodsBody>() || npc.type == ModContent.NPCType<DevourerofGodsTail>();
                if (isDoG && npc.alpha >= 252)
                    return false;

                if (OverridingListManager.InfernumPreDrawOverrideList.ContainsKey(npc.type))
                    return OverridingListManager.InfernumPreDrawOverrideList[npc.type].Invoke(npc, spriteBatch, drawColor);
            }
            return base.PreDraw(npc, spriteBatch, drawColor);
        }
        #endregion

        #region Healthbar Manipulation
        public override bool? DrawHealthBar(NPC npc, byte hbPosition, ref float scale, ref Vector2 position)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return base.DrawHealthBar(npc, hbPosition, ref scale, ref position);

            bool isDoG = npc.type == ModContent.NPCType<DevourerofGodsHead>() || npc.type == ModContent.NPCType<DevourerofGodsBody>() || npc.type == ModContent.NPCType<DevourerofGodsTail>();
            if (isDoG && npc.alpha >= 252)
                return false;

            if (npc.type == NPCID.EaterofWorldsBody)
                return false;

            return base.DrawHealthBar(npc, hbPosition, ref scale, ref position);
        }

        #endregion

        #region Frame Manipulation
        public override void FindFrame(NPC npc, int frameHeight)
        {
            if (OverridingListManager.InfernumFrameOverrideList.ContainsKey(npc.type) && InfernumMode.CanUseCustomAIs)
                OverridingListManager.InfernumFrameOverrideList[npc.type].DynamicInvoke(npc, frameHeight);
        }
        #endregion
    }
}