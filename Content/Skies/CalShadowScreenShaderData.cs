using CalamityMod.NPCs.CalClone;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.Content.Skies
{
    public class CalShadowScreenShaderData(string passName) : ScreenShaderData(passName)
    {
        private int CalCloneIndex;

        private void UpdatePIndex()
        {
            int cloneID = ModContent.NPCType<CalamitasClone>();
            if (CalCloneIndex >= 0 && Main.npc[CalCloneIndex].active && Main.npc[CalCloneIndex].type == cloneID)
                return;

            CalCloneIndex = -1;
            foreach (NPC n in Main.ActiveNPCs)
            {
                if (n.type == cloneID)
                {
                    CalCloneIndex = n.whoAmI;
                    break;
                }
            }
        }

        public override void Apply()
        {
            UpdatePIndex();
            if (CalCloneIndex != -1)
            {
                UseTargetPosition(Main.npc[CalCloneIndex].Center);
            }
            UseColor(Color.Transparent);
            base.Apply();
        }
    }
}
