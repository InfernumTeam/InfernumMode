using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using CalamityMod.NPCs.SlimeGod;

namespace InfernumMode
{
    public class InfernalChaliceHoldout : ModProjectile
    {
        public Player Owner => Main.player[Projectile.owner];
        public ref float Time => ref Projectile.ai[0];
        public const int Lifetime = 150;
        public override string Texture => "InfernumMode/Death2";

        public override void SetStaticDefaults() => Main.projFrames[Projectile.type] = 8;

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 96;
            Projectile.aiStyle = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = Lifetime;
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            Time++;
            if (!Owner.channel || Owner.noItems || Owner.CCed)
            {
                CreateMysticDeathDust();
                Projectile.Kill();
                return;
            }

            UpdatePlayerFields();

            if (Projectile.timeLeft == 1)
            {
                Projectile.Kill();
                ToggleModeAndCreateVisuals();
                return;
            }

            CreateIdleFireDust();
            Projectile.frame = (int)Time / 6 % Main.projFrames[Projectile.type];
        }

        public void ToggleModeAndCreateVisuals()
        {
            SoundEngine.PlaySound(SoundID.DD2_EtherianPortalDryadTouch, Main.LocalPlayer.Center);
            SoundEngine.PlaySound(SoundID.DD2_DarkMageHealImpact, Main.LocalPlayer.Center);

            bool infernumWasAlreadyActive = PoDWorld.InfernumMode;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int doom = 0; doom < 200; doom++)
                {
                    if (Main.npc[doom].active && (Main.npc[doom].boss || Main.npc[doom].type == NPCID.EaterofWorldsHead || Main.npc[doom].type == NPCID.EaterofWorldsTail || 
                        Main.npc[doom].type == ModContent.NPCType<SlimeGodRun>() ||
                        Main.npc[doom].type == ModContent.NPCType<SlimeGodRunSplit>() || 
                        Main.npc[doom].type == ModContent.NPCType<SlimeGod>() || 
                        Main.npc[doom].type == ModContent.NPCType<SlimeGodSplit>()))
                    {
                        Owner.KillMe(PlayerDeathReason.ByOther(12), 10000.0, 0, false);

                        Main.npc[doom].active = false;
                        Main.npc[doom].netUpdate = true;
                    }
                }

