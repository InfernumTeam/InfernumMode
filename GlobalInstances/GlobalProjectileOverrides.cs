using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Projectiles.Enemy;
using InfernumMode.BehaviorOverrides.BossAIs.Golem;
using InfernumMode.BehaviorOverrides.BossAIs.Providence;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.GlobalInstances
{
    public class GlobalProjectileOverrides : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public float[] ExtraAI = new float[100];
        public override void SetDefaults(Projectile projectile)
        {
            for (int i = 0; i < ExtraAI.Length; i++)
            {
                ExtraAI[i] = 0f;
            }
            if (InfernumMode.CanUseCustomAIs && projectile.type == ModContent.ProjectileType<HolyAura>())
                projectile.timeLeft = ProvidenceBehaviorOverride.AuraTime;
        }

        // TODO -- This is terrible. Refactor it at some point.
        public static void GrapplingHookAIOverride(Projectile projectile)
        {
            if (projectile.aiStyle != 7)
                return;

            if (Main.player[projectile.owner].dead || Main.player[projectile.owner].stoned || Main.player[projectile.owner].webbed || Main.player[projectile.owner].frozen)
            {
                projectile.Kill();
                return;
            }
            Vector2 playerCenter = Main.player[projectile.owner].MountedCenter;
            float num70 = playerCenter.X - projectile.Center.X;
            float num71 = playerCenter.Y - projectile.Center.Y;
            float num72 = (float)Math.Sqrt((double)(num70 * num70 + num71 * num71));
            projectile.rotation = (float)Math.Atan2((double)num71, (double)num70) - 1.57f;
            if (projectile.type == 256)
            {
                projectile.rotation = (float)Math.Atan2((double)num71, (double)num70) + 3.92500019f;
            }
            if (projectile.type == 446)
            {
                Lighting.AddLight(playerCenter, 0f, 0.4f, 0.3f);
                projectile.localAI[0] += 1f;
                if (projectile.localAI[0] >= 28f)
                {
                    projectile.localAI[0] = 0f;
                }
                DelegateMethods.v3_1 = new Vector3(0f, 0.4f, 0.3f);
                Utils.PlotTileLine(projectile.Center, playerCenter, 8f, new Utils.PerLinePoint(DelegateMethods.CastLightOpen));
            }
            if (projectile.type == 652)
            {
                int num3 = projectile.frameCounter + 1;
                projectile.frameCounter = num3;
                if (num3 >= 7)
                {
                    projectile.frameCounter = 0;
                    num3 = projectile.frame + 1;
                    projectile.frame = num3;
                    if (num3 >= Main.projFrames[projectile.type])
                    {
                        projectile.frame = 0;
                    }
                }
            }
            if (projectile.type >= 646 && projectile.type <= 649)
            {
                Vector3 zero = Vector3.Zero;
                switch (projectile.type)
                {
                    case 646:
                        zero = new Vector3(0.7f, 0.5f, 0.1f);
                        break;
                    case 647:
                        zero = new Vector3(0f, 0.6f, 0.7f);
                        break;
                    case 648:
                        zero = new Vector3(0.6f, 0.2f, 0.6f);
                        break;
                    case 649:
                        zero = new Vector3(0.6f, 0.6f, 0.9f);
                        break;
                }
                Lighting.AddLight(playerCenter, zero);
                Lighting.AddLight(projectile.Center, zero);
                DelegateMethods.v3_1 = zero;
                Utils.PlotTileLine(projectile.Center, playerCenter, 8f, new Utils.PerLinePoint(DelegateMethods.CastLightOpen));
            }

            Vector2 adjustedCenter = projectile.Center - new Vector2(5f);
            NPC[] platforms = Main.npc.Take(Main.maxNPCs).Where(n => n.active && n.type == ModContent.NPCType<GolemArenaPlatform>()).OrderBy(n => projectile.Distance(n.Center)).ToArray();
            NPC[] attachedPlatforms = platforms.Where(p =>
            {
                Rectangle platformHitbox = p.Hitbox;
                platformHitbox.Inflate(16, 8);
                return Utils.CenteredRectangle(adjustedCenter, Vector2.One * 32f).Intersects(platformHitbox);
            }).ToArray();
            bool platformRequirement = attachedPlatforms.Length > 0;

            if (projectile.ai[0] == 0f)
            {
                if ((num72 > 300f && projectile.type == 13) || (num72 > 400f && projectile.type == 32) || (num72 > 440f && projectile.type == 73) || (num72 > 440f && projectile.type == 74) || (num72 > 250f && projectile.type == 165) || (num72 > 350f && projectile.type == 256) || (num72 > 500f && projectile.type == 315) || (num72 > 550f && projectile.type == 322) || (num72 > 400f && projectile.type == 331) || (num72 > 550f && projectile.type == 332) || (num72 > 400f && projectile.type == 372) || (num72 > 300f && projectile.type == 396) || (num72 > 550f && projectile.type >= 646 && projectile.type <= 649) || (num72 > 600f && projectile.type == 652) || (num72 > 480f && projectile.type >= 486 && projectile.type <= 489) || (num72 > 500f && projectile.type == 446))
                {
                    projectile.ai[0] = 1f;
                }
                else if (projectile.type >= 230 && projectile.type <= 235)
                {
                    int num73 = 300 + (projectile.type - 230) * 30;
                    if (num72 > (float)num73)
                    {
                        projectile.ai[0] = 1f;
                    }
                }
                else if (ProjectileLoader.GrappleOutOfRange(num72, projectile))
                {
                    projectile.ai[0] = 1f;
                }
                Vector2 value4 = projectile.Center + new Vector2(5f);
                Point point = (adjustedCenter - new Vector2(16f)).ToTileCoordinates();
                Point point2 = (value4 + new Vector2(32f)).ToTileCoordinates();
                int num74 = point.X;
                int num75 = point2.X;
                int num76 = point.Y;
                int num77 = point2.Y;
                if (num74 < 0)
                {
                    num74 = 0;
                }
                if (num75 > Main.maxTilesX)
                {
                    num75 = Main.maxTilesX;
                }
                if (num76 < 0)
                {
                    num76 = 0;
                }
                if (num77 > Main.maxTilesY)
                {
                    num77 = Main.maxTilesY;
                }

                int num3;
                for (int num78 = num74; num78 < num75; num78 = num3 + 1)
                {
                    int num79 = num76;
                    while (num79 < num77)
                    {
                        if (Main.tile[num78, num79] == null)
                        {
                            Main.tile[num78, num79] = new Tile();
                        }
                        Vector2 vector8;
                        vector8.X = (float)(num78 * 16);
                        vector8.Y = (float)(num79 * 16);
                        if ((adjustedCenter.X + 10f > vector8.X && adjustedCenter.X < vector8.X + 16f && adjustedCenter.Y + 10f > vector8.Y && adjustedCenter.Y < vector8.Y + 16f && Main.tile[num78, num79].nactive() && 
                            (Main.tileSolid[(int)Main.tile[num78, num79].type] || Main.tile[num78, num79].type == 314) && (projectile.type != 403 || Main.tile[num78, num79].type == 314)) || platformRequirement)
                        {
                            if (Main.player[projectile.owner].grapCount < 10)
                            {
                                Main.player[projectile.owner].grappling[Main.player[projectile.owner].grapCount] = projectile.whoAmI;
                                Player player = Main.player[projectile.owner];
                                Player arg_419E_0 = player;
                                num3 = player.grapCount;
                                arg_419E_0.grapCount = num3 + 1;
                            }
                            if (Main.myPlayer == projectile.owner)
                            {
                                int num80 = 0;
                                int num81 = -1;
                                int num82 = 100000;
                                if (projectile.type == 73 || projectile.type == 74)
                                {
                                    for (int num83 = 0; num83 < 1000; num83 = num3 + 1)
                                    {
                                        if (num83 != projectile.whoAmI && Main.projectile[num83].active && Main.projectile[num83].owner == projectile.owner && Main.projectile[num83].aiStyle == 7 && Main.projectile[num83].ai[0] == 2f)
                                        {
                                            Main.projectile[num83].Kill();
                                        }
                                        num3 = num83;
                                    }
                                }
                                else
                                {
                                    int num84 = 3;
                                    if (projectile.type == 165)
                                    {
                                        num84 = 8;
                                    }
                                    if (projectile.type == 256)
                                    {
                                        num84 = 2;
                                    }
                                    if (projectile.type == 372)
                                    {
                                        num84 = 2;
                                    }
                                    if (projectile.type == 652)
                                    {
                                        num84 = 1;
                                    }
                                    if (projectile.type >= 646 && projectile.type <= 649)
                                    {
                                        num84 = 4;
                                    }
                                    ProjectileLoader.NumGrappleHooks(projectile, Main.player[projectile.owner], ref num84);
                                    for (int num85 = 0; num85 < 1000; num85 = num3 + 1)
                                    {
                                        if (Main.projectile[num85].active && Main.projectile[num85].owner == projectile.owner && Main.projectile[num85].aiStyle == 7)
                                        {
                                            if (Main.projectile[num85].timeLeft < num82)
                                            {
                                                num81 = num85;
                                                num82 = Main.projectile[num85].timeLeft;
                                            }
                                            num3 = num80;
                                            num80 = num3 + 1;
                                        }
                                        num3 = num85;
                                    }
                                    if (num80 > num84)
                                    {
                                        Main.projectile[num81].Kill();
                                    }
                                }
                            }
                            WorldGen.KillTile(num78, num79, true, true, false);
                            SoundEngine.PlaySound(0, num78 * 16, num79 * 16, 1, 1f, 0f);
                            projectile.velocity.X = 0f;
                            projectile.velocity.Y = 0f;
                            projectile.ai[0] = 2f;
                            if (platformRequirement)
                            {
                                projectile.Center = Vector2.Lerp(projectile.Center, platforms[0].Center, 0.7f);
                            }
                            else
                            {
                                projectile.position.X = (float)(num78 * 16 + 8 - projectile.width / 2);
                                projectile.position.Y = (float)(num79 * 16 + 8 - projectile.height / 2);
                            }

                            projectile.damage = 0;
                            projectile.netUpdate = true;
                            if (Main.myPlayer == projectile.owner)
                            {
                                NetMessage.SendData(13, -1, -1, null, projectile.owner, 0f, 0f, 0f, 0, 0, 0);
                                break;
                            }
                            break;
                        }
                        else
                        {
                            num3 = num79;
                            num79 = num3 + 1;
                        }
                    }
                    if (projectile.ai[0] == 2f)
                    {
                        return;
                    }
                    num3 = num78;
                }
                return;
            }
            if (projectile.ai[0] == 1f)
            {
                float num86 = 11f;
                if (projectile.type == 32)
                {
                    num86 = 15f;
                }
                if (projectile.type == 73 || projectile.type == 74)
                {
                    num86 = 17f;
                }
                if (projectile.type == 315)
                {
                    num86 = 20f;
                }
                if (projectile.type == 322)
                {
                    num86 = 22f;
                }
                if (projectile.type >= 230 && projectile.type <= 235)
                {
                    num86 = 11f + (float)(projectile.type - 230) * 0.75f;
                }
                if (projectile.type == 446)
                {
                    num86 = 20f;
                }
                if (projectile.type >= 486 && projectile.type <= 489)
                {
                    num86 = 18f;
                }
                if (projectile.type >= 646 && projectile.type <= 649)
                {
                    num86 = 24f;
                }
                if (projectile.type == 652)
                {
                    num86 = 24f;
                }
                if (projectile.type == 332)
                {
                    num86 = 17f;
                }
                ProjectileLoader.GrappleRetreatSpeed(projectile, Main.player[projectile.owner], ref num86);
                if (num72 < 24f)
                {
                    projectile.Kill();
                }
                num72 = num86 / num72;
                num70 *= num72;
                num71 *= num72;
                projectile.velocity.X = num70;
                projectile.velocity.Y = num71;
                return;
            }
            if (projectile.ai[0] == 2f)
            {
                int num87 = (int)(projectile.position.X / 16f) - 1;
                int num88 = (int)((projectile.position.X + (float)projectile.width) / 16f) + 2;
                int num89 = (int)(projectile.position.Y / 16f) - 1;
                int num90 = (int)((projectile.position.Y + (float)projectile.height) / 16f) + 2;
                if (num87 < 0)
                {
                    num87 = 0;
                }
                if (num88 > Main.maxTilesX)
                {
                    num88 = Main.maxTilesX;
                }
                if (num89 < 0)
                {
                    num89 = 0;
                }
                if (num90 > Main.maxTilesY)
                {
                    num90 = Main.maxTilesY;
                }
                bool flag2 = true;
                int num3;
                for (int num91 = num87; num91 < num88; num91 = num3 + 1)
                {
                    for (int num92 = num89; num92 < num90; num92 = num3 + 1)
                    {
                        if (Main.tile[num91, num92] == null)
                        {
                            Main.tile[num91, num92] = new Tile();
                        }
                        Vector2 vector9;
                        vector9.X = (float)(num91 * 16);
                        vector9.Y = (float)(num92 * 16);
                        if (projectile.position.X + (float)(projectile.width / 2) > vector9.X && projectile.position.X + (float)(projectile.width / 2) < vector9.X + 16f && projectile.position.Y + (float)(projectile.height / 2) > vector9.Y && projectile.position.Y + (float)(projectile.height / 2) < vector9.Y + 16f && Main.tile[num91, num92].nactive() && (Main.tileSolid[(int)Main.tile[num91, num92].type] || Main.tile[num91, num92].type == 314 || Main.tile[num91, num92].type == 5))
                        {
                            flag2 = false;
                        }
                        num3 = num92;
                    }
                    num3 = num91;
                }
                if (platformRequirement)
                {
                    Main.player[projectile.owner].position += attachedPlatforms[0].velocity;
                    projectile.position += attachedPlatforms[0].velocity;
                }
                if (flag2 && !platformRequirement)
                {
                    projectile.ai[0] = 1f;
                    return;
                }
                if (Main.player[projectile.owner].grapCount < 10)
                {
                    Main.player[projectile.owner].grappling[Main.player[projectile.owner].grapCount] = projectile.whoAmI;
                    Player player = Main.player[projectile.owner];
                    Player arg_484B_0 = player;
                    num3 = player.grapCount;
                    arg_484B_0.grapCount = num3 + 1;
                    return;
                }
            }
        }

        public override bool PreAI(Projectile projectile)
        {
            if (projectile.aiStyle == 7)
            {
                GrapplingHookAIOverride(projectile);
                return false;
            }

            if (InfernumMode.CanUseCustomAIs)
            {
                if (OverridingListManager.InfernumProjectilePreAIOverrideList.ContainsKey(projectile.type))
                    return (bool)OverridingListManager.InfernumProjectilePreAIOverrideList[projectile.type].DynamicInvoke(projectile);
            }
            return base.PreAI(projectile);
        }

        public override bool PreDraw(Projectile projectile, SpriteBatch spriteBatch, Color lightColor)
        {
            if (InfernumMode.CanUseCustomAIs && projectile.type == ModContent.ProjectileType<HolyAura>())
            {
                Texture2D texture = Main.projectileTexture[projectile.type];
                float clampedTime = Main.GlobalTime % 5f / 5f;
                Vector2 origin = texture.Size() / 2f;
                Vector2 baseDrawPosition = projectile.Center - Main.screenPosition;
                int totalAurasToDraw = 32;
                float[] posX = new float[totalAurasToDraw];
                float[] posY = new float[totalAurasToDraw];
                float[] hue = new float[totalAurasToDraw];
                float[] size = new float[totalAurasToDraw];
                float sizeScale = 0.8f;
                float sizeScalar = (1f - sizeScale) / totalAurasToDraw;
                float yPosOffset = 60f;
                float xPosOffset = 400f;
                Vector2 scale = new(8f, 6f);

                for (int i = 0; i < totalAurasToDraw; i++)
                {
                    float oscillatingTime = (float)Math.Cos(clampedTime * MathHelper.TwoPi + i / 2f);

                    posX[i] = oscillatingTime * (xPosOffset - i * 3f);

                    posY[i] = (float)Math.Sin(clampedTime * MathHelper.TwoPi + MathHelper.Pi / 3f + i) * yPosOffset;
                    posY[i] -= i * 3f;

                    hue[i] = (i / (float)totalAurasToDraw * 2f) % 1f;

                    size[i] = sizeScale + (i + 1) * sizeScalar;
                    size[i] *= 0.3f;

                    Color color = Main.hslToRgb(i / (float)totalAurasToDraw, 1f, 0.5f);
                    if (!Main.dayTime)
                        color = Color.Lerp(Color.Cyan, Color.Indigo, i / (float)totalAurasToDraw);

                    color.A = 24;

                    int fadeTime = 30;
                    if (projectile.timeLeft < fadeTime)
                    {
                        float fadeCompletion = projectile.timeLeft / (float)fadeTime;

                        color.A = (byte)MathHelper.Lerp(0, color.A, fadeCompletion);
                    }
                    color *= Utils.InverseLerp(0f, 25f, projectile.timeLeft, true);

                    float rotation = MathHelper.PiOver2 + oscillatingTime * MathHelper.PiOver4 * -0.3f + MathHelper.Pi * i;

                    for (int j = 0; j < 2; j++)
                    {
                        spriteBatch.Draw(texture, baseDrawPosition + new Vector2(posX[i], posY[i]), null, color, rotation, origin, new Vector2(size[i]) * scale, SpriteEffects.None, 0);
                        spriteBatch.Draw(texture, baseDrawPosition + new Vector2(posX[i], posY[i]), null, color, rotation, origin, new Vector2(size[i]) * scale, SpriteEffects.FlipVertically, 0);
                    }
                }

                return false;
            }
            return base.PreDraw(projectile, spriteBatch, lightColor);
        }

        public override bool PreKill(Projectile projectile, int timeLeft)
        {
            if (projectile.type == ModContent.ProjectileType<HolyBlast>())
            {
                if (projectile.owner == Main.myPlayer)
                {
                    Vector2 shootFromVector = new(projectile.Center.X, projectile.Center.Y);
                    float spread = MathHelper.PiOver2;
                    float startAngle = projectile.velocity.ToRotation() - spread / 2;
                    float deltaAngle = spread / 4f;
                    float offsetAngle;
                    for (int i = 0; i < 2; i++)
                    {
                        offsetAngle = startAngle + deltaAngle * (i + i * i) / 2f + 32f * i;
                        Projectile.NewProjectile(shootFromVector, offsetAngle.ToRotationVector2() * 5f, ModContent.ProjectileType<HolyFire2>(), projectile.damage, 0f, Main.myPlayer);
                        Projectile.NewProjectile(shootFromVector, offsetAngle.ToRotationVector2() * -5f, ModContent.ProjectileType<HolyFire2>(), projectile.damage, 0f, Main.myPlayer);
                    }
                }
                SoundEngine.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ProvidenceHolyBlastImpact"), projectile.Center);
                int dustType = (int)CalamityDusts.ProfanedFire;

                for (int i = 0; i < 6; i++)
                    Dust.NewDust(projectile.position, projectile.width, projectile.height, dustType, 0f, 0f, 50, default, 1.5f);

                for (int i = 0; i < 60; i++)
                {
                    Dust fire = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, dustType, 0f, 0f, 0, default, 2.5f);
                    fire.noGravity = true;
                    fire.velocity *= 3f;

                    fire = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, dustType, 0f, 0f, 50, default, 1.5f);
                    fire.velocity *= 2f;
                    fire.noGravity = true;
                }
                return false;
            }
            return base.PreKill(projectile, timeLeft);
        }

        public override bool OnTileCollide(Projectile projectile, Vector2 oldVelocity)
        {
            if (InfernumMode.CanUseCustomAIs)
            {
                if (projectile.type == ProjectileID.PhantasmalSphere)
                {
                    if (projectile.velocity.X != oldVelocity.X)
                    {
                        projectile.velocity.X = -oldVelocity.X * 0.55f;
                    }
                    if (projectile.velocity.Y != oldVelocity.Y)
                    {
                        projectile.velocity.Y = -oldVelocity.Y * 0.55f;
                    }
                    return false;
                }
            }
            return base.OnTileCollide(projectile, oldVelocity);
        }

        public override void ModifyHitPlayer(Projectile projectile, Player target, ref int damage, ref bool crit)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return;

            if (projectile.type == ModContent.ProjectileType<CrimsonSpike>())
                target.AddBuff(ModContent.BuffType<BurningBlood>(), 180);
            if (projectile.type == ModContent.ProjectileType<IchorShot>())
                target.AddBuff(ModContent.BuffType<BurningBlood>(), 120);
        }

        public override bool CanHitPlayer(Projectile projectile, Player target)
        {
            if (InfernumMode.CanUseCustomAIs)
            {
                if (projectile.type == ProjectileID.PhantasmalSphere)
                    return projectile.Infernum().ExtraAI[0] > 40f;
                if (projectile.type == ProjectileID.PhantasmalBolt)
                    return projectile.Infernum().ExtraAI[0] > 40f;
            }
            return base.CanHitPlayer(projectile, target);
        }
    }
}