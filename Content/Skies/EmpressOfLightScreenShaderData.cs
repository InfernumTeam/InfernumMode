using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;

namespace InfernumMode.Content.Skies
{
    public class EmpressOfLightScreenShaderData : ScreenShaderData
    {
        public int FairyIndex;

        public EmpressOfLightScreenShaderData(Effect shader, string pass) : base(new Ref<Effect>(shader), pass)
        {
        }

        private void UpdatePIndex()
        {
            int type = NPCID.HallowBoss;
            if (FairyIndex >= 0 && Main.npc[FairyIndex].active && Main.npc[FairyIndex].type == type)
                return;
            FairyIndex = -1;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == type)
                {
                    FairyIndex = i;
                    break;
                }
            }
        }

        public override void Apply()
        {
            UpdatePIndex();
            if (FairyIndex != -1)
                UseTargetPosition(Main.npc[FairyIndex].Center);
            base.Apply();
        }
    }
}
