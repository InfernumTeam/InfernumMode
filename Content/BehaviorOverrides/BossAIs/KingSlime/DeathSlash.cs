using CalamityMod;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.KingSlime
{
    public class DeathSlash : ModProjectile
    {
        internal PrimitiveTrail SlashDrawer;

        public List<Vector2> TrailCache = new();

        public float ScaleFactorDelta => Projectile.localAI[0];

        public ref float CurrentVerticalOffset => ref Projectile.ai[0];

        public ref float Time => ref Projectile.ai[1];

        public const int Lifetime = 300;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Ninja Slice");

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.timeLeft = Projectile.MaxUpdates * Lifetime;
        }
        public override void AI()
        {
            // Disappear if the ninja is not present.
            int ninjaIndex = NPC.FindFirstNPC(ModContent.NPCType<Ninja>());
            if (!Main.npc.IndexInRange(ninjaIndex))
            {
                Projectile.Kill();
                return;
            }

            // Get the ninja, and check whether we should be sticking to it.
            NPC ninjaNPC = Main.npc[ninjaIndex];
            bool stickToNinja = ninjaNPC.Infernum().ExtraAI[11] == Projectile.whoAmI && ninjaNPC.velocity != Vector2.Zero;

            if (stickToNinja)
            {
                // Add our current position to the List of Vectors to draw.
                TrailCache.Add(Projectile.Center);
                // Update our position to be accurate to the ninjas.
                Projectile.Center = ninjaNPC.Center + Vector2.UnitY * CurrentVerticalOffset + ninjaNPC.velocity;
                // Randomly change our offset.
                if (Main.rand.NextBool(4))
                {
                    float newIdealOffset = Main.rand.NextBool().ToDirectionInt() * Main.rand.NextFloat(4f, 28f);
                    CurrentVerticalOffset = MathHelper.Lerp(CurrentVerticalOffset, newIdealOffset, 0.667f);

                    Projectile.netUpdate = true;
                }
            }

            // Cap the amount of Vectors in the list at 20.
            if (TrailCache.Count > 20)
                TrailCache.RemoveAt(0);

            // If the ninja isnt moving, quickly clear the oldest Vector in the list.
            // This makes the projectile come back into the ninja. They both then disappear afterwards, done in the ninjas code.
            if (ninjaNPC.velocity == Vector2.Zero)
            {
                TrailCache.RemoveAt(0);
            }
            // Fade in.
            float disappearInterpolant = Utils.GetLerpValue(0f, 24f, Projectile.timeLeft / Projectile.MaxUpdates, true);
            float scaleGrowInterpolant = (float)Math.Pow(Utils.GetLerpValue(0f, 64f, Time, true), 1.72);
            Projectile.Opacity = Utils.GetLerpValue(0f, 24f, Time / Projectile.MaxUpdates, true) * disappearInterpolant;
            Projectile.scale = MathHelper.Lerp(0.24f, 1f, scaleGrowInterpolant) * disappearInterpolant;
            Time++;
        }
        public override bool? CanDamage() => false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < TrailCache.Count; i++)
            {
                if (Utils.CenteredRectangle(TrailCache[i], Vector2.One * WidthFunction(i / (float)(TrailCache.Count - 1f) * 0.7f)).Intersects(targetHitbox))
                {
                    return true;
                }

            }
            return false;
        }

        internal float WidthFunction(float completionRatio)
        {
            float baseWidth = MathHelper.Lerp(32f, 33f, (float)Math.Sin(MathHelper.Pi * 4f * completionRatio) * 0.5f + 0.5f) * Projectile.scale;
            return CalamityUtils.Convert01To010(completionRatio) * baseWidth * (1f + ScaleFactorDelta) * 0.5f;
        }

        internal Color ColorFunction(float completionRatio)
        {
            float opacity = CalamityUtils.Convert01To010(completionRatio);
            if (opacity >= 1f)
                opacity = 1f;
            opacity *= Projectile.Opacity * 0.18f;
            return Color.White * opacity;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            SlashDrawer ??= new(WidthFunction, ColorFunction, null, InfernumEffectsRegistry.RealityTearVertexShader);

            InfernumEffectsRegistry.RealityTearVertexShader.SetShaderTexture(InfernumTextureRegistry.GrayscaleWater);
            InfernumEffectsRegistry.RealityTearVertexShader.Shader.Parameters["useOutline"].SetValue(true);
            SlashDrawer.Draw(TrailCache, Projectile.Size * 0.5f - Main.screenPosition, 60);
            return false;
        }
    }
}
