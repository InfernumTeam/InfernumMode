using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Projectiles.Enemy;
using CalamityMod.Projectiles.Melee;
using CalamityMod.Projectiles.Typeless;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Providence;
using InfernumMode.Content.Items;
using InfernumMode.Content.Subworlds;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SubworldLibrary;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Core.GlobalInstances
{
    public class GlobalProjectileOverrides : GlobalProjectile
    {
        public bool FrameOneModifiersDone = false;

        public bool FadesAwayWhenManuallyKilled;

        public bool DrawAsShadow;

        public int FadeAwayTimer;

        public const int FadeAwayTime = 30;

        public float[] ExtraAI = new float[100];

        public override bool InstancePerEntity => true;

        public override void SetDefaults(Projectile projectile)
        {
            // Allow Infernum projectiles to draw offscreen by default.
            // TODO -- This is pretty far from ideal. While it may be a bit unpleasant projectiles should really be manually evaluated in terms of whether this is necessary.
            // Applying this effect universally is just asking for edge cases that cause performance issues, such as Providence's old ground/ceiling spears.
            if (projectile.ModProjectile?.Mod.Name == Mod.Name)
                ProjectileID.Sets.DrawScreenCheckFluff[projectile.type] = 20000;

            for (int i = 0; i < ExtraAI.Length; i++)
                ExtraAI[i] = 0f;
            if (InfernumMode.CanUseCustomAIs && projectile.type == ModContent.ProjectileType<HolyAura>())
                projectile.timeLeft = ProvidenceBehaviorOverride.AuraTime;
        }

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            if (FadesAwayWhenManuallyKilled)
                binaryWriter.Write(FadeAwayTimer);
        }

        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
        {
            if (FadesAwayWhenManuallyKilled)
                FadeAwayTimer = binaryReader.ReadInt32();
        }

        public override bool PreAI(Projectile projectile)
        {
            // Make projectiles fade away over time if marked as such.
            if (FadesAwayWhenManuallyKilled)
            {
                // Prevent natural death when fading away, since that could result in extra projectiles that we don't want appearing.
                if (FadeAwayTimer >= 1)
                    projectile.timeLeft = FadeAwayTimer;

                if (FadeAwayTimer >= 1 && projectile.FinalExtraUpdate())
                {
                    projectile.Opacity = MathHelper.Min(projectile.Opacity, FadeAwayTimer / (float)FadeAwayTime);

                    FadeAwayTimer--;
                    if (FadeAwayTimer <= 0)
                        projectile.active = false;
                }
            }

            if (InfernumMode.CanUseCustomAIs)
            {
                if (projectile.type == ModContent.ProjectileType<TrilobiteSpike>())
                    projectile.ModProjectile.CooldownSlot = 1;

                if (OverridingListManager.InfernumProjectilePreAIOverrideList.TryGetValue(projectile.type, out Delegate value))
                    return (bool)value.DynamicInvoke(projectile);
            }

            // No tombs.
            // h.
            bool isTomb = projectile.type is ProjectileID.Tombstone or ProjectileID.Gravestone or ProjectileID.RichGravestone1 or ProjectileID.RichGravestone2 or
                ProjectileID.RichGravestone3 or ProjectileID.RichGravestone4 or ProjectileID.RichGravestone4 or ProjectileID.Headstone or ProjectileID.Obelisk;

            bool illegalRocket = projectile.type is ProjectileID.DryRocket or ProjectileID.DryGrenade or ProjectileID.DryMine or ProjectileID.DrySnowmanRocket;
            illegalRocket |= projectile.type is ProjectileID.WetRocket or ProjectileID.WetGrenade or ProjectileID.WetMine or ProjectileID.WetBomb or ProjectileID.WetSnowmanRocket;
            illegalRocket |= projectile.type is ProjectileID.HoneyRocket or ProjectileID.HoneyGrenade or ProjectileID.HoneyMine or ProjectileID.HoneyBomb or ProjectileID.HoneySnowmanRocket;
            illegalRocket |= projectile.type is ProjectileID.LavaRocket or ProjectileID.LavaGrenade or ProjectileID.LavaMine or ProjectileID.LavaBomb or ProjectileID.LavaSnowmanRocket;
            illegalRocket |= projectile.type is ProjectileID.DirtBomb or ProjectileID.DirtStickyBomb;

            bool projectileInProvArena = new Rectangle((int)projectile.Center.X / 16, (int)projectile.Center.Y / 16, 4, 4).Intersects(WorldSaveSystem.ProvidenceArena);
            bool mouseInProvArena = new Rectangle((int)Main.MouseWorld.X / 16, (int)Main.MouseWorld.Y / 16, 4, 4).Intersects(WorldSaveSystem.ProvidenceArena);
            bool inColosseum = SubworldSystem.IsActive<LostColosseum>();
            if (illegalRocket && (projectileInProvArena || inColosseum))
                projectile.active = false;
            if (projectile.type == ModContent.ProjectileType<CrystylCrusherRay>() && (mouseInProvArena || inColosseum))
                projectile.active = false;

            if (isTomb)
                projectile.active = false;

            return base.PreAI(projectile);
        }

        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            if (InfernumMode.CanUseCustomAIs && projectile.type == ModContent.ProjectileType<HolyAura>() && ProvidenceBehaviorOverride.IsEnraged)
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
                    float oscillatingTime = MathF.Cos(clampedTime * MathHelper.TwoPi + i / 2f);

                    posX[i] = oscillatingTime * (xPosOffset - i * 3f);

                    posY[i] = MathF.Sin(clampedTime * MathHelper.TwoPi + MathHelper.Pi / 3f + i) * yPosOffset;
                    posY[i] -= i * 3f;

                    hue[i] = i / (float)totalAurasToDraw * 2f % 1f;

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
                if (OverridingListManager.InfernumProjectilePreDrawOverrideList.TryGetValue(projectile.type, out Delegate value))
                    return (bool)value.DynamicInvoke(projectile, Main.spriteBatch, lightColor);
            }
            return true;
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

            // Make the boss rush end thing create an infernal chalice as well as the rock.
            if (projectile.type == ModContent.ProjectileType<BossRushEndEffectThing>())
            {
                for (int i = Main.maxPlayers - 1; i >= 0; i--)
                {
                    Player p = Main.player[i];
                    if (p is null || !p.active)
                        continue;

                    int notRock = Item.NewItem(p.GetSource_Misc("CalamityMod_BossRushRock"), (int)p.position.X, (int)p.position.Y, p.width, p.height, ModContent.ItemType<DemonicChaliceOfInfernum>());
                    if (Main.netMode == NetmodeID.Server)
                    {
                        Main.timeItemSlotCannotBeReusedFor[notRock] = 54000;
                        NetMessage.SendData(MessageID.InstancedItem, i, -1, null, notRock);
                    }
                }
            }

            // Prevent projectiles that are fading away from dying via natural means and potentially spawning more projectiles.
            if (FadesAwayWhenManuallyKilled && FadeAwayTimer >= 1)
                return false;

            return base.PreKill(projectile, timeLeft);
        }

        public override Color? GetAlpha(Projectile projectile, Color lightColor)
        {
            if (projectile.type == ProjectileID.PhantasmalEye)
                return Color.White * projectile.Opacity;

            return base.GetAlpha(projectile, lightColor);
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

        public override bool CanHitPlayer(Projectile projectile, Player target)
        {
            if (InfernumMode.CanUseCustomAIs)
            {
                if (projectile.type == ProjectileID.PhantasmalSphere)
                    return projectile.Infernum().ExtraAI[0] >= 25f;
                if (projectile.type == ProjectileID.PhantasmalBolt)
                    return projectile.Infernum().ExtraAI[0] >= 40f;
            }

            // Prevent projectiles that are fading away from doing damage.
            if (FadesAwayWhenManuallyKilled && FadeAwayTimer >= 1)
                return false;

            return base.CanHitPlayer(projectile, target);
        }
    }
}