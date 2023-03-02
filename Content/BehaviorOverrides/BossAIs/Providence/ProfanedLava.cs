using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.Particles;
using CalamityMod.World;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Providence
{
    public class ProfanedLava : ModProjectile
    {
        public PrimitiveTrailCopy LavaDrawer
        {
            get;
            set;
        }

        public SlotId? LavaSound
        {
            get;
            set;
        }

        public Vector2[] SamplePoints
        {
            get
            {
                Vector2[] result = new Vector2[125];
                for (int i = 0; i < result.Length; i++)
                {
                    float horizontalOffset = MathHelper.Lerp(-3900f, 3900f, i / (float)(result.Length - 1f));
                    result[i] = Projectile.Bottom + Vector2.UnitX * horizontalOffset;
                }
                return result;
            }
        }

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public static NPC Providence => Main.npc[CalamityGlobalNPC.holyBoss];

        public ref float Time => ref Projectile.ai[0];

        public ref float LavaHeight => ref Projectile.ai[1];

        public override void SetStaticDefaults() => DisplayName.SetDefault("Searing Lava of Atonement");

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.Opacity = 1f;
            Projectile.timeLeft = int.MaxValue;
            Projectile.hide = true;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        public override void AI()
        {
            // Drain and eventually disappear once Providence is dead.
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.holyBoss))
            {
                if (SoundEngine.TryGetActiveSound(LavaSound.Value, out ActiveSound lavaActiveSound) && lavaActiveSound.IsPlaying)
                    lavaActiveSound.Stop();

                LavaHeight = MathHelper.Clamp(LavaHeight - 16f, 0f, 16000f) * 0.98f;
                if (LavaHeight <= 1f)
                    Projectile.Kill();

                return;
            }

            // Stick at the bottom of the arena.
            LavaHeight = Providence.Infernum().ExtraAI[ProvidenceBehaviorOverride.LavaHeightIndex];
            Projectile.Bottom = WorldSaveSystem.ProvidenceArena.BottomRight() * 16f - Vector2.UnitX * 3000f;

            if (Projectile.height != (int)LavaHeight)
                Projectile.height = (int)LavaHeight;
            Time++;

            // Emit smoke and fire at the very top of the lava.
            Player closestPlayer = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (Main.myPlayer == closestPlayer.whoAmI)
            {
                for (int i = 0; i < (ScreenEffectSystem.AnyBlurOrFlashActive() ? 30 : 8); i++)
                {
                    Vector2 smokeVelocity = -Vector2.UnitY.RotatedByRandom(0.75f) * Main.rand.NextFloat(1f, 4f);

                    // Make the smoke rise if a flash or blur is happening.
                    if (ScreenEffectSystem.AnyBlurOrFlashActive())
                        smokeVelocity *= 3f;

                    Vector2 smokeSpawnPosition = new(closestPlayer.Center.X + Main.rand.NextFloatDirection() * 1200f, Projectile.Top.Y + Main.rand.NextFloatDirection() * 15f + 35f);
                    CloudParticle smoke = new(smokeSpawnPosition, smokeVelocity, ProvidenceBehaviorOverride.IsEnraged ? Color.Cyan : Color.Orange, Color.DarkGray, 26, 0.8f, true);
                    GeneralParticleHandler.SpawnParticle(smoke);
                }
            }

            // Initialize the lava sound.
            if (LavaSound is not null && SoundEngine.TryGetActiveSound(LavaSound.Value, out ActiveSound lavaActiveSound2) && lavaActiveSound2.IsPlaying)
            {
                float verticalDistanceFromLava = MathHelper.Distance(Main.LocalPlayer.Center.Y, Projectile.Top.Y);
                float volume = Utils.GetLerpValue(8000f, 5400f, verticalDistanceFromLava, true) * 0.85f;
                if (!Main.LocalPlayer.Infernum_Biome().ZoneProfaned)
                    volume = 0.0001f;

                lavaActiveSound2.Volume = volume;
            }
            else
                LavaSound = SoundEngine.PlaySound(CalamityMod.NPCs.Providence.Providence.BurnLoopSound with { IsLooped = true }, Main.LocalPlayer.Center);
        }

        public Color ColorFunction(float completionRatio)
        {
            float colorInterpolant = (float)Math.Abs(Math.Sin(completionRatio * MathHelper.Pi * 8f + Main.GlobalTimeWrappedHourly));
            Color c = Color.Lerp(Color.Orange, Color.Red, colorInterpolant * 0.4f);
            if (ProvidenceBehaviorOverride.IsEnraged)
                c = Color.Lerp(c, Color.Blue, 0.95f);
            return c * Projectile.Opacity;
        }

        public float WidthFunction(float completionRatio) => LavaHeight;

        public Vector2 OffsetFunction(float completionRatio)
        {
            float maxOffset = LavaHeight * 0.03f;
            if (maxOffset >= 50f)
                maxOffset = 50f;

            float offsetInterpolant = SulphurousSea.FractalBrownianMotion(completionRatio * 5f, Time * Projectile.localAI[0] / 500f % 50f, Projectile.identity, 4);
            return Vector2.UnitY * offsetInterpolant * maxOffset;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            var samplePoints = SamplePoints;
            for (int i = 0; i < samplePoints.Length; i++)
            {
                float _ = 0f;
                float completionRatio = i / (float)Projectile.oldPos.Length;
                Vector2 top = samplePoints[i] + OffsetFunction(completionRatio);
                Vector2 bottom = samplePoints[i] - Vector2.UnitY * LavaHeight + OffsetFunction(completionRatio);
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), top, bottom, 88f, ref _))
                    return true;
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LavaDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, OffsetFunction, false, InfernumEffectsRegistry.ProfanedLavaVertexShader);

            InfernumEffectsRegistry.ProfanedLavaVertexShader.SetShaderTexture(InfernumTextureRegistry.Smudges);
            InfernumEffectsRegistry.ProfanedLavaVertexShader.Shader.Parameters["lavaHeightInterpolant"].SetValue(LavaHeight / 1400f);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            var samplePoints = SamplePoints;
            for (int i = 0; i < 4; i++)
            {
                Projectile.localAI[0] = (i % 2 == 0f).ToDirectionInt();
                LavaDrawer.Draw(samplePoints, -Main.screenPosition, 35, 0f);
            }
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        // The lava should draw above players, tiles, and NPCs.
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }
    }
}
