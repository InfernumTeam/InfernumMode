﻿using CalamityMod.DataStructures;
using CalamityMod.Particles;
using InfernumMode.Sounds;
using InfernumMode.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Projectiles.Wayfinder
{
    public class WayfinderGate : ModProjectile
    {
        public ref float Timer => ref Projectile.ai[0];
        public SlotId LoopSlot;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wayfinder Gate");
        }
        public override void SetDefaults()
        {
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.width = Projectile.height = 40;
            Projectile.timeLeft = 2;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.Opacity = 0;
            Projectile.hide = true;
        }

        public override void AI()
        {
            // Only exist if the position is set.
            if(WorldSaveSystem.WayfinderGateLocation == Vector2.Zero)
                Projectile.Kill();
            Projectile.active = true;
            // Fade in.
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.015f, 0f, 1f);
            if (Timer >= (1 / 0.015f))
                Projectile.Opacity = 1;
            // Ensure the position remains accurate.
            Projectile.Center = WorldSaveSystem.WayfinderGateLocation;

            // Never run out of time.
            Projectile.timeLeft = 2;

            // Handle the loop sound.
            if(Timer % 115 is 0)
                LoopSlot = SoundEngine.PlaySound(InfernumSoundRegistry.WayfinderGateLoop with { Volume = 0.2f }, Projectile.Center);

            if (SoundEngine.TryGetActiveSound(LoopSlot, out var sound))
            {
                if (sound.Position != Projectile.Center)
                    sound.Position = Projectile.Center;
            }

            // Periodically emit particles if any player is nearby.
            bool nearbyPlayer = false;
            for(int i = 0; i < Main.player.Length; i++)
            {
                Player player = Main.player[i];
                if (player.active && player.Center.Distance(Projectile.Center) < 1500)
                {
                    nearbyPlayer = true;
                    break;
                }
            }
            if(nearbyPlayer)
            {
                if (Main.rand.NextBool(8))
                {
                    Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(40, 10) + new Vector2(-7, -11);
                    Dust fire = Dust.NewDustDirect(position, 16, 16, Main.rand.NextBool() ? 267 : 6, 0f, 0f, 254, Color.White, 1.4f);
                    fire.velocity = -Vector2.UnitY.RotatedByRandom(0.5f) * Main.rand.Next(3,6);
                    fire.color = Color.Lerp(Color.Yellow, Color.Red, Main.rand.NextFloat(0.7f));
                    fire.noGravity = true;
                }
                if (Main.rand.NextBool(20))
                {
                    Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(40, 10) + new Vector2(-7, -11);
                    Vector2 velocity = -Vector2.UnitY.RotatedByRandom(0.5f) * 5;
                    Particle particle = new CritSpark(position, velocity, Main.rand.NextBool() ? Color.Orange : Color.Gold, Color.LightGoldenrodYellow, Main.rand.NextFloat(0.35f, 0.6f), 60, 0.2f);
                    GeneralParticleHandler.SpawnParticle(particle);
                }
                if(Main.rand.NextBool(80))
                {
                    Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(30, 10) + new Vector2(22, 20);
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(null), position, Vector2.Zero, ModContent.ProjectileType<WayfinderSymbol>(), 0, 0, Main.myPlayer);
                }
                // Spawn a symbol every 90 frames, due to the low chance of spawning often leading to empty patches of spawns.
                if (Timer % 90 == 0)
                {
                    Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(30, 10) + new Vector2(22, 20);
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(null), position, Vector2.Zero, ModContent.ProjectileType<WayfinderSymbol>(), 0, 0, Main.myPlayer);
                }
            }
            Timer++;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }

        public override void Kill(int timeLeft)
        {
            if (SoundEngine.TryGetActiveSound(LoopSlot, out var sound))
            {
                sound.Stop();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D outerTexture = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/WayfinderGateOuter").Value;
            Texture2D innerTexture = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/WayfinderGateInner").Value;

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Color outerColor = CalamityMod.CalamityUtils.ColorSwap(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], 10f);
            Color innerColor = Color.Lerp(Color.White, outerColor, 0.5f) * 0.6f;

            float rotOuter = Main.GlobalTimeWrappedHourly * 0.1f;
            float rotInner = Main.GlobalTimeWrappedHourly * 0.133f;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin((SpriteSortMode)1, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);

            int initialGateCreationTime = 120;
            if(Timer > initialGateCreationTime)
                Main.spriteBatch.Draw(bloomTexture, drawPos, null, outerColor * Projectile.Opacity * 0.55f, 0f, bloomTexture.Size() * 0.5f, 1f, 0, 0);
            else
            {
                float interpolant = Timer / initialGateCreationTime;
                float opacity = MathHelper.Lerp(1, 0.55f, interpolant);
                float scale2 = MathHelper.Lerp(2, 1, interpolant);

                Main.spriteBatch.Draw(bloomTexture, drawPos, null, outerColor * opacity, 0, bloomTexture.Size() * 0.5f, scale2, SpriteEffects.None, 0f);
            }
            Main.spriteBatch.Draw(outerTexture, drawPos, null, outerColor * Projectile.Opacity, rotOuter, outerTexture.Size() * 0.5f, 1, 0, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin((SpriteSortMode)0, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);

            Main.spriteBatch.Draw(innerTexture, drawPos, null, innerColor * Projectile.Opacity, rotInner, innerTexture.Size() * 0.5f, 1, 0, 0);

            float scale = (float)Math.Sin(Main.GlobalTimeWrappedHourly * MathHelper.TwoPi / 2f) * 0.3f + 0.7f;
            innerColor.A = 0;
            innerColor = innerColor * 0.1f * scale;
            for (float num5 = 0f; num5 < 1f; num5 += 1f / 16f)
                Main.spriteBatch.Draw(innerTexture, drawPos + (MathHelper.TwoPi * num5).ToRotationVector2() * (6f +2f), null, innerColor * Projectile.Opacity, rotInner, innerTexture.Size() * 0.5f, 1f, 0, 0f);
            return false;
        }


    }
}