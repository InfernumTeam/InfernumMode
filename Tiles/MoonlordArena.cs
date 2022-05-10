using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace InfernumMode.Tiles
{
    public class MoonlordArena : ModTile
    {
        public override void SetDefaults()
        {
            Main.tileSolid[Type] = true;
            Main.tileMergeDirt[Type] = true;
            Main.tileBlockLight[Type] = true;
            ModTranslation name = CreateMapEntryName();
            name.SetDefault("Arena");
            AddMapEntry(new Color(40, 70, 70), name);
        }

        public override bool CanKillTile(int i, int j, ref bool blockDamaged)
        {
            return false;
        }

        public override bool CanExplode(int i, int j)
        {
            return false;
        }

        public override void NearbyEffects(int i, int j, bool closer)
        {
            if (closer)
            {
                if (!NPC.AnyNPCs(NPCID.MoonLordCore))
                {
                    WorldGen.KillTile(i, j, false, false, false);
                    if (!Main.tile[i, j].active() && Main.netMode != NetmodeID.SinglePlayer)
                    {
                        NetMessage.SendData(MessageID.TileChange, -1, -1, null, 0, i, j, 0f, 0, 0, 0);
                    }
                }
            }
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            r = 40f / 255f;
            g = Main.DiscoR / 255f / 4.25f + (40f / 255f);
            b = 74f / 255f;
        }
    }
}
