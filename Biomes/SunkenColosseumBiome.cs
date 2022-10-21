using InfernumMode.Subworlds;
using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Biomes
{
    public class SunkenColosseumBiome : ModBiome
    {
        public override ModWaterStyle WaterStyle => ModContent.Find<ModWaterStyle>("CalamityMod/SunkenSeaWater");

        public override ModUndergroundBackgroundStyle UndergroundBackgroundStyle => ModContent.Find<ModUndergroundBackgroundStyle>("InfernumMode/SunkenColosseumBGStyle");

        public override SceneEffectPriority Priority => SceneEffectPriority.BossMedium;

        public override string BestiaryIcon => "InfernumMode/Biomes/SunkenColosseumIcon";

        public override string BackgroundPath => "InfernumMode/Backgrounds/SunkenColosseumBG";

        public override string MapBackground => "InfernumMode/Backgrounds/SunkenColosseumBG";

        public override int Music => MusicID.UndergroundDesert;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sunken Colosseum");
        }

        public override bool IsBiomeActive(Player player) => SubworldSystem.IsActive<SunkenColosseum>();

        public override float GetWeight(Player player) => 0.9f;
    }
}
