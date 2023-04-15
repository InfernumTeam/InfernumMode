using CalamityMod;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Primitives;
using InfernumMode.Content.Items.Weapons.Rogue;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
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

        public override string Texture => "InfernumMode/Content/Items/Weapons/Rogue/WanderersShell";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dreamer's Energy Bolt");
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
            // This takes a small amount of time to happen, to allow the blade to go in its intended direction before immediately racing
            // towards the nearest target.
            else if (Time >= Dreamtastic.BeamNoHomeTime)
            {
                NPC potentialTarget = Projectile.Center.ClosestNPCAt(1600f);
                if (potentialTarget is not null)
                {
                    Vector2 idealVelocity = Projectile.SafeDirectionTo(potentialTarget.Center) * (Projectile.velocity.Length() + 3.3f);
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, idealVelocity, 0.07f).MoveTowards(idealVelocity, 0.5f);
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

        public float TrailWidth(float completionRatio)
        {
            float width = Utils.GetLerpValue(1f, 0.4f, completionRatio, true) * MathF.Sin(MathF.Acos(Utils.GetLerpValue(0.3f, 0f, completionRatio, true))) * Projectile.scale;
            return width * MaxWidth;
        }

        public static Color TrailColor(float completionRatio)
        {
            float colorInterpolant = completionRatio;
            return Color.Lerp(Color.Fuchsia, Color.Cyan, colorInterpolant);
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            if (!HasHitTarget)
            {
                Projectile.velocity *= 0.8f;
                HasHitTarget = true;
                Projectile.netUpdate = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Color mainColor = MulticolorLerp((Main.GlobalTimeWrappedHourly * 0.5f + Projectile.whoAmI * 0.12f) % 1, Color.MediumPurple, Color.Magenta, Color.Violet, Color.DeepSkyBlue);

            // Draw the bloom under the trail.
            Main.EntitySpriteDraw(bloomTexture, Projectile.oldPos[2] + Projectile.Size * 0.5f - Main.screenPosition, null, (mainColor * 0.1f) with { A = 0 }, 0, bloomTexture.Size() * 0.5f, 1.3f * Projectile.scale, 0, 0);
            Main.EntitySpriteDraw(bloomTexture, Projectile.oldPos[1] + Projectile.Size * 0.5f - Main.screenPosition, null, (mainColor * 0.5f) with { A = 0 }, 0, bloomTexture.Size() * 0.5f, 0.34f * Projectile.scale, 0, 0);

            // Initialize the trail drawer.
            var trailShader = GameShaders.Misc["CalamityMod:ExobladePierce"];
            TrailDrawer ??= new(TrailWidth, TrailColor, null, true, trailShader);

            // Draw the trail.
            trailShader.SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/BasicTrail"));
            trailShader.SetShaderTexture2(InfernumTextureRegistry.FireNoise);
            trailShader.UseColor(mainColor);
            trailShader.UseSecondaryColor(Color.Purple);
            TrailDrawer.Draw(Projectile.oldPos, Projectile.Size * 0.5f - Main.screenPosition, 45);

            // Draw the bloom above the trail.
            Main.EntitySpriteDraw(bloomTexture, Projectile.oldPos[2] + Projectile.Size * 0.5f - Main.screenPosition, null, (Color.White * 0.2f) with { A = 0 }, 0, bloomTexture.Size() * 0.5f, Projectile.scale * 1.2f, 0, 0);
            Main.EntitySpriteDraw(bloomTexture, Projectile.oldPos[1] + Projectile.Size * 0.5f - Main.screenPosition, null, (Color.White * 0.5f) with { A = 0 }, 0, bloomTexture.Size() * 0.5f, Projectile.scale * 0.44f, 0, 0);
            return false;
        }
    }
}
