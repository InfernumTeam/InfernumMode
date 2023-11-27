using CalamityMod;
using CalamityMod.Particles;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Metaballs;
using InfernumMode.Common.Graphics.Primitives;
using InfernumMode.Content.Projectiles.Wayfinder;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Providence
{
    public class AcceleratingMagicProfanedRock : ModProjectile, ISpecializedDrawRegion
    {
        public int CurrentVarient
        {
            get;
            set;
        } = 1;

        public int MagicGlowTimer
        {
            get;
            set;
        } = 30;

        public PrimitiveTrailCopy AfterimageTrail
        {
            get;
            set;
        }

        public ref float Timer => ref Projectile.ai[0];

        public override string Texture => "CalamityMod/Projectiles/Typeless/ArtifactOfResilienceShard" + CurrentVarient;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Profaned Rock");
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
        }

        public override void SetDefaults()
        {
            // The size gets changed later, but is this be default.
            Projectile.width = 42;
            Projectile.height = 36;

            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.Opacity = 1f;
            Projectile.timeLeft = 240;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(CurrentVarient);
            writer.Write(MagicGlowTimer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            CurrentVarient = reader.ReadInt32();
            MagicGlowTimer = reader.ReadInt32();
        }

        public override void AI()
        {
            // Initialize the rock variant.
            if (Projectile.localAI[0] == 0)
            {
                Projectile.localAI[0] = 1f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    CurrentVarient = Main.rand.Next(1, 7);
                    switch (CurrentVarient)
                    {
                        case 2:
                            Projectile.width = 30;
                            Projectile.height = 38;
                            break;
                        case 3:
                            Projectile.width = 34;
                            Projectile.height = 38;
                            break;
                        case 4:
                            Projectile.width = 36;
                            Projectile.height = 46;
                            break;
                        case 5:
                            Projectile.width = 28;
                            Projectile.height = 36;
                            break;
                        case 6:
                            Projectile.width = 22;
                            Projectile.height = 20;
                            break;
                    }
                    Projectile.netUpdate = true;
                }
            }

            // Accelerate.
            if (Projectile.velocity.Length() < 32f)
                Projectile.velocity *= 1.037f;

            // Create rock particles.
            Particle rockParticle = new SandyDustParticle(Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 3f, Projectile.height / 3f), Vector2.Zero, Color.SandyBrown, Main.rand.NextFloat(0.45f, 0.75f), 30);
            GeneralParticleHandler.SpawnParticle(rockParticle);

            // Emit lava particles.
            if (Main.rand.NextBool() && Main.netMode != NetmodeID.Server)
            {
                Vector2 lavaSpawnPosition = Projectile.Center + Projectile.velocity * 0.5f;
                ModContent.GetInstance<ProfanedLavaMetaball>().SpawnParticles(ModContent.Request<Texture2D>(Texture).Value.CreateMetaballsFromTexture(lavaSpawnPosition, 0f, Projectile.scale * 0.8f, 12f, 190));
            }

            // Spin.
            Projectile.rotation -= 0.1f;
            Timer++;
        }

        public float PrimitiveWidthFunction(float _) => Projectile.scale * 30f;

        public Color PrimitiveColorFunction(float _) => Color.HotPink * Projectile.Opacity * 1.3f;

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawAfterimageTrail()
        {
            // Initialize the trail.
            var trailShader = GameShaders.Misc["CalamityMod:HeavenlyGaleTrail"];
            AfterimageTrail ??= new(PrimitiveWidthFunction, PrimitiveColorFunction, null, true, trailShader);

            float localIdentityOffset = Projectile.identity * 0.1372f;
            Color mainColor = CalamityUtils.MulticolorLerp((Main.GlobalTimeWrappedHourly * 2f + localIdentityOffset) % 1f, Color.Yellow, Color.Pink, Color.HotPink, Color.Goldenrod, Color.Orange);
            Color secondaryColor = CalamityUtils.MulticolorLerp((Main.GlobalTimeWrappedHourly * 2f + localIdentityOffset + 0.2f) % 1f, Color.Yellow, Color.Pink, Color.HotPink, Color.Goldenrod, Color.Orange);

            mainColor = Color.Lerp(Color.White, mainColor, 0.85f);
            secondaryColor = Color.Lerp(Color.White, secondaryColor, 0.85f);

            Vector2 trailOffset = Projectile.Size * 0.5f - Main.screenPosition;
            trailShader.SetShaderTexture(InfernumTextureRegistry.FireNoise);
            trailShader.UseImage2("Images/Extra_189");
            trailShader.UseColor(mainColor);
            trailShader.UseSecondaryColor(secondaryColor);
            AfterimageTrail.Draw(Projectile.oldPos, trailOffset, 5);
        }

        public void SpecialDraw(SpriteBatch spriteBatch)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;

            Color backglowColor = Color.Lerp(WayfinderSymbol.Colors[0], Color.Pink, 0.5f);
            backglowColor.A = 0;

            // Draw the afterimage trail first.
            DrawAfterimageTrail();

            // Draw the bloom line telegraph.
            if (Timer <= MagicGlowTimer)
            {
                float opacity = CalamityUtils.Convert01To010(Timer / MagicGlowTimer);
                BloomLineDrawInfo lineInfo = new(rotation: -Projectile.velocity.ToRotation(),
                    width: 0.003f + Pow(opacity, 5f) * (Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.001f + 0.001f),
                    bloom: Lerp(0.06f, 0.16f, opacity),
                    scale: Vector2.One * 1950f,
                    main: Color.Pink,
                    darker: Color.Orange,
                    opacity: opacity,
                    bloomOpacity: 0.4f,
                    lightStrength: 5f);

                Utilities.DrawBloomLineTelegraph(drawPosition, lineInfo, false);
            }

            float backglowCount = 12;
            for (int i = 0; i < backglowCount; i++)
            {
                Vector2 backglowOffset = (TwoPi * i / backglowCount).ToRotationVector2() * 4f;
                Main.EntitySpriteDraw(texture, drawPosition + backglowOffset, null, backglowColor * Projectile.Opacity, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
            }
            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(Color.White) * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            if (Timer <= MagicGlowTimer)
            {
                backglowColor = Color.HotPink * (1 - Timer / MagicGlowTimer);
                for (int i = 0; i < 3; i++)
                    Main.EntitySpriteDraw(texture, drawPosition, null, backglowColor * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }
        }

        public void PrepareSpriteBatch(SpriteBatch spriteBatch)
        {
            spriteBatch.EnforceCutoffRegion(new(0, 0, Main.screenWidth, Main.screenHeight), Main.GameViewMatrix.TransformationMatrix, SpriteSortMode.Immediate, BlendState.Additive);
        }
    }
}
