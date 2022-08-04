using CalamityMod;
using CalamityMod.Items.Accessories.Wings;
using CalamityMod.Items.Materials;
using CalamityMod.NPCs.AquaticScourge;
using CalamityMod.NPCs.AstrumAureus;
using CalamityMod.NPCs.AstrumDeus;
using CalamityMod.NPCs.BrimstoneElemental;
using CalamityMod.NPCs.Calamitas;
using CalamityMod.NPCs.Crabulon;
using CalamityMod.NPCs.Cryogen;
using CalamityMod.NPCs.DesertScourge;
using CalamityMod.NPCs.GreatSandShark;
using CalamityMod.NPCs.HiveMind;
using CalamityMod.NPCs.Leviathan;
using CalamityMod.NPCs.Perforator;
using CalamityMod.NPCs.PlaguebringerGoliath;
using CalamityMod.NPCs.Providence;
using CalamityMod.NPCs.Ravager;
using CalamityMod.NPCs.SlimeGod;
using CalamityMod.NPCs.SunkenSea;
using InfernumMode.Items.Relics;
using InfernumMode.OverridingSystem;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.GlobalInstances
{
    public partial class GlobalNPCOverrides : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            switch (npc.type)
            {
                case NPCID.BloodNautilus:
                    npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs && OverridingListManager.Registered(NPCID.BloodNautilus), ModContent.ItemType<BloodOrb>(), 1, 85, 105);
                    break;
            }

            // Relic hell.
            if (npc.type == NPCID.KingSlime)
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<KingSlimeRelic>());

            if (npc.type == ModContent.NPCType<DesertScourgeHead>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<DesertScourgeRelic>());

            if (npc.type == ModContent.NPCType<GiantClam>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<GiantClamRelic>());

            if (npc.type == NPCID.EyeofCthulhu)
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<EyeOfCthulhuRelic>());

            if (npc.type == ModContent.NPCType<Crabulon>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<CrabulonRelic>());

            if (npc.type is NPCID.DD2DarkMageT1 or NPCID.DD2DarkMageT3)
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<DarkMageRelic>());

            if (npc.type == NPCID.BrainofCthulhu)
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<BrainOfCthulhuRelic>());

            if (npc.type == NPCID.EaterofWorldsHead)
            {
                LeadingConditionRule EoWKill = new(DropHelper.If(info => info.npc.boss && InfernumMode.CanUseCustomAIs));
                EoWKill.Add(ModContent.ItemType<EaterOfWorldsRelic>());
            }

            if (npc.type == ModContent.NPCType<PerforatorHive>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<PerforatorsRelic>());

            if (npc.type == ModContent.NPCType<HiveMind>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<HiveMindRelic>());

            if (npc.type is NPCID.DD2OgreT2 or NPCID.DD2OgreT3)
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<OgreRelic>());

            if (npc.type == NPCID.QueenBee)
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<QueenBeeRelic>());

            if (npc.type == NPCID.Deerclops)
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<DeerclopsRelic>());

            if (npc.type == NPCID.SkeletronHead)
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<SkeletronRelic>());

            if (npc.type == ModContent.NPCType<SlimeGodCore>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<SlimeGodRelic>());

            if (npc.type == NPCID.WallofFlesh)
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<WallOfFleshRelic>());

            if (npc.type == NPCID.BloodNautilus)
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<DreadnautilusRelic>());

            if (npc.type == NPCID.QueenSlimeBoss)
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<QueenSlimeRelic>());

            if (npc.type == NPCID.TheDestroyer)
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<DestroyerRelic>());

            if (npc.type is NPCID.Retinazer or NPCID.Spazmatism)
            {
                LeadingConditionRule lastTwinStanding = new(DropHelper.If(_ => NPC.CountNPCS(NPCID.Retinazer) + NPC.CountNPCS(NPCID.Spazmatism) <= 1 && InfernumMode.CanUseCustomAIs));
                lastTwinStanding.Add(ModContent.ItemType<TwinsRelic>());
                npcLoot.Add(lastTwinStanding);
            }

            if (npc.type == NPCID.SkeletronPrime)
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<SkeletronPrimeRelic>());

            if (npc.type == ModContent.NPCType<Cryogen>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<CryogenRelic>());

            if (npc.type == ModContent.NPCType<AquaticScourgeHead>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<AquaticScourgeRelic>());

            if (npc.type == ModContent.NPCType<BrimstoneElemental>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<BrimstoneElementalRelic>());

            if (npc.type == ModContent.NPCType<CalamitasClone>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<CalamitasCloneRelic>());

            if (npc.type == NPCID.Plantera)
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<PlanteraRelic>());

            if (npc.type == ModContent.NPCType<GreatSandShark>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<GreatSandSharkRelic>());

            if (npc.type == ModContent.NPCType<Anahita>() || npc.type == ModContent.NPCType<Leviathan>())
            {
                LeadingConditionRule lastFishStanding = new(DropHelper.If(_ => NPC.CountNPCS(ModContent.NPCType<Anahita>()) + NPC.CountNPCS(ModContent.NPCType<Leviathan>()) <= 1 && InfernumMode.CanUseCustomAIs));
                lastFishStanding.Add(ModContent.ItemType<LeviathanRelic>());
            }

            if (npc.type == ModContent.NPCType<AstrumAureus>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<AstrumAureusRelic>());

            if (npc.type == NPCID.Golem)
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<GolemRelic>());

            if (npc.type == NPCID.DD2Betsy)
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<BetsyRelic>());

            if (npc.type == ModContent.NPCType<PlaguebringerGoliath>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<PlaguebringerGoliathRelic>());

            if (npc.type == ModContent.NPCType<RavagerBody>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<RavagerRelic>());

            if (npc.type == NPCID.HallowBoss)
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<EmpressOfLightRelic>());

            if (npc.type == NPCID.DukeFishron)
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<DukeFishronRelic>());

            if (npc.type == NPCID.CultistBoss)
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<LunaticCultistRelic>());

            if (npc.type == ModContent.NPCType<AstrumDeusHead>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<AstrumDeusRelic>());

            if (npc.type == NPCID.MoonLordCore)
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<MoonLordRelic>());
        }
    }
}