using CalamityMod.NPCs.AstrumDeus;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.Content.Skies
{
    public class DeusScreenShaderData(string passName) : ScreenShaderData(passName)
    {
        private int BossIndex;

        private void UpdatePIndex()
        {
            int bossType = ModContent.NPCType<AstrumDeusHead>();
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
            {
                UseTargetPosition(Main.npc[BossIndex].Center);
                UseOpacity(Lerp(0.15f, 0.8f, Main.npc[BossIndex].Infernum().ExtraAI[6]));

                Color endColor = Color.Lerp(new Color(237, 93, 83), new Color(109, 242, 196), Cos(Main.GlobalTimeWrappedHourly * 1.7f) * 0.5f + 0.5f);
                UseColor(Color.Lerp(Color.Lerp(Color.Purple, Color.Black, 0.75f), endColor, Main.npc[BossIndex].Infernum().ExtraAI[6]));
            }

            base.Apply();
        }
    }
}
