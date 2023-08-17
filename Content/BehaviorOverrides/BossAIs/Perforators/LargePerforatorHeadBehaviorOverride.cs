using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Perforator;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Perforators
{
    public class LargePerforatorHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<PerforatorHeadLarge>();

        public override void SetDefaults(NPC npc)
        {
            // Set defaults that, if were to be changed by Calamity, would cause significant issues to the fight.
            npc.width = 70;
            npc.height = 84;
            npc.scale = 1f;
            npc.Opacity = 0;
            npc.defense = 4;
        }

        public override bool PreAI(NPC npc)
        {
            // Create segments.
            if (npc.localAI[3] == 0f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    PerforatorHiveBehaviorOverride.CreateSegments(npc, 17, ModContent.NPCType<PerforatorBodyLarge>(), ModContent.NPCType<PerforatorTailLarge>());

                npc.localAI[3] = 1f;
            }

            // Fuck off if the hive is dead.
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.perfHive) || !Main.npc[CalamityGlobalNPC.perfHive].active)
            {
                npc.active = false;
                return false;
            }

            npc.target = Main.npc[CalamityGlobalNPC.perfHive].target;
            Player target = Main.player[npc.target];

            // Fade in.
            npc.Opacity = Clamp(npc.Opacity + 0.1f, 0f, 1f);

            // Fly towards the target.
            float xDamp = Utils.Remap(Math.Abs(Vector2.Dot(npc.velocity.SafeNormalize(Vector2.Zero), Vector2.UnitX)), 0f, 1f, 0.2f, 1f);
            float yDamp = Utils.Remap(Math.Abs(Vector2.Dot(npc.velocity.SafeNormalize(Vector2.Zero), Vector2.UnitY)), 0f, 1f, 0.2f, 1f);
            Vector2 flyDestination = target.Center;

            float maxSpeed = BossRushEvent.BossRushActive ? 34f : 18f;
            if (npc.WithinRange(flyDestination, 270f) && npc.velocity.Length() > maxSpeed / 3f)
                npc.velocity *= 1.01f;
            else
            {
                Vector2 velocityStep = npc.SafeDirectionTo(flyDestination) * new Vector2(xDamp, yDamp) * 0.3f;
                npc.velocity = (npc.velocity + velocityStep).ClampMagnitude(0f, maxSpeed);
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.027f);
            }
            npc.rotation = npc.velocity.ToRotation() + PiOver2;

            return false;
        }
    }
}
