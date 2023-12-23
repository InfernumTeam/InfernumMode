using CalamityMod;
using CalamityMod.Particles;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Primitives;
using InfernumMode.Content.Items.Weapons.Rogue;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.CalamityUtils;

namespace InfernumMode.Content.Projectiles.Rogue
{
    public class DreamtasticEnergyBolt : ModProjectile
    {
        public PrimitiveTrailCopy TrailDrawer
        {
            get;
            set;
        }

        public bool HasHitTarget
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value.ToInt();
        }

        public float MaxWidth => Projectile.ai[1];

        public ref float Time => ref Projectile.localAI[0];

        public override string Texture => InfernumTextureRegistry.InvisPath;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Dreamer's Energy Bolt");
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 36;
        }

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.minion = true;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.Opacity = 1f;
            Projectile.MaxUpdates = 2;
            Projectile.timeLeft = Projectile.MaxUpdates * 180;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = Projectile.MaxUpdates * 9;
            Projectile.DamageType = RogueDamageClass.Instance;
        }

        public override void AI()
        {
            // Slow down and dissipate if about to die.
            if (HasHitTarget)
            {
                Projectile.velocity = Projectile.velocity.RotatedBy((Projectile.identity % 2f == 0f).ToDirectionInt() * 0.06f) * 0.93f;
                if (Projectile.timeLeft >= 30)
                    Projectile.timeLeft = 30;
            }

            // Aim very, very quickly at targets.
            // This takes a small amount of time to happen, to allow the bolt to go in its intended direction before immediately racing
            // towards the nearest target.
            else if (Time >= Dreamtastic.BeamNoHomeTime)
            {
                NPC potentialTarget = Projectile.Center.ClosestNPCAt(1600f);
                if (potentialTarget is not null)
                {
                    Vector2 idealVelocity = Projectile.SafeDirectionTo(potentialTarget.Center) * (Projectile.velocity.Length() + 3.3f);
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, idealVelocity, 0.07f).MoveTowards(idealVelocity, 0.4f);
                }
                else
                    Projectile.velocity *= 0.99f;
            }

            Projectile.Size = Vector2.One * MaxWidth * 1.1f;
            Projectile.rotation = Projectile.velocity.ToRotation();

            // Generate energy dust.
            if (Main.rand.NextBool(2))
            {
                Color dustColor = Main.hslToRgb(Main.rand.NextFloat(), 1f, 0.8f);
                Dust energyDust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(20f, 20f) + Projectile.velocity, 267, Projectile.velocity * -0.6f, 0, dustColor);
                energyDust.scale = 0.3f;
                energyDust.fadeIn = Main.rand.NextFloat() * 1.2f;
                energyDust.noGravity = true;
            }

            // Fade away when close to dying.
            Projectile.scale = Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true);

            if (Projectile.FinalExtraUpdate())
                Time++;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!HasHitTarget)
            {
                Projectile.velocity *= 0.8f;
                HasHitTarget = true;
                Projectile.netUpdate = true;

                if (Main.netMode is NetmodeID.Server)
                    return;

                for (int i = 0; i < 2; i++)
                {
                    Vector2 position = Main.rand.NextVector2FromRectangle(target.Hitbox);
                    Vector2 velocity = target.SafeDirectionTo(position) * Main.rand.NextFloat(1f, 3f);
                    Color color = MulticolorLerp(Main.rand.NextFloat(), Color.MediumPurple, Color.Magenta, Color.Violet, Color.DeepSkyBlue);

                    if (Main.rand.NextBool())
                       GeneralParticleHandler.SpawnParticle(new SparkleParticle(position, velocity, color, Color.Lerp(color, Color.White, Main.rand.NextFloat(0.3f, 0.7f)), Main.rand.NextFloat(0.3f, 0.5f), 40, Main.rand.NextFloat(-0.05f, 0.05f), 5f));
                    else
                       GeneralParticleHandler.SpawnParticle(new GenericSparkle(position, velocity, color, Color.Lerp(color, Color.White, Main.rand.NextFloat(0.3f, 0.7f)), Main.rand.NextFloat(0.3f, 0.5f), 40, Main.rand.NextFloat(-0.05f, 0.05f), 5f));

                    if (Main.rand.NextBool())
                        GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(position, velocity * 1.3f, Main.rand.NextFloat(0.3f, 0.55f), color, 40, 1.5f, 2f, 3f, 0.04f));

                }

                Color color2 = MulticolorLerp(Main.rand.NextFloat(), Color.MediumPurple, Color.Magenta, Color.Violet, Color.DeepSkyBlue);
                GeneralParticleHandler.SpawnParticle(new StrongBloom(Main.rand.NextVector2FromRectangle(target.Hitbox), Vector2.Zero, color2 * 0.6f, Main.rand.NextFloat(0.7f, 1.1f), 30));
            }
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 6; i++)
            {
                Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width * 0.5f, Projectile.height * 0.5f);
                Color color = Color.Lerp(Color.Cyan, Color.Fuchsia, Main.rand.NextFloat(1f));
                Color bloomColor = Color.Lerp(color, Color.White, Main.rand.NextFloat(0.3f, 0.5f));
                CritSpark spark = new(position, Projectile.Center.DirectionTo(position) * Main.rand.NextFloat(3f, 5f), color, bloomColor, Main.rand.NextFloat(0.5f, 0.7f), Main.rand.Next(25, 35));
                GeneralParticleHandler.SpawnParticle(spark);
            }

            //Vector2 scale = Vector2.One * Main.rand.NextFloat(2.7f, 3f);
            //Color color2 = Color.HotPink;
            //Color bloomColor2 = Color.Lerp(color2, Color.White, Main.rand.NextFloat(0.3f, 0.5f));
            ////GeneralParticleHandler.SpawnParticle(new FlareShine(Projectile.Center, Vector2.Zero, color2, bloomColor2, 0f, scale, Vector2.Zero, 50, bloomScale: 5f));
            //GeneralParticleHandler.SpawnParticle(new StrongBloom(Projectile.Center, Vector2.Zero, color2 * 0.6f, Main.rand.NextFloat(0.3f, 0.6f), 30));

        }

        public float TrailWidth(float completionRatio)
        {
            float tipInterpolant = Sqrt(1f - Pow(Utils.GetLerpValue(0.3f, 0f, completionRatio, true), 2f));
            float width = Utils.GetLerpValue(1f, 0.4f, completionRatio, true) * tipInterpolant * Projectile.scale;
            return width * MaxWidth;
        }

        public Color TrailColor(float completionRatio)
        {
            float colorInterpolant = completionRatio;
            return Color.Lerp(Color.Fuchsia, Color.Cyan, colorInterpolant) * Utils.GetLerpValue(1.2f, 5f, Projectile.velocity.Length(), true);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float bloomScale = Utils.GetLerpValue(0.8f, 2.4f, Projectile.velocity.Length(), true) * Projectile.scale;
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Color mainColor = MulticolorLerp((Main.GlobalTimeWrappedHourly * 0.5f + Projectile.whoAmI * 0.12f) % 1, Color.MediumPurple, Color.Magenta, Color.Violet, Color.DeepSkyBlue);

            // Draw the bloom under the trail.
            Main.EntitySpriteDraw(bloomTexture, Projectile.oldPos[2] + Projectile.Size * 0.5f - Main.screenPosition, null, (mainColor * 0.1f) with { A = 0 }, 0, bloomTexture.Size() * 0.5f, bloomScale * 1.3f, 0, 0);
            Main.EntitySpriteDraw(bloomTexture, Projectile.oldPos[1] + Projectile.Size * 0.5f - Main.screenPosition, null, (mainColor * 0.5f) with { A = 0 }, 0, bloomTexture.Size() * 0.5f, bloomScale * 0.34f, 0, 0);

            // Initialize the trail drawer.
            var trailShader = GameShaders.Misc["CalamityMod:ExobladePierce"];
            TrailDrawer ??= new(TrailWidth, TrailColor, null, true, trailShader);

            // Draw the trail.
            trailShader.SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/BasicTrail"));
            trailShader.SetShaderTexture2(InfernumTextureRegistry.SmokyNoise);
            trailShader.UseColor(mainColor);
            trailShader.UseSecondaryColor(Color.Purple);
            TrailDrawer.Draw(Projectile.oldPos, Projectile.Size * 0.5f - Main.screenPosition, 45);

            // Draw the bloom above the trail.
            Main.EntitySpriteDraw(bloomTexture, Projectile.oldPos[2] + Projectile.Size * 0.5f - Main.screenPosition, null, (Color.White * 0.2f) with { A = 0 }, 0, bloomTexture.Size() * 0.5f, bloomScale * 1.2f, 0, 0);
            Main.EntitySpriteDraw(bloomTexture, Projectile.oldPos[1] + Projectile.Size * 0.5f - Main.screenPosition, null, (Color.White * 0.5f) with { A = 0 }, 0, bloomTexture.Size() * 0.5f, bloomScale * 0.44f, 0, 0);
            return false;
        }
    }
}
