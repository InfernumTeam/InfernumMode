using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Projectiles.Enemy;
using CalamityMod.Projectiles.Melee;
using InfernumMode.BehaviorOverrides.BossAIs.Providence;
using InfernumMode.OverridingSystem;
using InfernumMode.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
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
            if (projectile.ModProjectile?.Mod.Name == Mod.Name)
                ProjectileID.Sets.DrawScreenCheckFluff[projectile.type] = 20000;

            for (int i = 0; i < ExtraAI.Length; i++)
            {
                ExtraAI[i] = 0f;
            }
            if (InfernumMode.CanUseCustomAIs && projectile.type == ModContent.ProjectileType<HolyAura>())
                projectile.timeLeft = ProvidenceBehaviorOverride.AuraTime;
        }
        
        public override bool PreAI(Projectile projectile)
        {
            if (InfernumMode.CanUseCustomAIs)
            {
                if (OverridingListManager.InfernumProjectilePreAIOverrideList.ContainsKey(projectile.type))
                    return (bool)OverridingListManager.InfernumProjectilePreAIOverrideList[projectile.type].DynamicInvoke(projectile);
            }

            // No tombs.
            // h.
            bool isTomb = projectile.type is ProjectileID.Tombstone or ProjectileID.Gravestone or ProjectileID.RichGravestone1 or ProjectileID.RichGravestone2 or 
                ProjectileID.RichGravestone3 or ProjectileID.RichGravestone4 or ProjectileID.RichGravestone4 or ProjectileID.Headstone;

            bool illegalRocket = projectile.type is ProjectileID.DryRocket or ProjectileID.DryGrenade or ProjectileID.DryMine or ProjectileID.DrySnowmanRocket;
            illegalRocket |= projectile.type is ProjectileID.WetRocket or ProjectileID.WetGrenade or ProjectileID.WetMine or ProjectileID.WetBomb or ProjectileID.WetSnowmanRocket;
            illegalRocket |= projectile.type is ProjectileID.HoneyRocket or ProjectileID.HoneyGrenade or ProjectileID.HoneyMine or ProjectileID.HoneyBomb or ProjectileID.HoneySnowmanRocket;
            illegalRocket |= projectile.type is ProjectileID.LavaRocket or ProjectileID.LavaGrenade or ProjectileID.LavaMine or ProjectileID.LavaBomb or ProjectileID.LavaSnowmanRocket;
            illegalRocket |= projectile.type is ProjectileID.DirtBomb or ProjectileID.DirtStickyBomb;
            if (illegalRocket && new Rectangle((int)projectile.Center.X / 16, (int)projectile.Center.Y / 16, 4, 4).Intersects(WorldSaveSystem.ProvidenceArena))
                projectile.active = false;
            if (projectile.type == ModContent.ProjectileType<CrystylCrusherRay>() && new Rectangle((int)Main.MouseWorld.X / 16, (int)Main.MouseWorld.Y / 16, 4, 4).Intersects(WorldSaveSystem.ProvidenceArena))
                projectile.active = false;

            if (isTomb)
                projectile.active = false;

            return base.PreAI(projectile);
        }

        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            if (InfernumMode.CanUseCustomAIs && projectile.type == ModContent.ProjectileType<HolyAura>() && !Main.dayTime)
            {
                Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
                float clampedTime = Main.GlobalTimeWrappedHourly % 5f / 5f;
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

                    Color color = Color.Lerp(Color.Cyan, Color.Indigo, i / (float)totalAurasToDraw);
                    color.A = 24;

                    int fadeTime = 30;
                    if (projectile.timeLeft < fadeTime)
                    {
                        float fadeCompletion = projectile.timeLeft / (float)fadeTime;

                        color.A = (byte)MathHelper.Lerp(0, color.A, fadeCompletion);
                    }
                    color *= Utils.GetLerpValue(0f, 25f, projectile.timeLeft, true);

                    float rotation = MathHelper.PiOver2 + oscillatingTime * MathHelper.PiOver4 * -0.3f + MathHelper.Pi * i;

                    for (int j = 0; j < 2; j++)
                    {
                        Main.spriteBatch.Draw(texture, baseDrawPosition + new Vector2(posX[i], posY[i]), null, color, rotation, origin, new Vector2(size[i]) * scale, SpriteEffects.None, 0);
                        Main.spriteBatch.Draw(texture, baseDrawPosition + new Vector2(posX[i], posY[i]), null, color, rotation, origin, new Vector2(size[i]) * scale, SpriteEffects.FlipVertically, 0);
                    }
                }

                return false;
            }

            if (InfernumMode.CanUseCustomAIs)
            {
                if (Main.LocalPlayer.Calamity().trippy)
                {
                    SpriteEffects direction = SpriteEffects.None;
                    if (projectile.spriteDirection == 1)
                        direction = SpriteEffects.FlipHorizontally;

                    Color shroomColor = projectile.GetAlpha(new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB, 0));
                    float colorFadeFactor = 0.99f;
                    shroomColor.R = (byte)(shroomColor.R * colorFadeFactor);
                    shroomColor.G = (byte)(shroomColor.G * colorFadeFactor);
                    shroomColor.B = (byte)(shroomColor.B * colorFadeFactor);
                    shroomColor.A = (byte)(shroomColor.A * colorFadeFactor);
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 drawPosition = projectile.Center;
                        float horizontalOffset = Math.Abs(projectile.Center.X - Main.LocalPlayer.Center.X);
                        float verticalOffset = Math.Abs(projectile.Center.Y - Main.LocalPlayer.Center.Y);

                        if (i is 0 or 2)
                            drawPosition.X = Main.LocalPlayer.Center.X + horizontalOffset;
                        else
                            drawPosition.X = Main.LocalPlayer.Center.X - horizontalOffset;

                        if (i is 0 or 1)
                            drawPosition.Y = Main.LocalPlayer.Center.Y + verticalOffset;
                        else
                            drawPosition.Y = Main.LocalPlayer.Center.Y - verticalOffset;

                        Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
                        int frames = texture.Height / Main.projFrames[projectile.type];
                        int y = frames * projectile.frame;
                        drawPosition.Y -= projectile.height / 2;
                        Rectangle frame = new(0, y, texture.Width, frames);
                        Vector2 origin = frame.Size() * 0.5f;

                        Main.spriteBatch.Draw(texture,
                            drawPosition - Main.screenPosition + Vector2.UnitY * origin * 0.5f,
                            frame, shroomColor, projectile.rotation, origin, projectile.scale, direction, 0);
                    }
                }
                if (OverridingListManager.InfernumProjectilePreDrawOverrideList.ContainsKey(projectile.type))
                    return (bool)OverridingListManager.InfernumProjectilePreDrawOverrideList[projectile.type].DynamicInvoke(projectile, Main.spriteBatch, lightColor);
            }
            return base.PreDraw(projectile, ref lightColor);
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
                        Projectile.NewProjectile(projectile.GetSource_Death(), shootFromVector, offsetAngle.ToRotationVector2() * 5f, ModContent.ProjectileType<HolyFire2>(), projectile.damage, 0f, Main.myPlayer);
                        Projectile.NewProjectile(projectile.GetSource_Death(), shootFromVector, offsetAngle.ToRotationVector2() * -5f, ModContent.ProjectileType<HolyFire2>(), projectile.damage, 0f, Main.myPlayer);
                    }
                }
                SoundEngine.PlaySound(HolyBlast.ImpactSound, projectile.Center);
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