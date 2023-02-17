using CalamityMod.Events;
using CalamityMod.NPCs.ExoMechs;
using InfernumMode.Content.BehaviorOverrides.BossAIs.MoonLord;
using InfernumMode.Core.GlobalInstances.Systems;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Core.CrossCompatibility
{
    public class InfernumModCalls
    {
        public static object Call(params object[] args)
        {
            if (args is null || args.Length <= 0)
                return new ArgumentException("ERROR: No function name specified. First argument must be a function name.");
            if (args[0] is not string)
                return new ArgumentException("ERROR: First argument must be a string function name.");

            string methodName = args[0].ToString();
            switch (methodName)
            {
                case "GetInfernumActive":
                    return WorldSaveSystem.InfernumMode;
                case "SetInfernumActive":
                    WorldSaveSystem.InfernumMode = (bool)args[1];
                    break;
                case "CanPlayMusicForNPC":
                    int npcID = (int)args[1];
                    return CanPlayMusicForNPC(npcID);
            }
            return null;
        }

        public static bool CanPlayMusicForNPC(int npcID)
        {
            if (BossRushEvent.BossRushActive)
                return false;

            if (npcID == NPCID.KingSlime)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.EyeofCthulhu)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.BrainofCthulhu)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.EaterofWorldsHead)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.QueenBee)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.SkeletronHead)
                return NPC.AnyNPCs(npcID);
            if (npcID is NPCID.Retinazer or NPCID.Spazmatism or NPCID.SkeletronPrime or NPCID.TheDestroyer)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.Plantera)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.HallowBoss)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.DukeFishron)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.CultistBoss)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.MoonLordCore)
            {
                int npcIndex = NPC.FindFirstNPC(npcID);
                if (npcIndex < 0)
                    return false;
                return Main.npc[npcIndex].Infernum().ExtraAI[10] >= MoonLordCoreBehaviorOverride.IntroSoundLength;
            }
            if (npcID == ModContent.NPCType<Draedon>())
                return NPC.AnyNPCs(npcID) || InfernumMode.DraedonThemeTimer > 0;

            return false;
        }
    }
}
