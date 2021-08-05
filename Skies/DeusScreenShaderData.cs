using CalamityMod.NPCs.AstrumDeus;
using Microsoft.Xna.Framework;
using System;
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
            {
                UseTargetPosition(Main.npc[BossIndex].Center);
                UseOpacity(MathHelper.Lerp(0.15f, 0.8f, Main.npc[BossIndex].Infernum().ExtraAI[6]));

                Color endColor = Color.Lerp(new Color(237, 93, 83), new Color(109, 242, 196), (float)Math.Cos(Main.GlobalTime * 1.7f) * 0.5f + 0.5f);
                UseColor(Color.Lerp(Color.Lerp(Color.Purple, Color.Black, 0.75f), endColor, Main.npc[BossIndex].Infernum().ExtraAI[6]));
            }

            base.Apply();
        }
    }
}
