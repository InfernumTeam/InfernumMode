using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.Tiles.FurnitureProfaned;
using InfernumMode.Tiles;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.World.Generation;

using ModInstance = InfernumMode.InfernumMode;

namespace InfernumMode
{
	public class PoDWorld : ModWorld
	{
		public static int DraedonAttempts;
		public static int DraedonSuccesses;
		public static bool InfernumMode = false;

		public static Rectangle ProvidenceArena
		{
			get;
			set;
		} = Rectangle.Empty;

		public override void Initialize()
		{
			InfernumMode = false;
		}

		#region Save
		public override TagCompound Save()
		{
			var downed = new List<string>();
			if (InfernumMode)
				downed.Add("fuckYouMode");

			TagCompound tag = new TagCompound();
			tag["downed"] = downed;
			tag["ProvidenceArenaX"] = ProvidenceArena.X;
			tag["ProvidenceArenaY"] = ProvidenceArena.Y;
			tag["ProvidenceArenaWidth"] = ProvidenceArena.Width;
			tag["ProvidenceArenaHeight"] = ProvidenceArena.Height;
			return tag;
		}
		#endregion

		#region Load
		public override void Load(TagCompound tag)
		{
			var downed = tag.GetList<string>("downed");
			InfernumMode = downed.Contains("fuckYouMode");
			DraedonAttempts = tag.GetInt("DraedonAttempts");
			DraedonSuccesses = tag.GetInt("DraedonSuccesses");
			ProvidenceArena = new Rectangle(tag.GetInt("ProvidenceArenaX"), tag.GetInt("ProvidenceArenaY"), tag.GetInt("ProvidenceArenaWidth"), tag.GetInt("ProvidenceArenaHeight"));
		}
		#endregion

		#region LoadLegacy
		public override void LoadLegacy(BinaryReader reader)
		{
			int loadVersion = reader.ReadInt32();
			if (loadVersion == 0)
			{
				BitsByte flags = reader.ReadByte();
				InfernumMode = flags[0];
			}
		}
		#endregion

		#region NetSend
		public override void NetSend(BinaryWriter writer)
		{
			BitsByte flags = new BitsByte();
			flags[0] = InfernumMode;
			writer.Write(flags);
			writer.Write(DraedonAttempts);
			writer.Write(DraedonSuccesses);
		}
		#endregion

		#region NetReceive
		public override void NetReceive(BinaryReader reader)
		{
			BitsByte flags = reader.ReadByte();
			InfernumMode = flags[0];
			DraedonAttempts = reader.ReadInt32();
			DraedonSuccesses = reader.ReadInt32();
		}
		#endregion

		#region Updating
		public override void PostUpdate()
		{
			// Disable natural GSS spawns.
			if (ModInstance.CanUseCustomAIs)
				CalamityMod.CalamityMod.sharkKillCount = 0;

			if (!NPC.AnyNPCs(ModContent.NPCType<Draedon>()))
				CalamityGlobalNPC.draedon = -1;
		}
		#endregion Updating

		#region Worldgen
		public override void ModifyWorldGenTasks(List<GenPass> tasks, ref float totalWeight)
		{
			int floatingIslandIndex = tasks.FindIndex(g => g.Name == "Floating Islands");
			if (floatingIslandIndex != -1)
				tasks.Insert(floatingIslandIndex, new PassLegacy("Desert Digout Area", GenerateUndergroundDesertArea));
			int finalIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Final Cleanup"));
			if (finalIndex != -1)
			{
				int currentFinalIndex = finalIndex;
				tasks.Insert(++currentFinalIndex, new PassLegacy("Prov Arena", progress =>
				{
					progress.Message = "Constructing a temple for an ancient goddess";
					GenerateProfanedShrine(progress);
				}));
			}
		}

		public static void GenerateUndergroundDesertArea(GenerationProgress progress)
		{
			Vector2 cutoutAreaCenter = WorldGen.UndergroundDesertLocation.Center.ToVector2();

			for (int i = 0; i < 4; i++)
			{
				cutoutAreaCenter += WorldGen.genRand.NextVector2Circular(15f, 15f);
				WorldUtils.Gen(cutoutAreaCenter.ToPoint(), new Shapes.Mound(75, 48), Actions.Chain(
					new Modifiers.Blotches(12),
					new Actions.ClearTile(),
					new Actions.PlaceWall(WallID.Sandstone)
					));
			}
		}

