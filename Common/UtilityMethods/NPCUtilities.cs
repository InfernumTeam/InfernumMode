using CalamityMod;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares;
using InfernumMode.Core.OverridingSystem;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode
{
    public static partial class Utilities
    {
        public const float DefaultTargetRedecideThreshold = 4000f;

        public static void TargetClosestIfTargetIsInvalid(this NPC npc, float distanceThreshold = DefaultTargetRedecideThreshold)
        {
            bool invalidTargetIndex = npc.target is < 0 or >= 255;
            if (invalidTargetIndex)
            {
                npc.TargetClosest();
                return;
            }

            Player target = Main.player[npc.target];
            bool invalidTarget = target.dead || !target.active || target.Infernum().GetValue<int>("EelSwallowIndex") >= 0;
            if (invalidTarget)
                npc.TargetClosest();

            if (distanceThreshold >= 0f && !npc.WithinRange(target.Center, distanceThreshold - target.aggro))
                npc.TargetClosest();
        }


        public static NPC CurrentlyFoughtBoss
        {
            get
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].IsABoss())
                        return Main.npc[i].realLife >= 0 ? Main.npc[Main.npc[i].realLife] : Main.npc[i];
                }
                return null;
            }
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
                npc.type == ModContent.NPCType<AresPulseCannon>() ||
                npc.type == ModContent.NPCType<AresEnergyKatana>())
            {
                return true;
            }

            // Artemis and Apollo.
            if (npc.type == ModContent.NPCType<Artemis>() ||
                npc.type == ModContent.NPCType<Apollo>())
            {
                return true;
            }

            return false;
        }

        public static string GetNPCNameFromID(int id)
        {
            if (id < NPCID.Count)
                return id.ToString();

            return NPCLoader.GetNPC(id).FullName;
        }

        public static string GetNPCFullNameFromID(int id)
        {
            if (id < NPCID.Count)
                return NPC.GetFullnameByID(id);

            return NPCLoader.GetNPC(id).DisplayName.Value;
        }

        public static int GetNPCIDFromName(string name)
        {
            if (int.TryParse(name, out int id))
                return id;

            string[] splitName = name.Split('/');
            if (ModContent.TryFind(splitName[0], splitName[1], out ModNPC modNpc))
                return modNpc.Type;

            return NPCID.None;
        }

        public static T BehaviorOverride<T>(this NPC npc) where T : NPCBehaviorOverride
        {
            if (NPCBehaviorOverride.BehaviorOverrides.TryGetValue(npc.type, out NPCBehaviorOverride b) && b is T t)
                return t;

            return null;
        }
    }
}
