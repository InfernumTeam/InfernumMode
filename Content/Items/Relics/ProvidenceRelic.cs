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
                {
                    return "Bruh? What the heck? Are you OK?\n" +
                        "You were supposed to fight her at night AFTER beating her during the day first!";
                }

                return "The first major hurdle following the defeat of the Moon Lord. Your triumph over her was by no means a small feat.\n" +
                    "Perhaps consider fighting her again during the night for a special challenge?";
            }
        }

        public override Color? PersonalMessageColor => WorldSaveSystem.HasBeatenInfernumNightProvBeforeDay ?
            Color.Lerp(Color.Cyan, Color.Green, 0.15f) :
            Color.Lerp(Color.Orange, Color.Yellow, 0.35f);

        public override int TileID => ModContent.TileType<ProvidenceRelicTile>();
    }
}
