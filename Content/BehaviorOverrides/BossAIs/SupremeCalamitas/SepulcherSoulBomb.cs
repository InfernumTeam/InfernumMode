using CalamityMod;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Boss;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SepulcherSoulBomb : ModProjectile
    {
        public int ExplodeCountdown;

        public PrimitiveTrailCopy FireDrawer;

        public ref float Time => ref Projectile.ai[0];

        public ref float Radius => ref Projectile.ai[1];

        public const int Lifetime = 360;

        public const float MaxRadius = 500f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 8;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.scale = 1f;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(ExplodeCountdown);

        public override void ReceiveExtraAI(BinaryReader reader) => ExplodeCountdown = reader.ReadInt32();

        public override void AI()
        {
            Radius = Lerp(Radius, MaxRadius, 0.02f);
            Projectile.Opacity = Utils.GetLerpValue(8f, 42f, Projectile.timeLeft, true) * 0.55f;

            Time++;

            // Decrement the explosion countdown if appliable.
            if (ExplodeCountdown > 0)
            {
                ExplodeCountdown--;
                if (ExplodeCountdown < 18)
                    Radius = Lerp(Radius, 25f, 0.15f);

                if (ExplodeCountdown <= 0)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.WyrmChargeSound, Projectile.Center);

                    int dartRingCount = 3;
                    int dartsPerRing = 15;

                    // Create a particle effect explosion.
                    for (int i = 0; i < 75; i++)
                    {
                        SquishyLightParticle fire = new(Projectile.Center, Main.rand.NextVector2Unit() * Main.rand.NextFloat(6f, 20f), 1f, Color.Orange, 64, 1.4f, 2.7f);
                        GeneralParticleHandler.SpawnParticle(fire);
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        // Create darts if Sepulcher exists.
                        if (NPC.AnyNPCs(ModContent.NPCType<SepulcherHead>()))
                        {
                            for (int i = 0; i < dartRingCount; i++)
                            {
                                float dartSpeed = Lerp(8f, 3f, i / (float)(dartRingCount - 1f));
                                for (int j = 0; j < dartsPerRing; j++)
                                {
                                    Vector2 dartVelocity = (TwoPi * j / dartsPerRing).ToRotationVector2() * dartSpeed;
                                    if (i % 2 == 0)
                                        dartVelocity = dartVelocity.RotatedBy(Pi / dartsPerRing);
                                    Utilities.NewProjectileBetter(Projectile.Center, dartVelocity, ModContent.ProjectileType<BrimstoneBarrage>(), SupremeCalamitasBehaviorOverride.BrimstoneDartDamage, 0f);
                                }
                                dartsPerRing += 4;
                            }
                        }
                        Projectile.Kill();
                    }
                }
                return;
            }

            // If not exploding, hover in front of the Sepulcher's head.
            int sepulcherIndex = NPC.FindFirstNPC(ModContent.NPCType<SepulcherHead>());

            // Die if Sepulcher is not present.
            if (sepulcherIndex == -1)
            {
                Projectile.active = false;
                return;
            }

            float mouthOffset = Pow(Radius / MaxRadius, 2.3f) * MaxRadius * 0.92f + 45f;
            NPC sepulcher = Main.npc[sepulcherIndex];
            Projectile.Center = sepulcher.Center + sepulcher.velocity.SafeNormalize((sepulcher.rotation - PiOver2).ToRotationVector2()) * mouthOffset;

            // Create charge-up particles.
            if (Radius < MaxRadius * 0.98f)
            {
                Vector2 magicVelocity = sepulcher.SafeDirectionTo(Projectile.Center).RotatedByRandom(0.7f) * Main.rand.NextFloat(3f, 14f);
                var brimstoneMagic = new SquishyLightParticle(sepulcher.Center, magicVelocity, 1.6f, Color.Red, 70, 1f, 1.65f);
                GeneralParticleHandler.SpawnParticle(brimstoneMagic);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Utilities.CircularCollision(targetHitbox.Center.ToVector2(), projHitbox, Radius * 0.72f);

        public override bool? CanDamage() => ExplodeCountdown > 0 ? null : false;

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int sideCount = 512;
            Utilities.GetCircleVertices(sideCount, Radius, Projectile.Center, out var triangleIndices, out var vertices);

            LumUtils.CalculatePrimitiveMatrices(Main.screenWidth, Main.screenHeight, out Matrix view, out Matrix projection);
            Main.instance.GraphicsDevice.Textures[1] = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/ScrollingLayers/BrimstoneSoulLayer").Value;
            var tear = InfernumEffectsRegistry.RealityTearVertexShader;
            tear.TrySetParameter("uWorldViewProjection", view * projection);
            tear.TrySetParameter("useOutline", false);
            tear.TrySetParameter("uCoordinateZoom", 3.2f);
            tear.TrySetParameter("uTimeFactor", 3.2f);
            tear.TrySetParameter("uSaturation", 10f);
            tear.Apply();

            Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices.ToArray(), 0, vertices.Count, triangleIndices.ToArray(), 0, sideCount * 2);
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            return false;
        }
    }
}