                Utilities.DisplayText(infernumWasAlreadyActive ? "Very well, then." : "Good luck.", Color.Crimson);
                PoDWorld.InfernumMode = !infernumWasAlreadyActive;
                CalamityNetcode.SyncWorld();
            }

            if (Main.netMode != NetmodeID.Server)
            {
                // Create a lot of fire dust.
                for (int i = 0; i < 80; i++)
                {
                    Dust fire = Dust.NewDustDirect(Projectile.Center, Projectile.width, Projectile.height, 174, 0f, 0f, 200, default, 2.45f);
                    fire.position = Projectile.Center + Vector2.UnitY.RotatedByRandom(MathHelper.Pi) * Main.rand.NextFloat(Projectile.width * 0.5f);
                    fire.noGravity = true;
                    fire.velocity.Y -= 6f;
                    fire.velocity *= 3f;
                    fire.velocity -= Vector2.UnitY.RotatedByRandom(0.45f) * Main.rand.NextFloat(5f);

                    fire = Dust.NewDustDirect(Projectile.Center, Projectile.width, Projectile.height, 174, 0f, 0f, 100, default, 1.4f);
                    fire.position = Projectile.Center + Vector2.UnitY.RotatedByRandom(MathHelper.Pi) * Main.rand.NextFloat(Projectile.width * 0.5f);
                    fire.velocity.Y -= 6f;
                    fire.velocity *= 2f;
                    fire.noGravity = true;
                    fire.fadeIn = 1f;
                    fire.color = Color.Crimson * 0.5f;
                    fire.velocity -= Vector2.UnitY.RotatedByRandom(0.45f) * Main.rand.NextFloat(5f);
                }

                for (int i = 0; i < 40; i++)
                {
                    Dust fire = Dust.NewDustDirect(Projectile.Center, Projectile.width, Projectile.height, 267, 0f, 0f, 0, default, 2.9f);
                    fire.position = Projectile.Center + Vector2.UnitX.RotatedByRandom(MathHelper.Pi) * Projectile.width * Projectile.spriteDirection * 0.5f;
                    fire.color = Color.Lerp(Color.Magenta, Color.Black, 0.6f);
                    fire.noGravity = true;
                    fire.velocity.Y -= 6f;
                    fire.velocity *= 0.5f;
                    fire.velocity += Vector2.UnitY.RotatedByRandom(0.45f) * Main.rand.NextFloat(3f, 7f);
                }
            }
        }

        public void CreateMysticDeathDust()
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 25; i++)
            {
                Dust fire = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(22f, 28f), 267);
                fire.velocity = -Vector2.UnitY * Main.rand.NextFloat(1.8f, 3.2f);
                fire.color = Color.Lerp(Color.Red, Color.DarkMagenta, Main.rand.NextFloat(0.3f, 0.85f));
                fire.scale = Main.rand.NextFloat(1.1f, 1.35f);
                fire.fadeIn = 1.5f;
                fire.noGravity = true;
            }
        }

        public void CreateIdleFireDust()
        {
            if (Main.dedServ)
                return;

            int dustCount = (int)Math.Round(MathHelper.SmoothStep(1f, 5f, Time / Lifetime));
            float outwardness = 145f;
            float dustScale = MathHelper.Lerp(1.35f, 1.625f, Time / Lifetime);
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 spawnPosition = Projectile.Center + Main.rand.NextVector2Unit() * outwardness * Main.rand.NextFloat(0.7f, 1.3f);
                Vector2 dustVelocity = (Projectile.Center - spawnPosition) * 0.095f + Owner.velocity;

                Dust fire = Dust.NewDustPerfect(spawnPosition, 267);
                fire.velocity = dustVelocity;
                fire.scale = dustScale * Main.rand.NextFloat(0.75f, 1.15f);
                fire.color = Color.Lerp(Color.Red, Color.DarkMagenta, MathHelper.Lerp(Main.rand.NextFloat(0.3f, 0.85f), 1f, Time / Lifetime));
                fire.noGravity = true;
            }

            for (int i = 0; i < dustCount / 2; i++)
            {
                Dust fire = Dust.NewDustPerfect(Projectile.Top + Vector2.UnitY * 28f + Main.rand.NextVector2Circular(Projectile.width * 0.5f, 10f), 267);
                fire.velocity = -Vector2.UnitY.RotatedByRandom(0.16f) * Main.rand.NextFloat(2.5f, 3.5f);
                fire.color = Color.Lerp(Color.Red, Color.DarkMagenta, Main.rand.NextFloat());
                fire.scale = dustScale * Main.rand.NextFloat(0.6f, 0.85f);
                fire.fadeIn = 0.37f;
                fire.noGravity = true;
            }
        }

        public void UpdatePlayerFields()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.spriteDirection = Owner.direction;
                Projectile.localAI[0] = 1f;
            }
            Owner.itemRotation = 0f;
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Owner.ChangeDir(Projectile.spriteDirection);

            Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter, true) + new Vector2(Projectile.spriteDirection * 16f, -16f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Utilities.ProjTexture(Projectile.type);
            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Vector2 baseDrawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = frame.Size() * 0.5f;
            Color baseColor = Color.Lerp(Projectile.GetAlpha(lightColor), Color.White, Utils.GetLerpValue(40f, 120f, Time, true));
            SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            if (Time >= 70f)
            {
                float outwardness = MathHelper.SmoothStep(1f, 15f, Utils.GetLerpValue(70f, Lifetime - 40f, Time, true));
                Color afterimageColor = Color.Lerp(baseColor, Color.DarkRed, Utils.GetLerpValue(70f, 95f, Time, true)) * 0.225f;
                afterimageColor.A = 0;

                for (int i = 0; i < 10; i++)
                {
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 10f + Main.GlobalTimeWrappedHourly * 4.4f).ToRotationVector2() * outwardness;
                    Main.spriteBatch.Draw(texture, baseDrawPosition + drawOffset, frame, afterimageColor, 0f, origin, Projectile.scale, direction, 0f);
                }
            }
            Main.spriteBatch.Draw(texture, baseDrawPosition, frame, baseColor, 0f, origin, Projectile.scale, direction, 0f);

            return false;
        }
    }
}
