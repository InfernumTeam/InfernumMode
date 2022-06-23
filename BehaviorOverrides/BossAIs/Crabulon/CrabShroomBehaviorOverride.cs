using CalamityMod.NPCs.Crabulon;
using InfernumMode.OverridingSystem;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Crabulon
{
    public class CrabShroomBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<CrabShroom>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            Lighting.AddLight((int)(npc.Center.X / 16f), (int)(npc.Center.Y / 16f), 0f, 0.2f, 0.4f);

            npc.TargetClosest();
            Player target = Main.player[npc.target];

            float xVelocityLimit = 7.75f;
            float yVelocityLimit = 1.25f;
            npc.velocity.Y += 0.02f;
            if (npc.velocity.Y > yVelocityLimit)
                npc.velocity.Y = yVelocityLimit;

            if (npc.Right.X < target.position.X)
            {
                if (npc.velocity.X < 0f)
                    npc.velocity.X *= 0.98f;
                npc.velocity.X += 0.1f;
            }
            else if (npc.position.X > target.Right.X)
            {
                if (npc.velocity.X > 0f)
                    npc.velocity.X *= 0.98f;

                npc.velocity.X -= 0.1f;
            }
            if (Math.Abs(npc.velocity.X) > xVelocityLimit)
                npc.velocity.X *= 0.97f;

            npc.rotation = npc.velocity.X * 0.1f;
            return false;
        }
    }
}
