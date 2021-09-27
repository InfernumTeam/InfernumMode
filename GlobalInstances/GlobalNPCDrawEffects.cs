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

        #region Manual Drawing
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color drawColor)
        {
            if (InfernumMode.CanUseCustomAIs)
            {
                if (OverridingListManager.InfernumPreDrawOverrideList.ContainsKey(npc.type))
                    return (bool)OverridingListManager.InfernumPreDrawOverrideList[npc.type].DynamicInvoke(npc, spriteBatch, drawColor);
            }
            return base.PreDraw(npc, spriteBatch, drawColor);
        }
        #endregion

        #region Healthbar Manipulation
        public override bool? DrawHealthBar(NPC npc, byte hbPosition, ref float scale, ref Vector2 position)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return base.DrawHealthBar(npc, hbPosition, ref scale, ref position);

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