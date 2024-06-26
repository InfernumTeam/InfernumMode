﻿using CalamityMod.NPCs.HiveMind;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.Content.Skies
{
    public class HiveMindScreenShaderData(string passName) : ScreenShaderData(passName)
    {
        private int ProvIndex;

        private void UpdatePIndex()
        {
            int ProvType = ModContent.NPCType<HiveMind>();
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
