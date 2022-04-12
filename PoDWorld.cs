using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs;
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

            return new TagCompound
            {
                ["downed"] = downed,
                ["DraedonAttempts"] = DraedonAttempts,
                ["DraedonSuccesses"] = DraedonSuccesses,
            };
        }
        #endregion

        #region Load
        public override void Load(TagCompound tag)
        {
            var downed = tag.GetList<string>("downed");
            InfernumMode = downed.Contains("fuckYouMode");
            DraedonAttempts = tag.GetInt("DraedonAttempts");
            DraedonSuccesses = tag.GetInt("DraedonSuccesses");
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
            BitsByte flags = new();
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

        #region Great Sand Shark Desert Area
        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref float totalWeight)
        {
            int floatingIslandIndex = tasks.FindIndex(g => g.Name == "Floating Islands");
            if (floatingIslandIndex != -1)
                tasks.Insert(floatingIslandIndex, new PassLegacy("Desert Digout Area", GenerateUndergroundDesertArea));
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
        #endregion Great Sand Shark Desert Area
    }
}
