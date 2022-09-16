using CalamityMod;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class RealitySlice : ModProjectile
    {
        internal PrimitiveTrailCopy LightningDrawer;

        public bool Cosmilite;

        public Vector2 Start;

        public Vector2 End;

        public List<Vector2> TrailCache = new();
        
        public ref float Time => ref Projectile.ai[0];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Reality Tear");

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 84;
            Projectile.MaxUpdates = 2;
        }

        public override void AI()
        {
            // Disappear if neither the Ceaseless Void nor DoG not present.
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.voidBoss) && !Main.npc.IndexInRange(CalamityGlobalNPC.DoGHead))
            {
                Projectile.Kill();
                return;
            }

            float sliceInterpolant = (float)Math.Pow(Utils.GetLerpValue(0f, 27f, Time, true), 1.6);
            Projectile.Center = Vector2.Lerp(Start, End, sliceInterpolant);
            if (Time <= 27f)
                TrailCache.Add(Projectile.Center);

            // Fade in.
            float disappearInterpolant = Utils.GetLerpValue(0f, 16f, Projectile.timeLeft / Projectile.MaxUpdates, true);
            float scaleGrowInterpolant = (float)Math.Pow(Utils.GetLerpValue(0f, 15f, Time, true), 1.72);
            Projectile.Opacity = Utils.GetLerpValue(0f, 24f, Time / Projectile.MaxUpdates, true) * disappearInterpolant;
            Projectile.scale = MathHelper.Lerp(0.24f, 1f, scaleGrowInterpolant) * disappearInterpolant;
            Time++;
        }

        #region Drawing
        internal float WidthFunction(float completionRatio)
        {
            float width = Cosmilite ? 80f : 40f;
            return CalamityUtils.Convert01To010(completionRatio) * Projectile.scale * width;
        }

        internal Color ColorFunction(float completionRatio)
        {
            Color baseColor = Color.White;
            if (Cosmilite)
                baseColor = (Projectile.localAI[0] == 0f ? Color.Cyan : Color.Fuchsia) with { A = 0 };

            float opacity = CalamityUtils.Convert01To010(completionRatio) * 1.4f;
            if (opacity >= 1f)
                opacity = 1f;
            opacity *= Projectile.Opacity;
            return baseColor * opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (LightningDrawer is null)
                LightningDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["Infernum:RealityTear"]);

            GameShaders.Misc["Infernum:RealityTear"].SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/Stars"));
            GameShaders.Misc["Infernum:RealityTear"].Shader.Parameters["useOutline"].SetValue(true);

            Projectile.localAI[0] = 0f;
            LightningDrawer.Draw(TrailCache, Projectile.Size * 0.5f - Main.screenPosition, 50);
            if (Cosmilite)
            {
                Projectile.localAI[0] = 1f;
                LightningDrawer.Draw(TrailCache, Projectile.Size * 0.5f - Main.screenPosition, 50);
            }

            return false;
        }
        #endregion
    }
}
