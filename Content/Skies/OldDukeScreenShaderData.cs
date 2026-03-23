using CalamityMod.NPCs.OldDuke;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.Content.Skies
{
    public class OldDukeScreenShaderData(string passName) : ScreenShaderData(passName)
    {
        private int OldDukeIndex;

        private void UpdatePIndex()
        {
            int oldDukeType = ModContent.NPCType<OldDuke>();
            if (OldDukeIndex >= 0 && Main.npc[OldDukeIndex].active && Main.npc[OldDukeIndex].type == oldDukeType)
                return;

            OldDukeIndex = -1;
            foreach (NPC n in Main.ActiveNPCs)
            {
                if (n.type == oldDukeType && n.Infernum().ExtraAI[6] >= 2f)
                {
                    OldDukeIndex = n.whoAmI;
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
            UseColor(Color.Lerp(Color.AliceBlue, Color.Black, 0.6f));
            base.Apply();
        }
    }
}
