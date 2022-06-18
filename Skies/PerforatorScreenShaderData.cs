using CalamityMod.NPCs.Perforator;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.Skies
{
    public class PerforatorScreenShaderData : ScreenShaderData
    {
        private int HiveIndex;

        public PerforatorScreenShaderData(string passName)
            : base(passName)
        {
        }

        private void UpdatePIndex()
        {
            int ProvType = ModContent.NPCType<PerforatorHive>();
            if (HiveIndex >= 0 && Main.npc[HiveIndex].active && Main.npc[HiveIndex].type == ProvType)
            {
                return;
            }
            HiveIndex = -1;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == ProvType)
                {
                    HiveIndex = i;
                    break;
                }
            }
        }

        public override void Apply()
        {
            UpdatePIndex();
            if (HiveIndex != -1)
                UseTargetPosition(Main.npc[HiveIndex].Center);
            else
                Filters.Scene["InfernumMode:Perforators"].Deactivate();
            base.Apply();
        }
    }
}
