using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Calamitas;
using CalamityMod.World;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.CalamitasClone
{
    public class LifeSeekerBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<LifeSeeker>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            if (CalamityGlobalNPC.calamitas == -1 || !Main.npc[CalamityGlobalNPC.calamitas].active)
            {
                npc.life = 0;
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            NPC calamitas = Main.npc[CalamityGlobalNPC.calamitas];
            bool inFinalPhase = calamitas.ai[2] == 3f;

            if (inFinalPhase)
			{
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(calamitas.Center) * 18f, 0.1f);
                npc.rotation = npc.velocity.ToRotation() + MathHelper.Pi;
                npc.spriteDirection = -1;
                if (npc.Hitbox.Intersects(calamitas.Hitbox))
				{
                    int healQuantity = calamitas.lifeMax / 50;
                    calamitas.life += healQuantity;
                    calamitas.HealEffect(healQuantity);
                    npc.active = false;
                }
                return false;
			}
            return true;
		}
    }
}
