using InfernumMode.Content.Tiles.Relics;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class ProvidenceRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Providence Relic";

        public override string PersonalMessage
        {
            get
            {
                if (WorldSaveSystem.HasBeatenInfernumNightProvBeforeDay)
                    return Utilities.GetLocalization("Items.ProvidenceRelic.PersonalMessage.HasBeatenInfernumNightProvBeforeDayMessage").Value;

                return Utilities.GetLocalization("Items.ProvidenceRelic.PersonalMessage.DefaultMessage").Value;
            }
        }

        public override Color? PersonalMessageColor => WorldSaveSystem.HasBeatenInfernumNightProvBeforeDay ?
            Color.Lerp(Color.Cyan, Color.Green, 0.15f) :
            Color.Lerp(Color.Orange, Color.Yellow, 0.35f);

        public override int TileID => ModContent.TileType<ProvidenceRelicTile>();
    }
}
