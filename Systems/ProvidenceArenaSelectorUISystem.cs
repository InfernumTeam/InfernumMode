using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace InfernumMode.Systems
{
	public class ProvidenceArenaSelectorUISystem : ModSystem
	{
		private const float Epsilon = 5E-6f;
		private const float OutOfSelectionDimFactor = 0.06f;
		private static readonly Color BaseGridColor = new(0.24f, 0.8f, 0.9f, 0.5f);
		private static readonly Rectangle TexUpperHalfRect = new(0, 0, 18, 18);

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) =>
			layers.Insert(0, new LegacyGameInterfaceLayer("Prov Arena Selection Grid", RenderSchematicSelectionGrid));

		private static bool RenderSchematicSelectionGrid()
		{
			Texture2D gridSquareTex = TextureAssets.Extra[68].Value;
			Rectangle? rectNull = Main.LocalPlayer.Infernum().SelectedProvidenceArena;
			if (!rectNull.HasValue)
				return true;
			Rectangle selection = rectNull.Value;

			Vector2 topLeftScreenTile = (Main.screenPosition / 16f).Floor();
			for (int i = 0; i <= Main.screenWidth; i += 16)
			{
				for (int j = 0; j <= Main.screenHeight; j += 16)
				{
					Vector2 offset = new(i >> 4, j >> 4);
					Vector2 gridTilePos = topLeftScreenTile + offset;
					Point gridTilePoint = new((int)(gridTilePos.X + Epsilon), (int)(gridTilePos.Y + Epsilon));
					bool inSelection = selection.Contains(gridTilePoint);
					Color gridColor = BaseGridColor * (inSelection ? 1f : OutOfSelectionDimFactor);
					Main.spriteBatch.Draw(gridSquareTex, gridTilePos * 16f - Main.screenPosition, TexUpperHalfRect, gridColor, 0f, Vector2.Zero, 1f, 0, 0f);
				}
			}
			return true;
		}
	}
}
