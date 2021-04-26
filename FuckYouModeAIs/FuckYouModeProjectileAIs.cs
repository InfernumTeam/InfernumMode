using CalamityMod.Dusts;
using CalamityMod.Projectiles.Boss;
using InfernumMode.FuckYouModeAIs.MoonLord;
using InfernumMode.FuckYouModeAIs.Providence;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace InfernumMode.FuckYouModeAIs.MainAI
{
    public class FuckYouModeProjectileAIs : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public float[] ExtraAI = new float[100];
        public override void SetDefaults(Projectile projectile)
        {
            for (int i = 0; i < ExtraAI.Length; i++)
            {
                ExtraAI[i] = 0f;
            }
            if (PoDWorld.InfernumMode && projectile.type == ModContent.ProjectileType<HolyAura>())
                projectile.timeLeft = ProvidenceAIClass.AuraTime;
        }

        [OverrideAppliesTo(ProjectileID.PhantasmalEye, typeof(FuckYouModeProjectileAIs), "PhantasmalEyeAI", EntityOverrideContext.ProjectileAI)]
        public static bool PhantasmalEyeAI(Projectile projectile)
        {
            projectile.alpha -= 40;
            if (projectile.alpha < 0)
            {
                projectile.alpha = 0;
            }
            projectile.rotation = projectile.velocity.ToRotation() + 1.57079637f;
            projectile.tileCollide = projectile.localAI[0] >= 70f;
            projectile.localAI[0] += 1f;
            if (projectile.localAI[0] < 50f)
            {
                projectile.velocity.X = projectile.velocity.RotatedBy(projectile.ai[1]).X;
                projectile.velocity.X = (projectile.velocity * 40f + projectile.DirectionTo(Main.player[Player.FindClosest(projectile.Center, 1, 1)].Center) * 6).X / 41f;
                projectile.velocity.Y -= 0.07f;
            }
            else if (projectile.localAI[0] >= 50f)
            {
                projectile.velocity.X *= 0.95f;
                projectile.velocity.Y += 0.14f;
            }
            return false;
        }

        [OverrideAppliesTo(ProjectileID.PhantasmalBolt, typeof(FuckYouModeProjectileAIs), "PhantasmalBoltAI", EntityOverrideContext.ProjectileAI)]
        public static bool PhantasmalBoltAI(Projectile projectile)
        {
            projectile.Infernum().ExtraAI[5] += 1f;
            if (projectile.localAI[0] == 0f)
            {
                if (Main.rand.Next(2) == 0)
                    Main.PlaySound(SoundID.Item124, projectile.position);
                else
                    Main.PlaySound(SoundID.Item125, projectile.position);
                projectile.localAI[0] = 1f;
            }
            projectile.alpha -= 40;
            if (projectile.alpha < 0)
            {
                projectile.alpha = 0;
            }
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            projectile.velocity = Vector2.Clamp(projectile.velocity, new Vector2(-10f), new Vector2(10f));
            // Has 3 extra updates
            projectile.timeLeft = (int)MathHelper.Min(250 * 3, projectile.timeLeft);

            if (!NPC.AnyNPCs(NPCID.MoonLordCore))
            {
                projectile.active = false;
                return false;
            }
            NPC core = Main.npc[NPC.FindFirstNPC(NPCID.MoonLordCore)];
            projectile.tileCollide = projectile.Hitbox.Intersects(core.Infernum().arenaRectangle);

            int num60 = Dust.NewDust(projectile.Center, 0, 0, 229, 0f, 0f, 100, default, 1f);
            Main.dust[num60].noLight = true;
            Main.dust[num60].noGravity = true;
            Main.dust[num60].velocity = projectile.velocity;
            Dust dust3 = Main.dust[num60];
            dust3.position -= Vector2.One * 4f;
            Main.dust[num60].scale = 0.8f;
            int num = projectile.frameCounter + 1;
            projectile.frameCounter = num;
            if (num >= 9)
            {
                projectile.frameCounter = 0;
                num = projectile.frame + 1;
                projectile.frame = num;
                if (num >= 5)
                {
                    projectile.frame = 0;
                }
            }
            return false;
        }

        [OverrideAppliesTo(ProjectileID.PhantasmalSphere, typeof(FuckYouModeProjectileAIs), "PhantasmalSphereAI", EntityOverrideContext.ProjectileAI)]
        public static bool PhantasmalSphereAI(Projectile projectile)
        {
            projectile.Infernum().ExtraAI[5] += 1f;
            if (projectile.alpha > 200)
            {
                projectile.alpha = 200;
            }
            projectile.alpha -= 5;
            if (projectile.alpha < 0)
            {
                projectile.alpha = 0;
            }
            float num791 = projectile.alpha / 255f;
            projectile.scale = 1f - num791;
            projectile.tileCollide = projectile.scale >= 1f;
            if (projectile.ai[0] >= 0f)
            {
                projectile.ai[0] += 1f;
            }
            if (projectile.ai[0] == -1f)
            {
                projectile.frame = 1;
                projectile.extraUpdates = 1;
            }
            else if (projectile.ai[0] < 30f)
            {
                projectile.position = Main.npc[(int)projectile.ai[1]].Center - new Vector2(projectile.width, projectile.height) / 2f - projectile.velocity;
            }
            else
            {
                int num3 = projectile.frameCounter + 1;
                projectile.frameCounter = num3;
                if (num3 >= 6)
                {
                    projectile.frameCounter = 0;
                    num3 = projectile.frame + 1;
                    projectile.frame = num3;
                    if (num3 >= 2)
                    {
                        projectile.frame = 0;
                    }
                }
            }
            if (projectile.alpha < 40)
            {
                int num3;
                for (int num792 = 0; num792 < 2; num792 = num3 + 1)
                {
                    float num793 = (float)Main.rand.NextDouble() * 1f - 0.5f;
                    if (num793 < -0.5f)
                    {
                        num793 = -0.5f;
                    }
                    if (num793 > 0.5f)
                    {
                        num793 = 0.5f;
                    }
                    Vector2 value20 = new Vector2(-projectile.width * 0.65f * projectile.scale, 0f).RotatedBy(num793 * MathHelper.TwoPi, default).RotatedBy(projectile.velocity.ToRotation(), default);
                    int num794 = Dust.NewDust(projectile.Center - Vector2.One * 5f, 10, 10, 229, -projectile.velocity.X / 3f, -projectile.velocity.Y / 3f, 150, Color.Transparent, 0.7f);
                    Main.dust[num794].velocity = Vector2.Zero;
                    Main.dust[num794].position = projectile.Center + value20;
                    Main.dust[num794].noGravity = true;
                    num3 = num792;
                }
                return false;
            }
            return false;
        }

        [OverrideAppliesTo(ProjectileID.PhantasmalDeathray, typeof(FuckYouModeProjectileAIs), "PhantasmalDeathrayAI", EntityOverrideContext.ProjectileAI)]
        public static bool PhantasmalDeathrayAI(Projectile projectile)
        {
            Vector2? vector78 = null;

            if (projectile.velocity.HasNaNs() || projectile.velocity == Vector2.Zero)
            {
                projectile.velocity = -Vector2.UnitY;
            }

            if (Main.npc[(int)projectile.ai[1]].active && Main.npc[(int)projectile.ai[1]].type == NPCID.MoonLordHead)
            {
                Vector2 value21 = new Vector2(27f, 59f);
                Vector2 value22 = Utils.Vector2FromElipse(Main.npc[(int)projectile.ai[1]].localAI[0].ToRotationVector2(), value21 * Main.npc[(int)projectile.ai[1]].localAI[1]);
                projectile.position = Main.npc[(int)projectile.ai[1]].Center + value22 - new Vector2(projectile.width, projectile.height) / 2f;
            }
            else projectile.Kill();

            if (projectile.velocity.HasNaNs() || projectile.velocity == Vector2.Zero)
            {
                projectile.velocity = -Vector2.UnitY;
            }

            if (projectile.localAI[0] == 0f)
            {
                Main.PlaySound(SoundID.Zombie, (int)projectile.position.X, (int)projectile.position.Y, 104, 1f, 0f);
            }

            float num801 = 1f;
            projectile.localAI[0] += 1f;
            if (projectile.localAI[0] >= 180f)
            {
                projectile.Kill();
                return false;
            }

            projectile.scale = (float)Math.Sin(projectile.localAI[0] * MathHelper.Pi / 180f) * 10f * num801;
            if (projectile.scale > num801)
            {
                projectile.scale = num801;
            }

            float rotationalAcceleration = projectile.velocity.ToRotation();
            rotationalAcceleration += projectile.ai[0];
            projectile.rotation = rotationalAcceleration - MathHelper.PiOver2;
            projectile.velocity = rotationalAcceleration.ToRotationVector2();

            float num805 = 3f;
            float num806 = projectile.width;

            Vector2 samplingPoint = projectile.Center;
            if (vector78.HasValue)
            {
                samplingPoint = vector78.Value;
            }

            float[] array3 = new float[(int)num805];
            const float tryLaserLength = 2900f;
            Collision.LaserScan(samplingPoint, projectile.velocity, num806 * projectile.scale, tryLaserLength, array3);
            float laserLength = 0f;
            int num3;
            for (int num808 = 0; num808 < array3.Length; num808 = num3 + 1)
            {
                laserLength += array3[num808];
                num3 = num808;
            }
            laserLength /= num805;

            float amount = 0.5f;
            projectile.localAI[1] = MathHelper.Lerp(projectile.localAI[1], tryLaserLength, amount);
            Vector2 laserEndPoint = projectile.Center + projectile.velocity * (projectile.localAI[1] - 20f);

            if (projectile.localAI[0] % 35 == 34)
            {
                for (int k = 0; k < 8; k++)
                {
                    Vector2 velocity = (MathHelper.TwoPi / 8f * k).ToRotationVector2() * Main.rand.NextFloat(4f, 7f);
                    velocity = velocity.RotatedByRandom(MathHelper.ToRadians(7f));
                    Dust.NewDust(laserEndPoint, projectile.width, projectile.height, 229, velocity.X, velocity.Y, 0, default, 1f);
                }
                for (int i = 0; i < 4; i++)
                {
                    float angle = MathHelper.ToRadians(Main.rand.NextFloat(21f, 32f)) * (i - 2f) / 2f;
                    Projectile.NewProjectile(laserEndPoint, new Vector2(0f, -6f).RotatedBy(angle), ModContent.ProjectileType<PhantasmalSpark>(), 39, 1f);
                }
            }

            for (int num809 = 0; num809 < 2; num809 = num3 + 1)
            {
                float num810 = projectile.velocity.ToRotation() + ((Main.rand.Next(2) == 1) ? -1f : 1f) * 1.57079637f;
                float num811 = (float)Main.rand.NextDouble() * 2f + 2f;
                Vector2 vector80 = new Vector2((float)Math.Cos(num810) * num811, (float)Math.Sin(num810) * num811);
                int num812 = Dust.NewDust(laserEndPoint, 0, 0, 229, vector80.X, vector80.Y, 0, default, 1f);
                Main.dust[num812].noGravity = true;
                Main.dust[num812].scale = 1.7f;
                num3 = num809;
            }
            if (Main.rand.Next(5) == 0)
            {
                Vector2 value29 = projectile.velocity.RotatedBy(1.5707963705062866, default) * ((float)Main.rand.NextDouble() - 0.5f) * projectile.width;
                int num813 = Dust.NewDust(laserEndPoint + value29 - Vector2.One * 4f, 8, 8, 31, 0f, 0f, 100, default, 1.5f);
                Dust dust = Main.dust[num813];
                dust.velocity *= 0.5f;
                Main.dust[num813].velocity.Y = -Math.Abs(Main.dust[num813].velocity.Y);
            }
            DelegateMethods.v3_1 = new Vector3(0.3f, 0.65f, 0.7f);
            Utils.PlotTileLine(projectile.Center, projectile.Center + projectile.velocity * projectile.localAI[1], projectile.width * projectile.scale, new Utils.PerLinePoint(DelegateMethods.CastLight));

            return false;
        }
        public override bool PreAI(Projectile projectile)
        {
            if (PoDWorld.InfernumMode)
            {
                if (OverridingListManager.InfernumProjectilePreAIOverrideList.ContainsKey(projectile.type))
                {
                    if (OverridingListManager.ExclusionList.ContainsKey(new OverrideExclusionContext(projectile.type, EntityOverrideContext.ProjectileAI)) &&
                        !OverridingListManager.ExclusionList[new OverrideExclusionContext(projectile.type, EntityOverrideContext.ProjectileAI)].Invoke())
					{
                        return base.PreAI(projectile);
					}
                    return (bool)OverridingListManager.InfernumProjectilePreAIOverrideList[projectile.type].DynamicInvoke(projectile);
                }
            }
            return base.PreAI(projectile);
        }

		public override bool PreDraw(Projectile projectile, SpriteBatch spriteBatch, Color lightColor)
		{
            if (PoDWorld.InfernumMode && projectile.type == ModContent.ProjectileType<HolyAura>())
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
                Vector2 scale = new Vector2(8f, 6f);

                for (int i = 0; i < totalAurasToDraw; i++)
                {
                    float oscillatingTime = (float)Math.Sin(clampedTime * MathHelper.TwoPi + MathHelper.PiOver2 + i / 2f);

                    posX[i] = oscillatingTime * (xPosOffset - i * 3f);

                    posY[i] = (float)Math.Sin(clampedTime * MathHelper.TwoPi * 2f + MathHelper.Pi / 3f + i) * yPosOffset;
                    posY[i] -= i * 3f;

                    hue[i] = (i / (float)totalAurasToDraw * 2f) % 1f;

                    size[i] = sizeScale + (i + 1) * sizeScalar;
                    size[i] *= 0.3f;

                    Color color = Main.hslToRgb(i / (float)totalAurasToDraw, 1f, 0.5f);
                    color *= 1.8f;
                    color.A /= 2;

                    int fadeTime = 30;
                    if (projectile.timeLeft < fadeTime)
                    {
                        float fadeCompletion = projectile.timeLeft / (float)fadeTime;

                        if (color.R > 0)
                            color.R = (byte)MathHelper.Lerp(0, color.R, fadeCompletion);
                        if (color.G > 0)
                            color.G = (byte)MathHelper.Lerp(0, color.G, fadeCompletion);
                        if (color.B > 0)
                            color.B = (byte)MathHelper.Lerp(0, color.B, fadeCompletion);

                        color.A = (byte)MathHelper.Lerp(0, color.A, fadeCompletion);
                    }

                    float rotation = MathHelper.PiOver2 + oscillatingTime * MathHelper.PiOver4 * -0.3f + MathHelper.Pi * i;

                    spriteBatch.Draw(texture, baseDrawPosition + new Vector2(posX[i], posY[i]), null, color, rotation, origin, new Vector2(size[i]) * scale, SpriteEffects.None, 0);
                    spriteBatch.Draw(texture, baseDrawPosition + new Vector2(posX[i], posY[i]), null, color, rotation, origin, new Vector2(size[i]) * scale, SpriteEffects.FlipVertically, 0);
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
                    Vector2 shootFromVector = new Vector2(projectile.Center.X, projectile.Center.Y);
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
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ProvidenceHolyBlastImpact"), projectile.Center);
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

        public override void Kill(Projectile projectile, int timeLeft)
        {
            if (projectile.type == ProjectileID.PhantasmalEye && projectile.localAI[0] >= 70f && PoDWorld.InfernumMode)
            {
                for (int k = 0; k < 25; k++)
                {
                    Vector2 velocity = (MathHelper.TwoPi / 25f * k).ToRotationVector2() * Main.rand.NextFloat(8f, 15f);
                    velocity = velocity.RotatedByRandom(MathHelper.ToRadians(7f));
                    Dust.NewDust(projectile.position, projectile.width, projectile.height, 229, velocity.X, velocity.Y, 0, default, 1f);
                }
                float angle = MathHelper.ToRadians(30f);
                Projectile.NewProjectile(projectile.Top, new Vector2(0f, -4f).RotatedByRandom(angle), ModContent.ProjectileType<PhantasmalSpark>(), 39, 1f);
            }
        }
        public override bool OnTileCollide(Projectile projectile, Vector2 oldVelocity)
        {
            if (PoDWorld.InfernumMode)
            {
                if (projectile.type == ProjectileID.PhantasmalBolt)
                {
                    if (projectile.velocity.X != oldVelocity.X)
                    {
                        projectile.velocity.X = -oldVelocity.X;
                    }
                    if (projectile.velocity.Y != oldVelocity.Y)
                    {
                        projectile.velocity.Y = -oldVelocity.Y;
                    }
                    return false;
                }
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
        public override bool CanHitPlayer(Projectile projectile, Player target)
        {
            if (PoDWorld.InfernumMode)
            {
                if (projectile.type == ProjectileID.PhantasmalSphere)
                {
                    return projectile.Infernum().ExtraAI[5] > 70f;
                }
                if (projectile.type == ProjectileID.PhantasmalBolt)
                {
                    return projectile.Infernum().ExtraAI[5] > 70f;
                }
            }
            return base.CanHitPlayer(projectile, target);
        }
    }
}