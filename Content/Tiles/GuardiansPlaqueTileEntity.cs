using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles
{
    //public class GuardiansPlaqueTileEntity : ModTileEntity
    //{
    //    public bool Active
    //    {
    //        get;
    //        private set;
    //    } = false;

    //    public Vector2 Center => Position.ToWorldCoordinates(48f, 56f);

    //    public override bool IsTileValidForEntity(int x, int y)
    //    {
    //        Tile tile = Main.tile[x, y];
    //        if (tile.HasTile && tile.TileType == ModContent.TileType<GuardiansPlaque>() && tile.TileFrameX == 0)
    //            return tile.TileFrameY == 0;

    //        return false;
    //    }

    //    public override int Hook_AfterPlacement(int i, int j, int type, int style, int direction, int alternate)
    //    {
    //        if (Main.netMode == NetmodeID.MultiplayerClient)
    //        {
    //            NetMessage.SendTileSquare(Main.myPlayer, i, j, 6, 7);
    //            NetMessage.SendData(MessageID.TileEntityPlacement, -1, -1, null, i, j, Type);
    //            return -1;
    //        }
    //        return Place(i, j);
    //    }

    //    public override void Update()
    //    {
    //        float maxDistance = 100000;
    //        Active = false;
    //        for (int i = 0; i < 255; i++)
    //        {
    //            Player p = Main.player[i];
    //            if (p.active && p.DistanceSQ(Center) < maxDistance)
    //            {
    //                Active = true;
    //                break;
    //            }
    //        }
    //    }

    //    public override void OnNetPlace() => NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, ID, Position.X, Position.Y);

    //    public override void NetSend(BinaryWriter writer) => writer.Write(Active);

    //    public override void NetReceive(BinaryReader reader) => Active = reader.ReadBoolean();
    //}
}
