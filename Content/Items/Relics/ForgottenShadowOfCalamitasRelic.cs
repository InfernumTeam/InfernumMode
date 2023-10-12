using InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasShadow;
using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    [LegacyName("CalamitasCloneRelic")]
    public class ForgottenShadowOfCalamitasRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal" + CalamitasShadowBehaviorOverride.CustomName.Value + "Relic";

        public override int TileID => ModContent.TileType<ForgottenShadowOfCalamitasRelicTile>();
    }
}
