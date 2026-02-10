using System;
using CalamityMod.Events;
using CalamityMod;
using CalamityMod.NPCs.Crabulon;
using CalamityMod.World;
using InfernumMode.Core.OverridingSystem;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Crabulon
{
    public class CrabShroomBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<CrabShroom>();

        public override void SetDefaults(NPC npc)
        {
            // Set defaults that, if were to be changed by Calamity, would cause significant issues to the fight.
            npc.width = 14;
            npc.height = 14;
            npc.scale = 1f;
            npc.Opacity = 1f;

            // revert cal buff
            npc.lifeMax = 15;
            // revert cal 2.1 nerf
            if (npc.damage < 31)
                npc.damage = 31;
            if (BossRushEvent.BossRushActive)
                npc.lifeMax = 8000;

            double HPBoost = CalamityServerConfig.Instance.BossHealthBoost * 0.01;
            npc.lifeMax += (int)(npc.lifeMax * HPBoost);
        }

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
