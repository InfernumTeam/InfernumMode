using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.Content.Skies
{
    public class DragonfollyScreenShaderData(string passName) : ScreenShaderData(passName)
    {
        public int BirdbrainIndex;

        private void UpdatePIndex()
        {
            int type = ModContent.NPCType<CalamityMod.NPCs.Bumblebirb.Dragonfolly>();
            if (BirdbrainIndex >= 0 && Main.npc[BirdbrainIndex].active && Main.npc[BirdbrainIndex].type == type)
                return;
            BirdbrainIndex = -1;
            foreach (NPC n in Main.ActiveNPCs)
            {
                if (n.type == type)
                {
                    BirdbrainIndex = n.whoAmI;
                    break;
                }
            }
        }

        public override void Apply()
        {
            UpdatePIndex();
            if (BirdbrainIndex != -1)
                UseTargetPosition(Main.npc[BirdbrainIndex].Center);
            base.Apply();
        }
    }
}
