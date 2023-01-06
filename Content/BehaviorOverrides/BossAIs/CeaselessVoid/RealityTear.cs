using CalamityMod;
using CalamityMod.NPCs;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class RealityTear : ModProjectile
    {
        internal PrimitiveTrail LightningDrawer;

        public List<Vector2> TrailCache = new();

        public float ScaleFactorDelta => Projectile.localAI[0];

        public ref float CurrentVerticalOffset => ref Projectile.ai[0];

        public ref float Time => ref Projectile.ai[1];

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
            Projectile.timeLeft = Projectile.MaxUpdates * 135;
        }

        public override void AI()
        {
            // Disappear if the Ceaseless Void is not present.
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.voidBoss))
            {
                Projectile.Kill();
                return;
            }

            NPC ceaselessVoid = Main.npc[CalamityGlobalNPC.voidBoss];
            bool stickToVoid = ceaselessVoid.Infernum().ExtraAI[0] == Projectile.whoAmI;

            if (stickToVoid)
            {
                TrailCache.Add(Projectile.Center);
                Projectile.Center = ceaselessVoid.Center + Vector2.UnitY * CurrentVerticalOffset + ceaselessVoid.velocity;
                if (Main.rand.NextBool(4))
                {
                    float newIdealOffset = Main.rand.NextBool().ToDirectionInt() * Main.rand.NextFloat(24f, 76f);
                    CurrentVerticalOffset = MathHelper.Lerp(CurrentVerticalOffset, newIdealOffset, 0.667f);

                    Projectile.netUpdate = true;
                }
            }

            // Create barrages of otherwordly magic from the tear.
            else if (Main.netMode != NetmodeID.MultiplayerClient && TrailCache.Count >= 2 && Time % 8f == 7f)
            {
                int barragePointIndex = Main.rand.Next(TrailCache.Count - 1);
                Vector2 barrageVelocity = Main.rand.NextVector2CircularEdge(8f, 8f);
                Vector2 barrageSpawnPosition = Vector2.Lerp(TrailCache[barragePointIndex], TrailCache[barragePointIndex + 1], Main.rand.NextFloat());
                Utilities.NewProjectileBetter(barrageSpawnPosition, barrageVelocity, ModContent.ProjectileType<CelestialBarrage>(), 250, 0f, -1, 0f, ScaleFactorDelta);
            }

            // Fade in.
            float disappearInterpolant = Utils.GetLerpValue(0f, 24f, Projectile.timeLeft / Projectile.MaxUpdates, true);
            float scaleGrowInterpolant = (float)Math.Pow(Utils.GetLerpValue(0f, 64f, Time, true), 1.72);
            Projectile.Opacity = Utils.GetLerpValue(0f, 24f, Time / Projectile.MaxUpdates, true) * disappearInterpolant;
            Projectile.scale = MathHelper.Lerp(0.24f, 1f, scaleGrowInterpolant) * disappearInterpolant;
            Time++;
        }

        #region Drawing
        internal float WidthFunction(float completionRatio)
        {
            float baseWidth = MathHelper.Lerp(72f, 73f, (float)Math.Sin(MathHelper.Pi * 4f * completionRatio) * 0.5f + 0.5f) * Projectile.scale;
            return CalamityUtils.Convert01To010(completionRatio) * baseWidth * (1f + ScaleFactorDelta);
        }

        internal Color ColorFunction(float completionRatio)
        {
            float opacity = CalamityUtils.Convert01To010(completionRatio) * 1.4f;
            if (opacity >= 1f)
                opacity = 1f;
            opacity *= Projectile.Opacity;
            return Color.White * opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LightningDrawer = new(WidthFunction, ColorFunction, null, InfernumEffectsRegistry.RealityTearVertexShader);

            InfernumEffectsRegistry.RealityTearVertexShader.SetShaderTexture(InfernumTextureRegistry.Stars);
            InfernumEffectsRegistry.RealityTearVertexShader.Shader.Parameters["useOutline"].SetValue(true);
            LightningDrawer.Draw(TrailCache, Projectile.Size * 0.5f - Main.screenPosition, 82);
            return false;
        }
        #endregion
    }
}
