using InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasClone;
using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    [LegacyName("CalamitasCloneRelic")]
    public class ForgottenShadowOfCalamitasRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => $"Infernal {CalamitasCloneBehaviorOverride.CustomName} Relic";

        public override int TileID => ModContent.TileType<ForgottenShadowOfCalamitasRelicTile>();
    }
}
