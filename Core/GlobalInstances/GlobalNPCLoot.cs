using CalamityMod;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Placeables.Furniture.Trophies;
using CalamityMod.Items.SummonItems;
using CalamityMod.Items.Weapons.Rogue;
using CalamityMod.NPCs;
using CalamityMod.NPCs.AcidRain;
using CalamityMod.NPCs.AquaticScourge;
using CalamityMod.NPCs.AstrumAureus;
using CalamityMod.NPCs.AstrumDeus;
using CalamityMod.NPCs.BrimstoneElemental;
using CalamityMod.NPCs.Bumblebirb;
using CalamityMod.NPCs.CalClone;
using CalamityMod.NPCs.CeaselessVoid;
using CalamityMod.NPCs.Crabulon;
using CalamityMod.NPCs.Cryogen;
using CalamityMod.NPCs.DesertScourge;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.NPCs.HiveMind;
using CalamityMod.NPCs.Leviathan;
using CalamityMod.NPCs.NormalNPCs;
using CalamityMod.NPCs.Perforator;
using CalamityMod.NPCs.PlaguebringerGoliath;
using CalamityMod.NPCs.Polterghast;
using CalamityMod.NPCs.PrimordialWyrm;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.NPCs.Providence;
using CalamityMod.NPCs.Ravager;
using CalamityMod.NPCs.Signus;
using CalamityMod.NPCs.SlimeGod;
using CalamityMod.NPCs.StormWeaver;
using CalamityMod.NPCs.SunkenSea;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.NPCs.Yharon;
using CalamityMod.World;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon;
using InfernumMode.Content.BehaviorOverrides.BossAIs.GreatSandShark;
using InfernumMode.Content.Items.Dyes;
using InfernumMode.Content.Items.Relics;
using InfernumMode.Content.Items.SummonItems;
using InfernumMode.Content.Items.Weapons.Magic;
using InfernumMode.Content.Items.Weapons.Melee;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using OldDukeNPC = CalamityMod.NPCs.OldDuke.OldDuke;

namespace InfernumMode.Core.GlobalInstances
{
    public partial class GlobalNPCOverrides : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (!Utilities.CanOverride(npc, out object container))
            {
                return;
            }
            // Relic hell.
            container.NPCOverride().ModifyNPCLoot(npc, npcLoot);
            // Just give him these drops if in Infernum Mode.
            if (npc.type == ModContent.NPCType<Cataclysm>())
            {
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<CatastropheTrophy>(), 10);
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<CrushsawCrasher>(), 4);
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
                npcLoot.AddIf((info) => InfernumMode.CanUseCustomAIs && !AstrumDeusHead.ShouldNotDropThings(info.npc), ModContent.ItemType<AstrumDeusRelic>());

            if (npc.type == ModContent.NPCType<BereftVassal>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<BereftVassalRelic>());

            if (npc.type == NPCID.MoonLordCore)
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<MoonLordRelic>());

            if (npc.type == ModContent.NPCType<ProfanedGuardianCommander>())
            {
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<Punctus>());
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<ProfanedGuardiansRelic>());
            }

            if (npc.type == ModContent.NPCType<Dragonfolly>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<DragonfollyRelic>());

            if (npc.type == ModContent.NPCType<Providence>())
            {
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs && Main.npc[CalamityGlobalNPC.holyBoss].Infernum().ExtraAI[6] == 1f, ModContent.ItemType<ProfanedCrystalDye>(), 1, 4, 5);
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<ProvidenceRelic>());
            }

            if (npc.type == ModContent.NPCType<CeaselessVoid>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<CeaselessVoidRelic>());

            if (npc.type == ModContent.NPCType<StormWeaverHead>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<StormWeaverRelic>());

            if (npc.type == ModContent.NPCType<Signus>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<SignusRelic>());

            if (npc.type == ModContent.NPCType<Polterghast>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<PolterghastRelic>());

            if (npc.type == ModContent.NPCType<NuclearTerror>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<NuclearTerrorRelic>());

            if (npc.type == ModContent.NPCType<DevourerofGodsHead>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<DevourerOfGodsRelic>());

            if (npc.type == ModContent.NPCType<Yharon>())
            {
                // UNLIMITED CHICKEN NUGGET
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<YharonRelic>());
                npcLoot.Add(new DropOneByOne(ItemID.ChickenNugget, new DropOneByOne.Parameters()
                {
                    ChanceNumerator = 1,
                    ChanceDenominator = 1,
                    MinimumStackPerChunkBase = 10,
                    MaximumStackPerChunkBase = 15,
                    MinimumItemDropsCount = 600,
                    MaximumItemDropsCount = 700
                }));
            }

            if (npc.type == ModContent.NPCType<PrimordialWyrmHead>())
            {
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<EvokingSearune>());
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<IllusionersReverie>());
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<EyeOfMadness>());
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<AEWRelic>());
                npcLoot.AddIf(() => WorldSaveSystem.InPostAEWUpdateWorld, ModContent.ItemType<Terminus>());
            }

            bool isExoMech = npc.type == ModContent.NPCType<ThanatosHead>() || npc.type == ModContent.NPCType<Apollo>() || npc.type == ModContent.NPCType<AresBody>();
            if (isExoMech)
                npcLoot.AddIf(() => ExoMechManagement.TotalMechs <= 1 && InfernumMode.CanUseCustomAIs, ModContent.ItemType<DraedonRelic>());

            if (npc.type == ModContent.NPCType<SupremeCalamitas>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<SupremeCalamitasRelic>());

            // Make Eidolists always drop the tablet.
            if (npc.type == ModContent.NPCType<Eidolist>())
                npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<EidolonTablet>());
        }

        public override bool PreKill(NPC npc)
        {
            if (!Utilities.CanOverride(npc, out object container))
            {
                // Prevent wandering eye fishes from dropping loot if they were spawned by a dreadnautilus.
                if (InfernumMode.CanUseCustomAIs && npc.type == NPCID.EyeballFlyingFish && NPC.AnyNPCs(NPCID.BloodNautilus))
                    DropHelper.BlockEverything();
                return base.PreKill(npc);
            }

            //if (npc.type == NPCID.EaterofWorldsHead && NPCBehaviorOverride.Registered(npc.type))
            //    return EoWHeadBehaviorOverride.PerformDeathEffect(npc);

            //if (npc.type == ModContent.NPCType<OldDukeNPC>() && NPCBehaviorOverride.Registered(npc.type))
            //    CalamityWorld.StopRain();

            return container.NPCOverride().PreKill(npc);
        }
    }
}
