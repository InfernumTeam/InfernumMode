using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark
{
    public class WaterSlice : ModProjectile
    {
        internal PrimitiveTrail LightningDrawer;

        public List<Vector2> TrailCache = new();

        public float ScaleFactorDelta => Projectile.localAI[0];

        public ref float CurrentVerticalOffset => ref Projectile.ai[0];

        public ref float Time => ref Projectile.ai[1];

        public const int Lifetime = 300;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Water Tear");

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.timeLeft = Projectile.MaxUpdates * Lifetime;
        }

        public override void AI()
        {
            // Disappear if the bereft vassal is not present.
            int vassalIndex = NPC.FindFirstNPC(ModContent.NPCType<BereftVassal>());
            if (!Main.npc.IndexInRange(vassalIndex))
            {
                Projectile.Kill();
                return;
            }

            NPC ceaselessVoid = Main.npc[vassalIndex];
            bool stickToVassal = ceaselessVoid.Infernum().ExtraAI[3] == Projectile.whoAmI;

            if (stickToVassal)
            {
                TrailCache.Add(Projectile.Center);
                Projectile.Center = ceaselessVoid.Center + Vector2.UnitY * CurrentVerticalOffset + ceaselessVoid.velocity;
                if (Main.rand.NextBool(4))
                {
                    float newIdealOffset = Main.rand.NextBool().ToDirectionInt() * Main.rand.NextFloat(4f, 28f);
                    CurrentVerticalOffset = MathHelper.Lerp(CurrentVerticalOffset, newIdealOffset, 0.667f);

                    Projectile.netUpdate = true;
                }
            }

            // Fade in.
            float disappearInterpolant = Utils.GetLerpValue(0f, 24f, Projectile.timeLeft / Projectile.MaxUpdates, true);
            float scaleGrowInterpolant = (float)Math.Pow(Utils.GetLerpValue(0f, 64f, Time, true), 1.72);
            Projectile.Opacity = Utils.GetLerpValue(0f, 24f, Time / Projectile.MaxUpdates, true) * disappearInterpolant;
            Projectile.scale = MathHelper.Lerp(0.24f, 1f, scaleGrowInterpolant) * disappearInterpolant;
            Time++;
        }

        public override bool? CanDamage() => Projectile.scale >= 0.9f ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < TrailCache.Count; i++)
            {
                if (Utils.CenteredRectangle(TrailCache[i], Vector2.One * WidthFunction(i / (float)(TrailCache.Count - 1f) * 0.7f)).Intersects(targetHitbox))
                    return true;
            }
            return false;
        }

        #region Drawing
        internal float WidthFunction(float completionRatio)
        {
            float baseWidth = MathHelper.Lerp(32f, 33f, (float)Math.Sin(MathHelper.Pi * 4f * completionRatio) * 0.5f + 0.5f) * Projectile.scale;
            return CalamityUtils.Convert01To010(completionRatio) * baseWidth * (1f + ScaleFactorDelta);
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
            LightningDrawer ??= new(WidthFunction, ColorFunction, null, GameShaders.Misc["Infernum:RealityTear"]);

            GameShaders.Misc["Infernum:RealityTear"].SetShaderTexture(InfernumTextureRegistry.Water);
            GameShaders.Misc["Infernum:RealityTear"].Shader.Parameters["useOutline"].SetValue(true);
            LightningDrawer.Draw(TrailCache, Projectile.Size * 0.5f - Main.screenPosition, 54);
            return false;
        }
        #endregion
    }
}