		public static void GenerateProfanedShrine(GenerationProgress progress)
		{
			int width = 250;
			int height = 125;
			ushort slabID = (ushort)ModContent.TileType<ProfanedSlab>();
			ushort runeID = (ushort)ModContent.TileType<RunicProfanedBrick>();
			ushort provSummonerID = (ushort)ModContent.TileType<ProvidenceSummoner>();

			int left = Main.maxTilesX - width - Main.offLimitBorderTiles;
			int top = Main.maxTilesY - 180;
			int bottom = top + height;
			int centerX = left + width / 2;

			// Define the arena area.
			PoDWorld.ProvidenceArena = new Rectangle(left, top, width, height);

			// Clear out the entire area where the shrine will be made.
			for (int x = left; x < left + width; x++)
			{
				for (int y = top; y < bottom; y++)
				{
					Main.tile[x, y].liquid = 0;
					Main.tile[x, y].active(false);
				}
			}

			// Create the floor and ceiling.
			for (int x = left; x < left + width; x++)
			{
				int y = bottom - 1;
				while (!Main.tile[x, y].active())
				{
					Main.tile[x, y].liquid = 0;
					Main.tile[x, y].active(true);
					Main.tile[x, y].type = WorldGen.genRand.NextBool(5) ? runeID : slabID;
					Main.tile[x, y].slope(0);
					Main.tile[x, y].halfBrick(false);

					y++;

					if (y >= Main.maxTilesY)
						break;
				}

				y = top + 1;
				while (!Main.tile[x, y].active())
				{
					Main.tile[x, y].liquid = 0;
					Main.tile[x, y].active(true);
					Main.tile[x, y].type = WorldGen.genRand.NextBool(5) ? runeID : slabID;
					Main.tile[x, y].slope(0);
					Main.tile[x, y].halfBrick(false);

					y--;

					if (y < top - 40)
						break;
				}
			}

			// Create the right wall.
			for (int y = top; y < bottom + 2; y++)
			{
				int x = left + width - 1;
				Main.tile[x, y].liquid = 0;
				Main.tile[x, y].active(true);
				Main.tile[x, y].type = WorldGen.genRand.NextBool(5) ? runeID : slabID;
				Main.tile[x, y].slope(0);
				Main.tile[x, y].halfBrick(false);
			}

			// Find the vertical point at which stairs should be placed.
			int stairLeft = left - 1;
			int stairTop = bottom - 1;
			while (Main.tile[stairLeft, stairTop].liquid > 0 || Main.tile[stairLeft, stairTop].active())
				stairTop--;

			// Create stairs until a bottom is reached.
			int stairWidth = bottom - stairTop;
			for (int x = stairLeft - 3; x < stairLeft + stairWidth; x++)
			{
				int stairHeight = stairWidth - (x - stairLeft);
				if (x < stairLeft)
					stairHeight = stairWidth;

				for (int y = -stairHeight; y < 0; y++)
				{
					Main.tile[x, y + bottom].liquid = 0;
					Main.tile[x, y + bottom].type = WorldGen.genRand.NextBool(5) ? runeID : slabID;
					Main.tile[x, y + bottom].active(true);
					Main.tile[x, y + bottom].slope(0);
					Main.tile[x, y + bottom].halfBrick(false);
					WorldGen.TileFrame(x, y + bottom);
				}
			}

			// Settle liquids.
			Liquid.QuickWater(3);
			WorldGen.WaterCheck();

			Liquid.quickSettle = true;
			for (int i = 0; i < 10; i++)
			{
				while (Liquid.numLiquid > 0)
					Liquid.UpdateLiquid();
				WorldGen.WaterCheck();
			}
			Liquid.quickSettle = false;

			// Clear out any liquids.
			for (int x = left - 20; x < Main.maxTilesX; x++)
			{
				for (int y = top - 15; y < bottom + 8; y++)
					Main.tile[x, y].liquid = 0;
			}

			// Create the Providence altar.
			short frameX = 0;
			short frameY;
			for (int x = centerX; x < centerX + ProvidenceSummoner.Width; x++)
			{
				frameY = 0;
				for (int y = bottom - 3; y < bottom + ProvidenceSummoner.Height - 3; y++)
				{
					Main.tile[x, y].liquid = 0;
					Main.tile[x, y].type = provSummonerID;
					Main.tile[x, y].frameX = frameX;
					Main.tile[x, y].frameY = frameY;
					Main.tile[x, y].active(true);
					Main.tile[x, y].slope(0);
					Main.tile[x, y].halfBrick(false);

					frameY += 18;
				}
				frameX += 18;
			}
		}
		#endregion Great Sand Shark Desert Area
	}
}
