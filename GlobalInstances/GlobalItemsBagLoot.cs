using CalamityMod;
using CalamityMod.Items.Materials;
using CalamityMod.Items.TreasureBags;
using CalamityMod.Items.TreasureBags.MiscGrabBags;
using InfernumMode.Items.Pets;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.GlobalInstances
{
    public partial class GlobalItems : GlobalItem
    {
        public override void ModifyItemLoot(Item item, ItemLoot loot)
        {
            // Starter bags provide the Blasted Tophat.
            if (item.type == ModContent.ItemType<StarterBag>())
                loot.Add(ModContent.ItemType<BlastedTophat>());

            // The Eater of Worlds and Brain of Cthulhu both drop 125 extra ore and 50 extra scales/tissue samples.
            if (item.type == ItemID.EaterOfWorldsBossBag)
            {
                var eowInfLCR = loot.DefineConditionalDropSet(DropHelper.If(() => InfernumMode.CanUseCustomAIs));
                eowInfLCR.Add(ItemID.DemoniteOre, 1, 125, 126);
                eowInfLCR.Add(ItemID.ShadowScale, 1, 50, 51);
            }
            if (item.type == ItemID.BrainOfCthulhuBossBag)
            {
                var bocInfLCR = loot.DefineConditionalDropSet(DropHelper.If(() => InfernumMode.CanUseCustomAIs));
                bocInfLCR.Add(ItemID.CrimtaneOre, 1, 125, 126);
                bocInfLCR.Add(ItemID.TissueSample, 1, 50, 51);
            }

            // Queen slime drops 30 extra souls of light.
            if (item.type == ItemID.QueenSlimeBossBag)
            {
                var qsInfLCR = loot.DefineConditionalDropSet(DropHelper.If(() => InfernumMode.CanUseCustomAIs));
                qsInfLCR.Add(ItemID.SoulofLight, 1, 30, 31);
            }

            // Mechs bosses drops 30 extra souls.
            if (item.type == ItemID.DestroyerBossBag)
            {
                var desInfLCR = loot.DefineConditionalDropSet(DropHelper.If(() => InfernumMode.CanUseCustomAIs));
                desInfLCR.Add(ItemID.SoulofMight, 1, 30, 31);
            }
            if (item.type == ItemID.TwinsBossBag)
            {
                var twinsInfLCR = loot.DefineConditionalDropSet(DropHelper.If(() => InfernumMode.CanUseCustomAIs));
                twinsInfLCR.Add(ItemID.SoulofSight, 1, 30, 31);
            }
            if (item.type == ItemID.SkeletronPrimeBossBag)
            {
                var primeInfLCR = loot.DefineConditionalDropSet(DropHelper.If(() => InfernumMode.CanUseCustomAIs));
                primeInfLCR.Add(ItemID.SoulofFright, 1, 30, 31);
            }

            // Calamitas Clone drops 25 extra ashes of calamity.
            if (item.type == ModContent.ItemType<CalamitasBag>())
            {
                var cloneInfLCR = loot.DefineConditionalDropSet(DropHelper.If(() => InfernumMode.CanUseCustomAIs));
                cloneInfLCR.Add(ModContent.ItemType<AshesofCalamity>(), 1, 25, 26);
            }

            // Plantera drops 25 extra living shards.
            if (item.type == ItemID.PlanteraBossBag)
            {
                var plantInfLCR = loot.DefineConditionalDropSet(DropHelper.If(() => InfernumMode.CanUseCustomAIs));
                plantInfLCR.Add(ModContent.ItemType<LivingShard>(), 1, 25, 26);
            }

            // The Plaguebringer Goliath drops 25 extra infected armor plating.
            if (item.type == ModContent.ItemType<PlaguebringerGoliathBag>())
            {
                var pbgInfLCR = loot.DefineConditionalDropSet(DropHelper.If(() => InfernumMode.CanUseCustomAIs));
                pbgInfLCR.Add(ModContent.ItemType<InfectedArmorPlating>(), 1, 25, 26);
            }

            // Astrum Deus drops 60 extra fragments.
            if (item.type == ModContent.ItemType<AstrumDeusBag>())
            {
                var deusInfLCR = loot.DefineConditionalDropSet(DropHelper.If(() => InfernumMode.CanUseCustomAIs));
                deusInfLCR.Add(ItemID.FragmentSolar, 1, 60, 61);
                deusInfLCR.Add(ItemID.FragmentStardust, 1, 60, 61);
                deusInfLCR.Add(ItemID.FragmentNebula, 1, 60, 61);
                deusInfLCR.Add(ItemID.FragmentVortex, 1, 60, 61);
            }

            // The Moon Lord drops 600 extra luminite.
            if (item.type == ItemID.MoonLordBossBag)
            {
                var mlInfLCR = loot.DefineConditionalDropSet(DropHelper.If(() => InfernumMode.CanUseCustomAIs));
                mlInfLCR.Add(ItemID.LunarOre, 1, 600, 601);
            }

            // The Dragonfolly drops 25 extra effulgent feathers.
            if (item.type == ModContent.ItemType<DragonfollyBag>())
            {
                var follyInfLCR = loot.DefineConditionalDropSet(DropHelper.If(() => InfernumMode.CanUseCustomAIs));
                follyInfLCR.Add(ModContent.ItemType<EffulgentFeather>(), 1, 25, 26);
            }

            // Providence drops 30 extra divine geodes.
            if (item.type == ModContent.ItemType<ProvidenceBag>())
            {
                var provInfLCR = loot.DefineConditionalDropSet(DropHelper.If(() => InfernumMode.CanUseCustomAIs));
                provInfLCR.Add(ModContent.ItemType<DivineGeode>(), 1, 30, 31);
            }

            // Sentinels drops 8 extra of thier respective drop material.
            if (item.type == ModContent.ItemType<StormWeaverBag>())
            {
                var swInfLCR = loot.DefineConditionalDropSet(DropHelper.If(() => InfernumMode.CanUseCustomAIs));
                swInfLCR.Add(ModContent.ItemType<ArmoredShell>(), 1, 8, 9);
            }
            if (item.type == ModContent.ItemType<CeaselessVoidBag>())
            {
                var voidInfLCR = loot.DefineConditionalDropSet(DropHelper.If(() => InfernumMode.CanUseCustomAIs));
                voidInfLCR.Add(ModContent.ItemType<DarkPlasma>(), 1, 8, 9);
            }
            if (item.type == ModContent.ItemType<SignusBag>())
            {
                var signusInfLCR = loot.DefineConditionalDropSet(DropHelper.If(() => InfernumMode.CanUseCustomAIs));
                signusInfLCR.Add(ModContent.ItemType<TwistingNether>(), 1, 8, 9);
            }

            // Polterghast drops 25 extra ruinous souls.
            if (item.type == ModContent.ItemType<PolterghastBag>())
            {
                var polterInfLCR = loot.DefineConditionalDropSet(DropHelper.If(() => InfernumMode.CanUseCustomAIs));
                polterInfLCR.Add(ModContent.ItemType<RuinousSoul>(), 1, 25, 26);
            }

            // The Devourer of Gods drops 400 extra cosmilite bars.
            if (item.type == ModContent.ItemType<DevourerofGodsBag>())
            {
                var dogInfLCR = loot.DefineConditionalDropSet(DropHelper.If(() => InfernumMode.CanUseCustomAIs));
                dogInfLCR.Add(ModContent.ItemType<CosmiliteBar>(), 1, 400, 401);
            }

            // Yharon drops 35 extra yharon soul fragments.
            if (item.type == ModContent.ItemType<YharonBag>())
            {
                var yharonInfLCR = loot.DefineConditionalDropSet(DropHelper.If(() => InfernumMode.CanUseCustomAIs));
                yharonInfLCR.Add(ModContent.ItemType<YharonSoulFragment>(), 1, 35, 36);
            }

            // Draedon drops 40 extra exo prisms.
            if (item.type == ModContent.ItemType<DraedonBag>())
            {
                var draedonInfLCR = loot.DefineConditionalDropSet(DropHelper.If(() => InfernumMode.CanUseCustomAIs));
                draedonInfLCR.Add(ModContent.ItemType<ExoPrism>(), 1, 40, 41);
            }

            // Calamitas drops 40 extra ashes of annihilation.
            if (item.type == ModContent.ItemType<SupremeCalamitasCoffer>())
            {
                var calInfLCR = loot.DefineConditionalDropSet(DropHelper.If(() => InfernumMode.CanUseCustomAIs));
                calInfLCR.Add(ModContent.ItemType<AshesofAnnihilation>(), 1, 40, 41);
            }
        }
    }
}
