using CalamityMod.NPCs.AstrumDeus;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.Skies
{
    public class DeusScreenShaderData : ScreenShaderData
    {
        private int BossIndex;

        public DeusScreenShaderData(string passName) : base(passName) { }

        private void UpdatePIndex()
        {
            int bossType = ModContent.NPCType<AstrumDeusHeadSpectral>();
            if (BossIndex >= 0 && Main.npc[BossIndex].active && Main.npc[BossIndex].type == bossType)
                return;

            BossIndex = -1;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == bossType)
                {
                    BossIndex = i;
                    break;
                }
            }
        }

        public override void Apply()
        {
            UpdatePIndex();
            if (BossIndex != -1)
                UseTargetPosition(Main.npc[BossIndex].Center);

            base.Apply();
        }
    }
}
