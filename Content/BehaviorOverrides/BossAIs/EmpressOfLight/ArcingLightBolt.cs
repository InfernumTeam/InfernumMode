using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class ArcingLightBolt : ModProjectile, ISpecializedDrawRegion
    {
        public PrimitiveTrailCopy TrailDrawer
        {
            get;
            set;
        }

        public Color MyColor
        {
            get
            {
                Color color = Main.hslToRgb(Projectile.ai[1] % 1f, 1f, 0.56f) * Projectile.Opacity * 1.3f;
                if (EmpressOfLightBehaviorOverride.ShouldBeEnraged)
                    color = EmpressOfLightBehaviorOverride.GetDaytimeColor(Projectile.ai[1] % 1f) * Projectile.Opacity;

                color.A /= 10;
                return color;
            }
        }

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/Gleam";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Light Bolt");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.penetrate = -1;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 360;
            Projectile.Opacity = 0f;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (Projectile.timeLeft > 30)
                Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.1f, 0f, 1f);
            else
            {
                Projectile.Opacity = Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true);
                Projectile.velocity *= 0.95f;
            }

            // Find relevant entities.
            int empressIndex = NPC.FindFirstNPC(NPCID.HallowBoss);
            NPC lacewing = null;
            float distanceToLacewing = float.MaxValue;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].type != NPCID.EmpressButterfly || !Main.npc[i].active)
                    continue;

                distanceToLacewing = MathHelper.Min(distanceToLacewing, Projectile.Distance(Main.npc[i].Center));
                lacewing = Main.npc[i];
            }

            // Move towards the empress if performing the lacewing animation or a charge-up for certain attacks.
            bool moveToEmpress = lacewing is not null && empressIndex >= 0 && Main.npc[empressIndex].Opacity > 0.4f;
            float empressFlySpeed = 70f;
            float empressFlyAcceleration = 0.07f;
            bool bigProjectileIsPresent = Utilities.AnyProjectiles(ModContent.ProjectileType<TheMoon>());
            if (empressIndex >= 0 && Main.npc[empressIndex].ai[0] == (int)EmpressOfLightBehaviorOverride.EmpressOfLightAttackType.UltimateRainbow && !bigProjectileIsPresent)
            {
                empressFlySpeed = 30f;
                empressFlyAcceleration = 0.046f;
                moveToEmpress = true;
            }

            // Dissipate if close to a lacewing.
            if (distanceToLacewing <= 250f && Projectile.velocity.AngleBetween(Projectile.SafeDirectionTo(lacewing.Center)) < 0.3f && Projectile.timeLeft > 30 && !moveToEmpress)
                Projectile.timeLeft = 30;

            // Spin and accelerate over time.
            if (Projectile.timeLeft >= 270 && !moveToEmpress)
                Projectile.velocity = Projectile.velocity.RotatedBy(Projectile.ai[0] * MathHelper.TwoPi / 420f) * 1.0175f;

            if (moveToEmpress)
            {
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Main.npc[empressIndex].Center) * empressFlySpeed, empressFlyAcceleration);
                if (Projectile.WithinRange(Main.npc[empressIndex].Center, 400f) && Projectile.timeLeft > 24)
                    Projectile.timeLeft = 24;
            }
        }

        public override void Kill(int timeLeft)
        {
            int dustCount = 10;
            float angularOffset = Projectile.velocity.ToRotation();
            for (int i = 0; i < dustCount; i++)
            {
                Dust rainbowMagic = Dust.NewDustPerfect(Projectile.Center, 267);
                rainbowMagic.fadeIn = 1f;
                rainbowMagic.noGravity = true;
                rainbowMagic.color = Main.hslToRgb(Main.rand.NextFloat(), 0.9f, 0.6f) * 0.8f;
                if (i % 4 == 0)
                {
                    rainbowMagic.velocity = angularOffset.ToRotationVector2() * 3.2f;
                    rainbowMagic.scale = 1.2f;
                }
                else if (i % 2 == 0)
                {
                    rainbowMagic.velocity = angularOffset.ToRotationVector2() * 1.8f;
                    rainbowMagic.scale = 0.85f;
                }
                else
                {
                    rainbowMagic.velocity = angularOffset.ToRotationVector2();
                    rainbowMagic.scale = 0.8f;
                }
                angularOffset += MathHelper.TwoPi / dustCount;
                rainbowMagic.velocity += Projectile.velocity * Main.rand.NextFloat(0.5f);
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public Color ColorFunction(float completionRatio)
        {
            Color rainbow = Main.hslToRgb((completionRatio - Main.GlobalTimeWrappedHourly * 1.4f) % 1f, 1f, 0.5f);
            Color c = Color.Lerp(MyColor with { A = 255 }, rainbow, completionRatio) * (1f - completionRatio) * Projectile.Opacity;
            return c;
        }

        public float WidthFunction(float completionRatio)
        {
            float fade = (1f - completionRatio) * Utils.GetLerpValue(-0.03f, 0.1f, completionRatio, true);
            return MathHelper.SmoothStep(0f, 1f, fade) * Projectile.Opacity * 10f;
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void SpecialDraw(SpriteBatch spriteBatch)
        {
            // Initialize the telegraph drawer.
            TrailDrawer ??= new(WidthFunction, ColorFunction, specialShader: InfernumEffectsRegistry.PrismaticRayVertexShader);

            // Prepare trail data.
            InfernumEffectsRegistry.PrismaticRayVertexShader.UseOpacity(0.2f);
            InfernumEffectsRegistry.PrismaticRayVertexShader.UseImage1("Images/Misc/Perlin");
            Main.instance.GraphicsDevice.Textures[2] = InfernumTextureRegistry.StreakSolid.Value;

            // Draw the afterimage trail.
            TrailDrawer.Draw(Projectile.oldPos, Projectile.Size * 0.5f + Projectile.velocity.SafeNormalize(Vector2.Zero) * 6f - Main.screenPosition, 25);

            // Draw the gleam.
            Texture2D sparkleTexture = InfernumTextureRegistry.LargeStar.Value;
            Color sparkleColor = Color.Lerp(MyColor, Color.White, 0.4f) with { A = 255 };
            Vector2 drawCenter = Projectile.Center - Main.screenPosition;
            Vector2 origin = sparkleTexture.Size() * 0.5f;
            Vector2 sparkleScale = new Vector2(0.3f, 1f) * Projectile.Opacity * Projectile.scale * 0.12f;
            Vector2 orthogonalsparkleScale = new Vector2(0.3f, 1.6f) * Projectile.Opacity * Projectile.scale * 0.12f;
            Main.spriteBatch.Draw(sparkleTexture, drawCenter, null, sparkleColor, MathHelper.PiOver2 + Projectile.rotation, origin, orthogonalsparkleScale, 0, 0f);
            Main.spriteBatch.Draw(sparkleTexture, drawCenter, null, sparkleColor, Projectile.rotation, origin, sparkleScale, 0, 0f);
            Main.spriteBatch.Draw(sparkleTexture, drawCenter, null, sparkleColor, MathHelper.PiOver2 + Projectile.rotation, origin, orthogonalsparkleScale * 0.6f, 0, 0f);
            Main.spriteBatch.Draw(sparkleTexture, drawCenter, null, sparkleColor, Projectile.rotation, origin, sparkleScale * 0.6f, 0, 0f);
        }

        public void PrepareSpriteBatch(SpriteBatch spriteBatch)
        {
            Main.spriteBatch.EnforceCutoffRegion(new(0, 0, Main.screenWidth, Main.screenHeight), Main.GameViewMatrix.TransformationMatrix, SpriteSortMode.Immediate, BlendState.Additive);
        }
    }
}
