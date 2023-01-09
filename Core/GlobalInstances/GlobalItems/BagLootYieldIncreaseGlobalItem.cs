using CalamityMod;
using CalamityMod.Items.Materials;
using CalamityMod.Items.TreasureBags;
using CalamityMod.Items.TreasureBags.MiscGrabBags;
using InfernumMode.Content.Items.Pets;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.GlobalInstances.GlobalItems
{
    public class BagLootYieldIncreaseGlobalItem : GlobalItem
    {
        public override void ModifyItemLoot(Item item, ItemLoot loot)
        {
            // Yes, this can be abused by beating a boss, grabbing the bags, and temporarily turning on Infernum for more loot.
            // I do not care.
            void addInfernumExclusiveItem(int itemID, int quantity)
            {
                var infLCR = loot.DefineConditionalDropSet(DropHelper.If(() => InfernumMode.CanUseCustomAIs));
                infLCR.Add(itemID, 1, quantity, quantity + 1);
            }

            // Starter bags provide the Blasted Tophat.
            if (item.type == ModContent.ItemType<StarterBag>())
                loot.Add(ModContent.ItemType<BlastedTophat>());

            // The Eater of Worlds and Brain of Cthulhu both drop 125 extra ore and 50 extra scales/tissue samples.
            if (item.type == ItemID.EaterOfWorldsBossBag)
            {
                addInfernumExclusiveItem(ItemID.DemoniteOre, 125);
                addInfernumExclusiveItem(ItemID.ShadowScale, 50);
            }
            if (item.type == ItemID.BrainOfCthulhuBossBag)
            {
                addInfernumExclusiveItem(ItemID.CrimtaneOre, 125);
                addInfernumExclusiveItem(ItemID.TissueSample, 50);
            }

            // Queen slime drops 30 extra souls of light.
            if (item.type == ItemID.QueenSlimeBossBag)
                addInfernumExclusiveItem(ItemID.SoulofLight, 30);

            // Mechs bosses drops 30 extra souls.
            if (item.type == ItemID.DestroyerBossBag)
                addInfernumExclusiveItem(ItemID.SoulofMight, 30);

            if (item.type == ItemID.TwinsBossBag)
                addInfernumExclusiveItem(ItemID.SoulofSight, 30);

            if (item.type == ItemID.SkeletronPrimeBossBag)
                addInfernumExclusiveItem(ItemID.SoulofFright, 30);

            // Calamitas Clone drops 25 extra ashes of calamity.
            if (item.type == ModContent.ItemType<CalamitasBag>())
                addInfernumExclusiveItem(ModContent.ItemType<AshesofCalamity>(), 25);

            // Plantera drops 25 extra living shards.
            if (item.type == ItemID.PlanteraBossBag)
                addInfernumExclusiveItem(ModContent.ItemType<LivingShard>(), 25);

            // The Plaguebringer Goliath drops 25 extra infected armor plating.
            if (item.type == ModContent.ItemType<PlaguebringerGoliathBag>())
                addInfernumExclusiveItem(ModContent.ItemType<InfectedArmorPlating>(), 25);

            // Astrum Deus drops 60 extra fragments.
            if (item.type == ModContent.ItemType<AstrumDeusBag>())
            {
                addInfernumExclusiveItem(ItemID.FragmentNebula, 60);
                addInfernumExclusiveItem(ItemID.FragmentSolar, 60);
                addInfernumExclusiveItem(ItemID.FragmentStardust, 60);
                addInfernumExclusiveItem(ItemID.FragmentVortex, 60);
            }

            // The Moon Lord drops 750 extra luminite.
            if (item.type == ItemID.MoonLordBossBag)
                addInfernumExclusiveItem(ItemID.LunarOre, 60);

            // The Dragonfolly drops 25 extra effulgent feathers.
            if (item.type == ModContent.ItemType<DragonfollyBag>())
                addInfernumExclusiveItem(ModContent.ItemType<EffulgentFeather>(), 25);

            // Providence drops 30 extra divine geodes.
            if (item.type == ModContent.ItemType<ProvidenceBag>())
                addInfernumExclusiveItem(ModContent.ItemType<DivineGeode>(), 30);

            // Sentinels drops 8 extra of their respective drop material.
            if (item.type == ModContent.ItemType<StormWeaverBag>())
                addInfernumExclusiveItem(ModContent.ItemType<ArmoredShell>(), 8);

            if (item.type == ModContent.ItemType<CeaselessVoidBag>())
                addInfernumExclusiveItem(ModContent.ItemType<DarkPlasma>(), 8);

            if (item.type == ModContent.ItemType<SignusBag>())
                addInfernumExclusiveItem(ModContent.ItemType<TwistingNether>(), 8);

            // Polterghast drops 25 extra ruinous souls.
            if (item.type == ModContent.ItemType<PolterghastBag>())
                addInfernumExclusiveItem(ModContent.ItemType<RuinousSoul>(), 25);

            // The Devourer of Gods drops 960 extra cosmilite bars.
            // You will never have to fight DoG more than once for materials.
            if (item.type == ModContent.ItemType<DevourerofGodsBag>())
                addInfernumExclusiveItem(ModContent.ItemType<CosmiliteBar>(), 960);

            // Yharon drops 35 extra yharon soul fragments.
            if (item.type == ModContent.ItemType<YharonBag>())
                addInfernumExclusiveItem(ModContent.ItemType<YharonSoulFragment>(), 35);

            // Draedon drops 40 extra exo prisms.
            if (item.type == ModContent.ItemType<DraedonBag>())
                addInfernumExclusiveItem(ModContent.ItemType<ExoPrism>(), 40);

            // Calamitas drops 40 extra ashes of annihilation.
            if (item.type == ModContent.ItemType<SupremeCalamitasCoffer>())
                addInfernumExclusiveItem(ModContent.ItemType<AshesofAnnihilation>(), 40);
        }
    }
}
