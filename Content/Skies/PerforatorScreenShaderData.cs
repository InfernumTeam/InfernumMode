using CalamityMod.NPCs.Perforator;
using InfernumMode.Assets.Effects;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.Content.Skies
{
    public class PerforatorScreenShaderData(string passName) : ScreenShaderData(passName)
    {
        private int HiveIndex;

        private void UpdatePIndex()
        {
            int ProvType = ModContent.NPCType<PerforatorHive>();
            if (HiveIndex >= 0 && Main.npc[HiveIndex].active && Main.npc[HiveIndex].type == ProvType)
            {
                return;
            }
            HiveIndex = -1;
            foreach (NPC n in Main.ActiveNPCs)
            {
                if (n.type == ProvType)
                {
                    HiveIndex = n.whoAmI;
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
                InfernumEffectsRegistry.PerforatorsScreenShader.Deactivate();
            base.Apply();
        }
    }
}
