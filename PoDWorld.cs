using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

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
            if (!NPC.AnyNPCs(ModContent.NPCType<Draedon>()))
                CalamityGlobalNPC.draedon = -1;
        }
        #endregion Updating
    }
}