using Terraria;
using Terraria.Graphics.Shaders;

namespace InfernumMode.Skies
{
    public class HiveMindScreenShaderData : ScreenShaderData
    {
        private int ProvIndex;

        public HiveMindScreenShaderData(string passName)
            : base(passName)
        {
        }

        private void UpdatePIndex()
        {
            int ProvType = InfernumMode.CalamityMod.NPCType("HiveMindP2");
            if (ProvIndex >= 0 && Main.npc[ProvIndex].active && Main.npc[ProvIndex].type == ProvType)
            {
                return;
            }
            ProvIndex = -1;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == ProvType)
                {
                    ProvIndex = i;
                    break;
                }
            }
        }

        public override void Apply()
        {
            UpdatePIndex();
            if (ProvIndex != -1)
            {
                UseTargetPosition(Main.npc[ProvIndex].Center);
            }
            base.Apply();
        }
    }
}
