using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.Ogre
{
    public class OgreStompBehaviorOverride : ProjectileBehaviorOverride
    {
        public override int ProjectileOverrideType => ProjectileID.DD2OgreSmash;
        public override ProjectileOverrideContext ContentToOverride => ProjectileOverrideContext.ProjectileAI;

        public override bool PreAI(Projectile projectile)
        {
            projectile.ai[0] += 1f;
            if (projectile.ai[0] >= 10f)
            {
                projectile.Kill();
                return false;
            }
            projectile.velocity = Vector2.Zero;
            projectile.position = projectile.Center;
            projectile.Size = new Vector2(16f, 16f) * MathHelper.Lerp(1.25f, 8f, Utils.InverseLerp(0f, 9f, projectile.ai[0]));
            projectile.Center = projectile.position;
            Point topLeftPoint = projectile.TopLeft.ToTileCoordinates();
            Point bottomRightPoint = projectile.BottomRight.ToTileCoordinates();
            if ((int)projectile.ai[0] % 3 == 0)
            {
                int lifetimeIncrement = (int)projectile.ai[0] / 3;
                for (int i = topLeftPoint.X; i <= bottomRightPoint.X; i++)
                {
                    for (int j = topLeftPoint.Y; j <= bottomRightPoint.Y; j++)
                    {
                        if (Vector2.Distance(projectile.Center, new Vector2(i * 16, j * 16)) <= projectile.width / 2)
                        {
                            Tile tile = Framing.GetTileSafely(i, j);
                            if (tile.active() && Main.tileSolid[tile.type] && !Main.tileSolidTop[tile.type] && !Main.tileFrameImportant[tile.type])
                            {
                                Tile tileAbove = Framing.GetTileSafely(i, j - 1);
                                if (!tileAbove.active() || !Main.tileSolid[tileAbove.type] || Main.tileSolidTop[tileAbove.type])
                                {
                                    int dustCount = WorldGen.KillTile_GetTileDustAmount(true, tile, i, j);
                                    for (int k = 0; k < dustCount; k++)
                                    {
                                        Dust dust = Main.dust[WorldGen.KillTile_MakeTileDust(i, j, tile)];
                                        dust.velocity.Y -= 3f + lifetimeIncrement * 1.5f;
                                        dust.velocity.Y *= Main.rand.NextFloat();
                                        dust.scale += lifetimeIncrement * 0.03f;
                                    }
                                    if (lifetimeIncrement >= 2)
                                    {
                                        for (int l = 0; l < dustCount - 1; l++)
                                        {
                                            Dust dust4 = Main.dust[WorldGen.KillTile_MakeTileDust(i, j, tile)];
                                            Dust dust5 = dust4;
                                            dust5.velocity.Y -= 1f + lifetimeIncrement;
                                            Dust dust6 = dust4;
                                            dust6.velocity.Y *= Main.rand.NextFloat();
                                        }
                                    }
                                    if (dustCount > 0 && !Main.rand.NextBool(3))
                                    {
                                        float horizontalOffsetFactor = Math.Abs((topLeftPoint.X / 2 + bottomRightPoint.X / 2) - i) / 20f;
                                        Gore gore = Gore.NewGoreDirect(projectile.position, Vector2.Zero, 61 + Main.rand.Next(3), 1f - lifetimeIncrement * 0.15f + horizontalOffsetFactor * 0.5f);
                                        gore.velocity.Y -= 0.1f + lifetimeIncrement * 0.5f + horizontalOffsetFactor * lifetimeIncrement * 1f;
                                        gore.velocity.Y *= Main.rand.NextFloat();
                                        gore.position = new Vector2(i * 16 + 20, j * 16 + 20);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
