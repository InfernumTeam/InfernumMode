using CalamityMod.Schematics;
using InfernumMode.Projectiles;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Items
{
	public class ProvidenceArenaDelimiter : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Providence Arena Delimiter");
			Tooltip.SetDefault("Hold and drag to select an area to turn into Infernum Providence's arena");
		}

		public override void SetDefaults()
		{
			Item.width = 38;
			Item.height = 26;

			Item.useTime = Item.useAnimation = 40;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.channel = true;

			Item.shoot = ModContent.ProjectileType<ProvidenceArenaDelimiterProj>();
			Item.shootSpeed = 0f;

			Item.rare = ItemRarityID.Red;
			Item.value = 0;

			Item.UseSound = SoundID.Item100;
		}
	}
}
