using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Yharon
{
    public class ChargeFlare : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Big Flare");
            Main.projFrames[projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = 100;
            projectile.height = 100;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.timeLeft = 180;
            projectile.scale = 0.15f;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(projectile.localAI[0]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            projectile.localAI[0] = reader.ReadSingle();
        }

        public override void AI()
        {
            projectile.frameCounter++;
            if (projectile.frameCounter > 4)
            {
                projectile.frame++;
                projectile.frameCounter = 0;
            }
            if (projectile.frame > 3)
            {
                projectile.frame = 0;
            }

            projectile.alpha += 10;
            projectile.scale = MathHelper.Lerp(projectile.scale, 2.4f, 0.05f);

            if (!Main.dedServ)
            {
                Vector2 dustSpawnPosition = projectile.Center + Main.rand.NextVector2CircularEdge(30, 30f) * (float)Math.Pow(projectile.scale, 2f);
                Dust dust = Dust.NewDustPerfect(dustSpawnPosition, DustID.Fire);
                dust.scale = 0.2f + projectile.scale;
                dust.noGravity = true;
                dust.velocity = projectile.DirectionFrom(dustSpawnPosition) * 3f;
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, Main.DiscoG, 53, projectile.alpha);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Item, (int)projectile.Center.X, (int)projectile.Center.Y, 20);
            if (projectile.owner == Main.myPlayer)
            {
                int xTileCoords = (int)(projectile.Center.X / 16f);
                int yTileCoords = (int)(projectile.Center.Y / 16f);
                xTileCoords = Utils.Clamp(xTileCoords, 10, Main.maxTilesX - 10);
                yTileCoords = Utils.Clamp(yTileCoords, 10, Main.maxTilesY - 110);
                int spawnAreaY = Main.maxTilesY - yTileCoords;
                for (int y = yTileCoords; y < yTileCoords + spawnAreaY; y++)
                {
                    Tile tile = Main.tile[xTileCoords, y + 10];
                    if (tile is null)
                        tile = new Tile();
                    if (tile.active() && !TileID.Sets.Platforms[tile.type] && (Main.tileSolid[tile.type] || tile.liquid != 0))
                    {
                        yTileCoords = y;
                        break;
                    }
                }
                int spawnLimitY = (int)(Main.player[projectile.owner].Center.Y / 16f) + 50;
                if (yTileCoords > spawnLimitY)
                    yTileCoords = spawnLimitY;
                Projectile infernado = Projectile.NewProjectileDirect(new Vector2(xTileCoords * 16 + 8, yTileCoords * 16 - 24), Vector2.Zero, ModContent.ProjectileType<Infernado>(), 0, 4f, Main.myPlayer, 11f, 25f);
                infernado.netUpdate = true;
            }
        }
    }
}
