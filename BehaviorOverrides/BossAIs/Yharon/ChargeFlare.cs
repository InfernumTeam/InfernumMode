using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Yharon
{
    public class ChargeFlare : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Big Flare");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 100;
            Projectile.height = 100;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.scale = 0.15f;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > 3)
            {
                Projectile.frame = 0;
            }

            Projectile.alpha += 10;
            Projectile.scale = MathHelper.Lerp(Projectile.scale, 2.4f, 0.05f);

            if (!Main.dedServ)
            {
                Vector2 dustSpawnPosition = Projectile.Center + Main.rand.NextVector2CircularEdge(30, 30f) * (float)Math.Pow(Projectile.scale, 2f);
                Dust dust = Dust.NewDustPerfect(dustSpawnPosition, DustID.Fire);
                dust.scale = 0.2f + Projectile.scale;
                dust.noGravity = true;
                dust.velocity = Projectile.DirectionFrom(dustSpawnPosition) * 3f;
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, Main.DiscoG, 53, Projectile.alpha);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item, (int)Projectile.Center.X, (int)Projectile.Center.Y, 20);
            if (Projectile.owner == Main.myPlayer)
            {
                int xTileCoords = (int)(Projectile.Center.X / 16f);
                int yTileCoords = (int)(Projectile.Center.Y / 16f);
                xTileCoords = Utils.Clamp(xTileCoords, 10, Main.maxTilesX - 10);
                yTileCoords = Utils.Clamp(yTileCoords, 10, Main.maxTilesY - 110);
                int spawnAreaY = Main.maxTilesY - yTileCoords;
                for (int y = yTileCoords; y < yTileCoords + spawnAreaY; y++)
                {
                    Tile tile = Main.tile[xTileCoords, y + 10];
                    if (tile is null)
                        tile = new Tile();
                    if (tile.HasTile && !TileID.Sets.Platforms[tile.TileType] && (Main.tileSolid[tile.TileType] || tile.LiquidAmount != 0))
                    {
                        yTileCoords = y;
                        break;
                    }
                }
                int spawnLimitY = (int)(Main.player[Projectile.owner].Center.Y / 16f) + 50;
                if (yTileCoords > spawnLimitY)
                    yTileCoords = spawnLimitY;
                Projectile infernado = Projectile.NewProjectileDirect(new Vector2(xTileCoords * 16 + 8, yTileCoords * 16 - 24), Vector2.Zero, ModContent.ProjectileType<Infernado>(), 0, 4f, Main.myPlayer, 11f, 25f);
                infernado.netUpdate = true;
            }
        }
    }
}
