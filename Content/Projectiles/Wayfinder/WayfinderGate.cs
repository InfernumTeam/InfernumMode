using CalamityMod.Particles;
using InfernumMode.Assets.Sounds;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Wayfinder
{
    public class WayfinderGate : ModProjectile
    {
        public SlotId LoopSlot;

        public ref float Timer => ref Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.width = Projectile.height = 40;
            Projectile.timeLeft = 2;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.Opacity = 0f;
            Projectile.hide = true;
        }

        public override void AI()
        {
            // Only exist if the position is set.
            if (WorldSaveSystem.WayfinderGateLocation == Vector2.Zero)
                Projectile.Kill();

            // Fade in.
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.015f, 0f, 1f);

            // Ensure the position remains accurate.
            Projectile.Center = WorldSaveSystem.WayfinderGateLocation;

            // Never die naturally.
            Projectile.timeLeft = 2;

            // Handle the loop sound.
            if (Timer % 115f is 0f)
                LoopSlot = SoundEngine.PlaySound(InfernumSoundRegistry.WayfinderGateLoop with { Volume = 0.2f }, Projectile.Center);

            if (SoundEngine.TryGetActiveSound(LoopSlot, out var sound))
            {
                if (sound.Position != Projectile.Center)
                    sound.Position = Projectile.Center;
            }

            // Periodically emit particles if any player is nearby.
            bool nearbyPlayer = false;
            for (int i = 0; i < Main.player.Length; i++)
            {
                Player player = Main.player[i];
                if (player.active && player.WithinRange(Projectile.Center, 1500f))
                {
                    nearbyPlayer = true;
                    break;
                }
            }
            if (nearbyPlayer)
            {
                if (Main.rand.NextBool(8))
                {
                    Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(40f, 10f) + new Vector2(-7f, -11f);
                    Dust fire = Dust.NewDustDirect(position, 16, 16, Main.rand.NextBool() ? 267 : 6, 0f, 0f, 254, Color.White, 1.4f);
                    fire.velocity = -Vector2.UnitY.RotatedByRandom(0.5f) * Main.rand.Next(3, 6);
                    fire.color = Color.Lerp(Color.Yellow, Color.Red, Main.rand.NextFloat(0.7f));
                    fire.noGravity = true;
                }
                if (Main.rand.NextBool(20))
                {
                    Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(40f, 10f) + new Vector2(-7f, -11f);
                    Vector2 velocity = -Vector2.UnitY.RotatedByRandom(0.5f) * 5;
                    Particle particle = new CritSpark(position, velocity, Main.rand.NextBool() ? Color.Orange : Color.Gold, Color.LightGoldenrodYellow, Main.rand.NextFloat(0.35f, 0.6f), 60, 0.2f);
                    GeneralParticleHandler.SpawnParticle(particle);
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (Main.rand.NextBool(80))
                    {
                        Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(30f, 10f) + new Vector2(22f, 20f);
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(null), position, Vector2.Zero, ModContent.ProjectileType<WayfinderSymbol>(), 0, 0, Main.myPlayer);
                    }

                    // Spawn a symbol every 90 frames, due to the low chance of spawning often leading to empty patches of spawns.
                    if (Timer % 90f == 0f)
                    {
                        Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(30f, 10f) + new Vector2(22f, 20f);
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(null), position, Vector2.Zero, ModContent.ProjectileType<WayfinderSymbol>(), 0, 0, Main.myPlayer);
                    }
                }
            }
            Timer++;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }

        public override void OnKill(int timeLeft)
        {
            if (SoundEngine.TryGetActiveSound(LoopSlot, out var sound))
                sound.Stop();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D outerTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Projectiles/Wayfinder/WayfinderGateOuter").Value;
            Texture2D innerTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Projectiles/Wayfinder/WayfinderGateInner").Value;

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Color outerColor = CalamityMod.CalamityUtils.ColorSwap(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], 10f);
            Color innerColor = Color.Lerp(Color.White, outerColor, 0.5f) * 0.6f;

            float rotOuter = Main.GlobalTimeWrappedHourly * 0.1f;
            float rotInner = Main.GlobalTimeWrappedHourly * 0.133f;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            int initialGateCreationTime = 120;
            if (Timer > initialGateCreationTime)
                Main.spriteBatch.Draw(bloomTexture, drawPos, null, outerColor * Projectile.Opacity * 0.55f, 0f, bloomTexture.Size() * 0.5f, 1f, 0, 0);
            else
            {
                float interpolant = Timer / initialGateCreationTime;
                float opacity = Lerp(1, 0.55f, interpolant);
                float scale2 = Lerp(2, 1, interpolant);

                Main.spriteBatch.Draw(bloomTexture, drawPos, null, outerColor * opacity, 0, bloomTexture.Size() * 0.5f, scale2, SpriteEffects.None, 0f);
            }
            Main.spriteBatch.Draw(outerTexture, drawPos, null, outerColor * Projectile.Opacity, rotOuter, outerTexture.Size() * 0.5f, 1, 0, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Main.spriteBatch.Draw(innerTexture, drawPos, null, innerColor * Projectile.Opacity, rotInner, innerTexture.Size() * 0.5f, 1, 0, 0);

            float scale = Sin(Main.GlobalTimeWrappedHourly * TwoPi / 2f) * 0.3f + 0.7f;
            innerColor.A = 0;
            innerColor = innerColor * 0.1f * scale;
            for (float i = 0f; i < 1f; i += 1f / 16f)
                Main.spriteBatch.Draw(innerTexture, drawPos + (TwoPi * i).ToRotationVector2() * (6f + 2f), null, innerColor * Projectile.Opacity, rotInner, innerTexture.Size() * 0.5f, 1f, 0, 0f);
            return false;
        }


    }
}
