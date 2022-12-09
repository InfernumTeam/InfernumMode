using InfernumMode.Subworlds;
using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Biomes
{
    public class LostColosseumBiome : ModBiome
    {
        public override ModWaterStyle WaterStyle => ModContent.Find<ModWaterStyle>("CalamityMod/SunkenSeaWater");

        public override ModSurfaceBackgroundStyle SurfaceBackgroundStyle => ModContent.Find<ModSurfaceBackgroundStyle>("InfernumMode/LostColosseumSurfaceBGStyle");

        public override ModUndergroundBackgroundStyle UndergroundBackgroundStyle => ModContent.Find<ModUndergroundBackgroundStyle>("InfernumMode/LostColosseumBGStyle");

        public override SceneEffectPriority Priority => SceneEffectPriority.BossMedium;

        public override string BestiaryIcon => "InfernumMode/Biomes/LostColosseumIcon";

        public override string BackgroundPath => "InfernumMode/Backgrounds/LostColosseumBG";

        public override string MapBackground => "InfernumMode/Backgrounds/LostColosseumBG";

        public override int Music => LostColosseum.HasBereftVassalBeenDefeated ? MusicID.Desert : MusicLoader.GetMusicSlot(Mod, "Sounds/Music/LostColosseum");

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Lost Colosseum");
        }

        public override bool IsBiomeActive(Player player) => SubworldSystem.IsActive<LostColosseum>();

        public override float GetWeight(Player player) => 0.96f;
    }
}
