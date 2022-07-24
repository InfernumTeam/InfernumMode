using CalamityMod;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class RealityTear : ModProjectile
    {
        internal PrimitiveTrailCopy LightningDrawer;

        public List<Vector2> TrailCache = new();

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
            Projectile.timeLeft = Projectile.MaxUpdates * 150;
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
            else if (Main.netMode != NetmodeID.MultiplayerClient && TrailCache.Count >= 2 && Time % 10f == 9f)
            {
                int barragePointIndex = Main.rand.Next(TrailCache.Count - 1);
                Vector2 barrageVelocity = Main.rand.NextVector2CircularEdge(8f, 8f);
                Vector2 barrageSpawnPosition = Vector2.Lerp(TrailCache[barragePointIndex], TrailCache[barragePointIndex + 1], Main.rand.NextFloat());
                Utilities.NewProjectileBetter(barrageSpawnPosition, barrageVelocity, ModContent.ProjectileType<CelestialBarrage>(), 250, 0f);
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
            return CalamityUtils.Convert01To010(completionRatio) * baseWidth;
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
            if (LightningDrawer is null)
                LightningDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["Infernum:RealityTear"]);

            GameShaders.Misc["Infernum:RealityTear"].SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/Stars"));
            LightningDrawer.Draw(TrailCache, Projectile.Size * 0.5f - Main.screenPosition, 82);
            return false;
        }
        #endregion
    }
}
