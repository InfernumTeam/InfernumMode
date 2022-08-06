using CalamityMod;
using CalamityMod.Items.Accessories.Wings;
using CalamityMod.Items.Materials;
using CalamityMod.NPCs.AquaticScourge;
using CalamityMod.NPCs.AstrumAureus;
using CalamityMod.NPCs.AstrumDeus;
using CalamityMod.NPCs.BrimstoneElemental;
using CalamityMod.NPCs.Bumblebirb;
using CalamityMod.NPCs.Calamitas;
using CalamityMod.NPCs.CeaselessVoid;
using CalamityMod.NPCs.Crabulon;
using CalamityMod.NPCs.Cryogen;
using CalamityMod.NPCs.DesertScourge;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.NPCs.GreatSandShark;
using CalamityMod.NPCs.HiveMind;
using CalamityMod.NPCs.Leviathan;
using CalamityMod.NPCs.OldDuke;
using CalamityMod.NPCs.Perforator;
using CalamityMod.NPCs.PlaguebringerGoliath;
using CalamityMod.NPCs.Polterghast;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.NPCs.Providence;
using CalamityMod.NPCs.Ravager;
using CalamityMod.NPCs.Signus;
using CalamityMod.NPCs.SlimeGod;
using CalamityMod.NPCs.StormWeaver;
using CalamityMod.NPCs.SunkenSea;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.NPCs.Yharon;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.Athena;
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
                    npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<BloodOrb>(), 1, 85, 105);
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
                npcLoot.Add(EoWKill);
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
                npcLoot.Add(lastFishStanding);
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

            if (npc.type == ModContent.NPCType<ProfanedGuardianCommander>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<ProfanedGuardiansRelic>());

            if (npc.type == ModContent.NPCType<Bumblefuck>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<DragonfollyRelic>());

            if (npc.type == ModContent.NPCType<Providence>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<ProvidenceRelic>());

            if (npc.type == ModContent.NPCType<CeaselessVoid>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<CeaselessVoidRelic>());

            if (npc.type == ModContent.NPCType<StormWeaverHead>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<StormWeaverRelic>());

            if (npc.type == ModContent.NPCType<Signus>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<SignusRelic>());

            if (npc.type == ModContent.NPCType<Polterghast>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<PolterghastRelic>());

            if (npc.type == ModContent.NPCType<OldDuke>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<OldDukeRelic>());

            if (npc.type == ModContent.NPCType<DevourerofGodsHead>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<DevourerOfGodsRelic>());

            if (npc.type == ModContent.NPCType<Yharon>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<YharonRelic>());

            if (Utilities.IsExoMech(npc))
            {
                LeadingConditionRule lastMechStanding = new(DropHelper.If(_ => 
                    NPC.CountNPCS(ModContent.NPCType<ThanatosHead>()) + 
                    NPC.CountNPCS(ModContent.NPCType<Apollo>()) +
                    NPC.CountNPCS(ModContent.NPCType<AresBody>()) +
                    NPC.CountNPCS(ModContent.NPCType<AthenaNPC>()) <= 1 && InfernumMode.CanUseCustomAIs));
                lastMechStanding.Add(ModContent.ItemType<DraedonRelic>());
                npcLoot.Add(lastMechStanding);
            }
            
            if (npc.type == ModContent.NPCType<SupremeCalamitas>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<SupremeCalamitasRelic>());
        }
    }
}