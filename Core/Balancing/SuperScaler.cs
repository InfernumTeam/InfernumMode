using System;
////using Fargowiltas.Items.Vanity;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Core.Balancing
{
    public class SuperScaler : GlobalNPC
    {

        public bool FirstFrame = true;

        public override bool InstancePerEntity => true;

        public override bool PreAI(NPC npc)
        {
            if (FirstFrame && InfernumConfig.Instance.SuperScaler)
            {
                FirstFrame = false;
                if (!npc.townNPC && !npc.CountsAsACritter && npc.life > 10)
                {
                    float lifeFraction = npc.GetLifePercent();
					if (npc.boss)
					{                    
						npc.lifeMax = (int)Math.Round(npc.lifeMax * 60.0);
						npc.life = (int)Math.Round(npc.lifeMax * lifeFraction);
					}
					else if (npc.type != NPCID.GolemHead && npc.type != NPCID.GolemHeadFree)
                    {
						npc.lifeMax = (int)Math.Round(npc.lifeMax * 2.5);
						npc.life = (int)Math.Round(npc.lifeMax * lifeFraction);
					}

                }
            }
            return true;
        }
    }
}
