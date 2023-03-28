using CalamityMod;
using CalamityMod.NPCs.CalClone;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Boss;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasClone
{
    public class SoulSeekerResurrectionBeam : ModProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy BeamDrawer
        {
            get;
            set;
        }

        public bool HasSummonedSeeker
        {
            get;
            set;
        }

        public ref float Time => ref Projectile.ai[0];

        public ref float LaserLength => ref Projectile.ai[1];

        public const int Lifetime = 30;

        public const float MaxLaserLength = 5500f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Necromantic Beam");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.alpha = 255;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Fade in.
            Projectile.alpha = Utils.Clamp(Projectile.alpha - 25, 0, 255);

            Projectile.scale = CalamityUtils.Convert01To010(Projectile.timeLeft / (float)Lifetime) * 3f;
            if (Projectile.scale > 1f)
                Projectile.scale = 1f;

            // Calculate the laser length.
            float maxCheckDistance = Utils.GetLerpValue(-1f, 10f, Time, true) * MaxLaserLength;
            float[] distanceSamples = new float[12];
            Collision.LaserScan(Projectile.Center, Projectile.velocity, Projectile.width, maxCheckDistance, distanceSamples);
            LaserLength = distanceSamples.Average();

            // Summon a seeker if necessary.
            if (Time >= 10f && !HasSummonedSeeker && (LaserLength < MaxLaserLength - 200f || Time >= Lifetime - 1f))
            {
                Vector2 seekerSpawnPosition = Projectile.Center + Projectile.velocity * LaserLength;
                SoundEngine.PlaySound(BrimstoneMonster.SpawnSound, seekerSpawnPosition);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int seekerID = ModContent.NPCType<SoulSeeker>();
                    int totalSeekers = NPC.CountNPCS(seekerID) + 1;

                    if (totalSeekers >= 7)
                    {
                        HasSummonedSeeker = true;
                        return;
                    }

                    int seeker = NPC.NewNPC(Projectile.GetSource_FromThis(), (int)seekerSpawnPosition.X, (int)seekerSpawnPosition.Y, seekerID);
                    if (Main.npc.IndexInRange(seeker))
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, seeker);

                    int seekerIndex = 0;
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC n = Main.npc[i];

                        if (n.active && n.type == seekerID)
                        {
                            n.ai[0] = MathHelper.TwoPi * seekerIndex / totalSeekers;
                            n.ai[1] = 0f;
                            n.ai[3] = 0f;
                            n.netUpdate = true;
                            seekerIndex++;
                        }
                    }
                }

                for (int i = 0; i < 10; i++)
                {
                    Color fireColor = Main.rand.NextBool() ? Color.Yellow : Color.Red;
                    CloudParticle fireCloud = new(seekerSpawnPosition, (MathHelper.TwoPi * i / 10f).ToRotationVector2() * 6f, fireColor, Color.DarkGray, 36, Main.rand.NextFloat(1.9f, 2.3f));
                    GeneralParticleHandler.SpawnParticle(fireCloud);
                }
                HasSummonedSeeker = true;
            }

            Time++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            float width = Projectile.width * 0.8f;
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity * (LaserLength - 80f) * 0.65f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, width, ref _);
        }

        public float WidthFunction(float completionRatio)
        {
            float squeezeInterpolant = Utils.GetLerpValue(0f, 0.16f, completionRatio, true) * Utils.GetLerpValue(1f, 0.95f, completionRatio, true);
            float baseWidth = MathHelper.SmoothStep(2f, Projectile.width, squeezeInterpolant) * MathHelper.Clamp(Projectile.scale, 0.01f, 1f);
            return baseWidth * MathHelper.Lerp(1f, 2.3f, Projectile.localAI[0]);
        }

        public override bool ShouldUpdatePosition() => false;

        public Color ColorFunction(float completionRatio)
        {
            float opacity = Utils.GetLerpValue(0.92f, 0.6f, completionRatio, true) * MathHelper.Lerp(1f, 0.45f, Projectile.localAI[0]) * Projectile.Opacity * 0.4f;
            Color color = Color.Lerp(Color.Red, Color.Yellow, (float)Math.Abs(Math.Sin(completionRatio * MathHelper.Pi + Main.GlobalTimeWrappedHourly)) * 0.5f);
            return color * opacity;
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            BeamDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.ArtemisLaserVertexShader);

            // Select textures to pass to the shader, along with the electricity color.
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseColor(Color.Red);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakMagma);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseImage2("Images/Misc/Perlin");
            InfernumEffectsRegistry.ArtemisLaserVertexShader.Shader.Parameters["uStretchReverseFactor"].SetValue((LaserLength + 1f) / MaxLaserLength * 8f);

            List<Vector2> points = new();
            for (int i = 0; i <= 8; i++)
                points.Add(Vector2.Lerp(Projectile.Center - Projectile.velocity * 18f, Projectile.Center + Projectile.velocity * LaserLength, i / 8f));

            BeamDrawer.DrawPixelated(points, Projectile.Size * 0.5f - Main.screenPosition, 60);
            Main.spriteBatch.ExitShaderRegion();
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Time >= 8f;
    }
}
