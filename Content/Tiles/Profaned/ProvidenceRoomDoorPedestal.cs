using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Tiles.FurnitureProfaned;
using InfernumMode.Assets.Sounds;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.Netcode;
using InfernumMode.Core.Netcode.Packets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace InfernumMode.Content.Tiles.Profaned
{
    public class ProvidenceRoomDoorPedestal : ModTile
    {
        public SlotId ShimmerID;

        public const int Width = 4;

        public const int Height = 1;

        public override void SetStaticDefaults()
        {
            MinPick = int.MaxValue;
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileSpelunker[Type] = true;
            Main.tileNoAttach[ModContent.TileType<ProfanedCrystal>()] = false;

            // Apparently this is necessary in multiplayer for some reason???
            MinPick = int.MaxValue;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x1);
            TileObjectData.newTile.Width = Width;
            TileObjectData.newTile.Height = Height;
            TileObjectData.newTile.Origin = new Point16(2, 0);
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16 };
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.addTile(Type);
            AddMapEntry(new Color(122, 66, 59));
        }

        public override bool CanExplode(int i, int j) => false;

        public override bool CanKillTile(int i, int j, ref bool blockDamaged) => false;

        public override bool CreateDust(int i, int j, ref int type)
        {
            // Fire dust.
            type = 6;
            return true;
        }

        public override void NearbyEffects(int i, int j, bool closer)
        {
            if (Main.gamePaused)
                return;

            // Calculate the door position in the world.
            Tile tile = CalamityUtils.ParanoidTileRetrieval(i, j);
            Vector2 bottom = new Vector2(i, j).ToWorldCoordinates(8f, 0f);
            if ((WorldSaveSystem.ProvidenceDoorXPosition == 0 || WorldSaveSystem.ProvidenceDoorXPosition != bottom.X) && tile.TileFrameX == 18 && tile.TileFrameY == 0)
            {
                WorldSaveSystem.ProvidenceDoorXPosition = (int)bottom.X;
                CalamityNetcode.SyncWorld();
            }

            ref int shatterTimer = ref Main.LocalPlayer.Infernum_Biome().providenceRoomShatterTimer;

            if (WorldSaveSystem.HasProvidenceDoorShattered)
                return;

            int verticalOffset = 0;
            for (int k = 2; k < 200; k++)
            {
                if (WorldGen.SolidTile(i, j - k))
                {
                    verticalOffset = k * 16 + 24;
                    break;
                }
            }

            bool close = Main.LocalPlayer.WithinRange(bottom, 300f) || shatterTimer >= 2;
            shatterTimer = Utils.Clamp(shatterTimer + close.ToDirectionInt(), 0, 420);
            if (!DownedBossSystem.downedGuardians)
                shatterTimer = 0;

            if (shatterTimer == 2)
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceDoorShatterSound with { Volume = 2f });

            // Do some screen shake anticipation effects.
            if (close && DownedBossSystem.downedGuardians)
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = Utils.Remap(shatterTimer, 240f, 360f, 1f, 16f);

            // Have the door shatter into a bunch of crystals.
            if (DownedBossSystem.downedGuardians && shatterTimer >= 360f)
            {
                for (int k = 0; k < verticalOffset; k += Main.rand.Next(6, 12))
                {
                    Vector2 crystalSpawnPosition = bottom - Vector2.UnitY * k + Main.rand.NextVector2Circular(24f, 24f);
                    Vector2 crystalVelocity = -Vector2.UnitY.RotatedByRandom(1.06f) * Main.rand.NextFloat(4f, 10f);

                    if (!Collision.SolidCollision(crystalSpawnPosition, 1, 1))
                        Gore.NewGore(new EntitySource_WorldEvent(), crystalSpawnPosition, crystalVelocity, Mod.Find<ModGore>($"ProvidenceDoor{Main.rand.Next(1, 3)}").Type, 1.16f);
                }

                for (int k = 0; k < verticalOffset; k += Main.rand.Next(4, 9))
                {
                    Vector2 crystalShardSpawnPosition = bottom - Vector2.UnitY * k + Main.rand.NextVector2Circular(8f, 8f);
                    Vector2 shardVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3.6f, 13.6f);
                    Dust shard = Dust.NewDustPerfect(crystalShardSpawnPosition, 255, shardVelocity);
                    shard.noGravity = Main.rand.NextBool();
                    shard.scale = Main.rand.NextFloat(1.3f, 1.925f);
                    shard.velocity.Y -= 5f;
                }
                WorldSaveSystem.HasProvidenceDoorShattered = true;

                // Stop the loop sound.
                if (SoundEngine.TryGetActiveSound(ShimmerID, out var sound))
                    sound.Stop();

                if (Main.netMode != NetmodeID.SinglePlayer)
                    PacketManager.SendPacket<ProfanedTempleDoorOpenPacket>();
                shatterTimer = 0;
            }

            int horizontalBuffer = 32;
            Vector2 top = bottom - Vector2.UnitY * verticalOffset;
            Rectangle area = new((int)top.X - Width * 8 + horizontalBuffer / 2, (int)top.Y, Width * 16 - horizontalBuffer, verticalOffset);

            // Hurt the player if they touch the spikes.
            if (Main.LocalPlayer.Hitbox.Intersects(area))
            {
                Main.LocalPlayer.Hurt(PlayerDeathReason.ByCustomReason($"{Main.LocalPlayer.name} was somehow impaled by a pillar of crystals."), 100, 0);
                Main.LocalPlayer.AddBuff(Main.dayTime ? ModContent.BuffType<HolyFlames>() : ModContent.BuffType<Nightwither>(), 180);
            }
            if (!SoundEngine.TryGetActiveSound(ShimmerID, out var _))
            {
                if (!WorldSaveSystem.HasProvidenceDoorShattered)
                    ShimmerID = SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceDoorShimmerSoundLoop with { Volume = 0.1f }, bottom);
            }
        }

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            int xFrameOffset = Main.tile[i, j].TileFrameX;
            int yFrameOffset = Main.tile[i, j].TileFrameY;
            if (xFrameOffset != 0 || yFrameOffset != 0)
                return;

            Main.instance.TilesRenderer.AddSpecialLegacyPoint(i, j);
        }

        public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch)
        {
            if (WorldSaveSystem.HasProvidenceDoorShattered)
                return;

            Texture2D door = ModContent.Request<Texture2D>("InfernumMode/Content/Tiles/Profaned/ProvidenceRoomDoor").Value;
            Vector2 drawOffest = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawPosition = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y) + drawOffest;
            Color drawColor = Color.White;

            int verticalOffset = 0;
            for (int k = 2; k < 200; k++)
            {
                if (WorldGen.SolidTile(i, j - k))
                {
                    verticalOffset = k * 16 + 24;
                    break;
                }
            }

            for (int dy = verticalOffset; dy >= 0; dy -= 96)
            {
                Vector2 drawOffset = new(-12f, -dy - 48f);
                spriteBatch.Draw(door, drawPosition + drawOffset, null, drawColor, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            }
        }
    }
}
