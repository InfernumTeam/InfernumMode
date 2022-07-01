using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.Athena;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode
{
    public static partial class Utilities
    {
        public const float DefaultTargetRedecideThreshold = 4000f;
        public static void TargetClosestIfTargetIsInvalid(this NPC npc, float distanceThreshold = DefaultTargetRedecideThreshold)
        {
            bool invalidTargetIndex = npc.target < 0 || npc.target >= 255;
            if (invalidTargetIndex)
            {
                npc.TargetClosest();
                return;
            }

            Player target = Main.player[npc.target];
            bool invalidTarget = target.dead || !target.active;
            if (invalidTarget)
                npc.TargetClosest();

            if (distanceThreshold >= 0f && !npc.WithinRange(target.Center, distanceThreshold - target.aggro))
                npc.TargetClosest();
        }

        public static bool IsExoMech(NPC npc)
        {
            // Thanatos.
            if (npc.type == ModContent.NPCType<ThanatosHead>() ||
                npc.type == ModContent.NPCType<ThanatosBody1>() ||
                npc.type == ModContent.NPCType<ThanatosBody2>() ||
                npc.type == ModContent.NPCType<ThanatosTail>())
            {
                return true;
            }

            // Ares.
            if (npc.type == ModContent.NPCType<AresBody>() ||
                npc.type == ModContent.NPCType<AresLaserCannon>() ||
                npc.type == ModContent.NPCType<AresTeslaCannon>() ||
                npc.type == ModContent.NPCType<AresPlasmaFlamethrower>() ||
                npc.type == ModContent.NPCType<AresGaussNuke>() ||
                npc.type == ModContent.NPCType<AresPulseCannon>())
            {
                return true;
            }

            // Athena.
            if (npc.type == ModContent.NPCType<AthenaNPC>())
                return true;

            // Artemis and Apollo.
            if (npc.type == ModContent.NPCType<Artemis>() ||
                npc.type == ModContent.NPCType<Apollo>())
            {
                return true;
            }

            return false;
        }
    }
}
