using CalamityMod.NPCs.CalClone;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.Content.Skies
{
    public class CalCloneScreenShaderData : ScreenShaderData
    {
        private int CalCloneIndex;

        public CalCloneScreenShaderData(string passName)
            : base(passName)
        {
        }

        private void UpdatePIndex()
        {
            int cloneID = ModContent.NPCType<CalamitasClone>();
            if (CalCloneIndex >= 0 && Main.npc[CalCloneIndex].active && Main.npc[CalCloneIndex].type == cloneID)
                return;

            CalCloneIndex = -1;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == cloneID)
                {
                    CalCloneIndex = i;
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
