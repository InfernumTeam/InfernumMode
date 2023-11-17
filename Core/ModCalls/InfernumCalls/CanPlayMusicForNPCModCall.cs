using System;
using System.Collections.Generic;
using CalamityMod.Events;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.NPCs.PrimordialWyrm;
using CalamityMod.NPCs.SunkenSea;
using CalamityMod.NPCs.SupremeCalamitas;
using InfernumMode.Content.BehaviorOverrides.BossAIs.MoonLord;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;

namespace InfernumMode.Core.ModCalls.InfernumCalls
{
    public class CanPlayMusicForNPCModCall : ReturnValueModCall<bool>
    {
        public override IEnumerable<string> CallCommands
        {
            get
            {
                yield return "CanPlayMusicForNPC";
            }
        }

        public override IEnumerable<Type> InputTypes
        {
            get
            {
                yield return typeof(int);
            }
        }

        protected override bool ProcessGeneric(params object[] argsWithoutCommand)
        {
            int npcID = (int)argsWithoutCommand[0];
            if (BossRushEvent.BossRushActive)
                return false;

            // Minibosses.
            if (npcID is NPCID.BigMimicCorruption or NPCID.BigMimicCrimson or NPCID.BigMimicHallow)
            {
                int npcIndex = NPC.FindFirstNPC(npcID);
                if (npcIndex < 0)
                    return false;
                return Main.npc[npcIndex].ai[2] >= 1f;
            }
            if (npcID is NPCID.SandElemental)
                return NPC.AnyNPCs(npcID);
            if (npcID == ModContent.NPCType<GiantClam>())
            {
                int npcIndex = NPC.FindFirstNPC(npcID);
                if (npcIndex < 0)
                    return false;
                return Main.npc[npcIndex].Infernum().ExtraAI[5] >= 1f;
            }

            // Bosses.
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
            if (npcID == NPCID.WallofFlesh)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.BloodNautilus)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.QueenSlimeBoss)
                return NPC.AnyNPCs(npcID);
            if (npcID is NPCID.Retinazer or NPCID.Spazmatism or NPCID.SkeletronPrime or NPCID.TheDestroyer)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.Plantera)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.Golem)
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
            if (npcID == ModContent.NPCType<PrimordialWyrmHead>())
                return NPC.AnyNPCs(npcID);
            if (npcID == ModContent.NPCType<Draedon>())
                return NPC.AnyNPCs(npcID) || InfernumMode.DraedonThemeTimer > 0;
            if (npcID == ModContent.NPCType<SupremeCalamitas>())
                return NPC.AnyNPCs(npcID);

            return false;
        }
    }
}
