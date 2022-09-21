using CalamityMod;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Boss;
using InfernumMode.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
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

        public override void SetStaticDefaults() => DisplayName.SetDefault("Wrathful Spirits");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 8;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.scale = 1f;
            Projectile.hide = true;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(ExplodeCountdown);

        public override void ReceiveExtraAI(BinaryReader reader) => ExplodeCountdown = reader.ReadInt32();

        public override void AI()
        {
            Radius = MathHelper.Lerp(Radius, MaxRadius, 0.02f);
            Projectile.Opacity = Utils.GetLerpValue(8f, 42f, Projectile.timeLeft, true) * 0.55f;

            Time++;

            // Decrement the explosion countdown if appliable.
            if (ExplodeCountdown > 0)
            {
                ExplodeCountdown--;
                if (ExplodeCountdown < 18)
                    Radius = MathHelper.Lerp(Radius, 25f, 0.15f);

                if (ExplodeCountdown <= 0)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.WyrmChargeSound, Projectile.Center);

                    int dartRingCount = 3;
                    int dartsPerRing = 15;

                    // Explode into a spread of darts, fire bursts, and souls.
                    for (int i = 0; i < 75; i++)
                    {
                        SquishyLightParticle fire = new(Projectile.Center, Main.rand.NextVector2Unit() * Main.rand.NextFloat(6f, 20f), 1f, Color.Orange, 64, 1.4f, 2.7f);
                        GeneralParticleHandler.SpawnParticle(fire);
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        // Create souls.
                        for (int i = 0; i < 25; i++)
                        {
                            Vector2 soulVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(12f, 28f);
                            Utilities.NewProjectileBetter(Projectile.Center, soulVelocity, ModContent.ProjectileType<LostSoulProjFriendly>(), 0, 0f);
                        }

                        // Create darts.
                        for (int i = 0; i < dartRingCount; i++)
                        {
                            float dartSpeed = MathHelper.Lerp(8f, 3f, i / (float)(dartRingCount - 1f));
                            for (int j = 0; j < dartsPerRing; j++)
                            {
                                Vector2 dartVelocity = (MathHelper.TwoPi * j / dartsPerRing).ToRotationVector2() * dartSpeed;
                                if (i % 2 == 0)
                                    dartVelocity = dartVelocity.RotatedBy(MathHelper.Pi / dartsPerRing);
                                Utilities.NewProjectileBetter(Projectile.Center, dartVelocity, ModContent.ProjectileType<BrimstoneBarrage>(), 500, 0f);
                            }
                            dartsPerRing += 4;
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
            NPC sepulcher = Main.npc[sepulcherIndex];
            Projectile.Center = sepulcher.Center + sepulcher.velocity.SafeNormalize((sepulcher.rotation - MathHelper.PiOver2).ToRotationVector2()) * (Radius * 0.92f + 45f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Utilities.CircularCollision(targetHitbox.Center.ToVector2(), projHitbox, Radius * 0.8f);

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int sideCount = 512;
            Utilities.GetCircleVertices(sideCount, Radius, Projectile.Center, out var triangleIndices, out var vertices);

            CalamityUtils.CalculatePerspectiveMatricies(out Matrix view, out Matrix projection);
            GameShaders.Misc["Infernum:RealityTear"].SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/BrimstoneSoulLayer"));
            GameShaders.Misc["Infernum:RealityTear"].Shader.Parameters["uWorldViewProjection"].SetValue(view * projection);
            GameShaders.Misc["Infernum:RealityTear"].Shader.Parameters["useOutline"].SetValue(false);
            GameShaders.Misc["Infernum:RealityTear"].Shader.Parameters["uCoordinateZoom"].SetValue(3.2f);
            GameShaders.Misc["Infernum:RealityTear"].Shader.Parameters["uTimeFactor"].SetValue(3.2f);
            GameShaders.Misc["Infernum:RealityTear"].UseSaturation(10f);
            GameShaders.Misc["Infernum:RealityTear"].Apply();

            Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices.ToArray(), 0, vertices.Count, triangleIndices.ToArray(), 0, sideCount * 2);
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            return false;
        }
    }
}
