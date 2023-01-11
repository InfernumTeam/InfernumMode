using CalamityMod.DataStructures;
using InfernumMode.Assets.Effects;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Shaders;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.WallOfFlesh
{
    public class CursedGeyser : ModProjectile, IPixelPrimitiveDrawer
    {
        internal PrimitiveTrailCopy TentacleDrawer;
        internal ref float Time => ref Projectile.ai[0];
        internal ref float GeyserHeight => ref Projectile.ai[1];
        internal const float MaxGeyserHeight = 660f;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("The Fountain of Pain");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.timeLeft = 120;
        }

        public override void AI()
        {
            Projectile.Opacity = (float)Math.Sin(Projectile.timeLeft / 120f * MathHelper.Pi) * 4f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;

            if (GeyserHeight < 2f)
                GeyserHeight = 2f;

            if (Time > 55f)
                GeyserHeight = MathHelper.Lerp(GeyserHeight, GeyserHeight * 1.44f, 0.1243f) + 2f;
            else
                Projectile.timeLeft++;

            if (Time == 0f)
            {
                Player closestPlayer = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                SoundEngine.PlaySound(SoundID.Item73, closestPlayer.Center);
            }

            Time++;

            CreateVisuals();
            if (Main.netMode != NetmodeID.MultiplayerClient && Time >= 60f && Time <= 80f && Time % 6f == 5f)
                Utilities.NewProjectileBetter(Projectile.Center, Main.rand.NextVector2CircularEdge(8f, 8f), ModContent.ProjectileType<CursedSoul>(), 55, 0f);
        }

        internal void CreateVisuals()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            // Making bubbling lava as a subtle indicator.
            if (Time % 4f == 3f && Time < 60f)
            {
                Vector2 dustSpawnPosition = Projectile.Center - Vector2.UnitY * (GeyserHeight + 8f);
                dustSpawnPosition.X += Main.rand.NextFloatDirection() * WidthFunction(1f);
                Dust bubble = Dust.NewDustPerfect(dustSpawnPosition, 267, Vector2.UnitY * -12f);
                bubble.noGravity = true;
                bubble.scale = 2.6f;
                bubble.color = ColorFunction(1f);
            }

            // As well as liquid disruption.
            WaterShaderData ripple = (WaterShaderData)Filters.Scene["WaterDistortion"].GetShader();
            Vector2 ripplePos = Projectile.Center - Vector2.UnitY * (GeyserHeight + 45f);
            Color waveData = ColorFunction(1f);
            ripple.QueueRipple(ripplePos, waveData, Vector2.One * 75f, RippleShape.Square, -MathHelper.PiOver2);
        }

        internal Color ColorFunction(float completionRatio)
        {
            return Color.OrangeRed * Projectile.Opacity;
        }

        internal float WidthFunction(float completionRatio) => MathHelper.Lerp(56f, 63f, (float)Math.Abs(Math.Cos(Main.GlobalTimeWrappedHourly * 2f))) * Projectile.Opacity;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            float width = WidthFunction(0.6f);
            Vector2 start = Projectile.Center;
            Vector2 end = Projectile.Center - Vector2.UnitY * GeyserHeight;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, width, ref _);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Time < 55f)
            {
                float telegraphFade = Utils.GetLerpValue(0f, 8f, Time, true) * Utils.GetLerpValue(55f, 45f, Time, true);
                float telegraphWidth = telegraphFade * 4f;
                Color telegraphColor = Color.Crimson * telegraphFade;
                Vector2 start = Projectile.Center - Vector2.UnitY * 2000f;
                Vector2 end = Projectile.Center + Vector2.UnitY * 2000f;
                Main.spriteBatch.DrawLineBetter(start, end, telegraphColor, telegraphWidth);
            }
            return false;
        }

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            TentacleDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.WoFGeyserVertexShader);

            InfernumEffectsRegistry.WoFGeyserVertexShader.UseSaturation(-1f);
            InfernumEffectsRegistry.WoFGeyserVertexShader.UseColor(Color.Lerp(Color.Orange, Color.Red, 0.5f));
            InfernumEffectsRegistry.WoFGeyserVertexShader.SetShaderTexture(ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin"));

            List<Vector2> points = new();
            for (int i = 0; i <= 8; i++)
                points.Add(Vector2.Lerp(Projectile.Center, Projectile.Center - Vector2.UnitY * GeyserHeight, i / 8f));
            TentacleDrawer.DrawPixelated(new BezierCurve(points.ToArray()).GetPoints(20), Vector2.UnitX * 10f - Main.screenPosition, 35);

            InfernumEffectsRegistry.WoFGeyserVertexShader.UseSaturation(1f);
            TentacleDrawer.DrawPixelated(new BezierCurve(points.ToArray()).GetPoints(20), Vector2.UnitX * -10f - Main.screenPosition, 35);
        }
    }
}
