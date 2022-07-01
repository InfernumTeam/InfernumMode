using CalamityMod.NPCs.OldDuke;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.Skies
{
    public class OldDukeScreenShaderData : ScreenShaderData
    {
        private int OldDukeIndex;

        public OldDukeScreenShaderData(string passName)
            : base(passName)
        {
        }

        private void UpdatePIndex()
        {
            int oldDukeType = ModContent.NPCType<OldDuke>();
            if (OldDukeIndex >= 0 && Main.npc[OldDukeIndex].active && Main.npc[OldDukeIndex].type == oldDukeType)
                return;

            OldDukeIndex = -1;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == oldDukeType && Main.npc[i].Infernum().ExtraAI[6] >= 2f)
                {
                    OldDukeIndex = i;
                    break;
                }
            }
        }

        public override void Apply()
        {
            UpdatePIndex();
            if (OldDukeIndex != -1)
            {
                UseTargetPosition(Main.npc[OldDukeIndex].Center);
            }
            base.Apply();
        }
    }
}
