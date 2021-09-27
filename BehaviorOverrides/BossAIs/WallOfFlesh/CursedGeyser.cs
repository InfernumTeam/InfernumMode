using CalamityMod;
using InfernumMode.ILEditingStuff;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Shaders;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.WallOfFlesh
{
    public class CursedGeyser : ModProjectile
    {
        internal PrimitiveTrailCopy TentacleDrawer;
        internal ref float Time => ref projectile.ai[0];
        internal ref float GeyserHeight => ref projectile.ai[1];
        internal const float MaxGeyserHeight = 660f;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("The Fountain of Pain");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 20;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.netImportant = true;
            projectile.timeLeft = 120;
        }

        public override void AI()
        {
            projectile.Opacity = (float)Math.Sin(projectile.timeLeft / 120f * MathHelper.Pi) * 4f;
            if (projectile.Opacity > 1f)
                projectile.Opacity = 1f;

            if (GeyserHeight < 2f)
                GeyserHeight = 2f;

            if (Time > 35f)
                GeyserHeight = MathHelper.Lerp(GeyserHeight, GeyserHeight * 1.44f, 0.123f);
            else
                projectile.timeLeft++;

            if (Time == 0f)
            {
                Player closestPlayer = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
                Main.PlaySound(SoundID.Item73, closestPlayer.Center);
            }

            Time++;

            CreateVisuals();
            if (Main.netMode != NetmodeID.MultiplayerClient && Time >= 60f && Time <= 80f && Time % 6f == 5f)
                Utilities.NewProjectileBetter(projectile.Center, Main.rand.NextVector2CircularEdge(8f, 8f), ModContent.ProjectileType<CursedSoul>(), 55, 0f);
        }

        internal void CreateVisuals()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            // Making bubbling lava as a subtle indicator.
            if (Time % 4f == 3f && Time < 60f)
            {
                Vector2 dustSpawnPosition = projectile.Center - Vector2.UnitY * (GeyserHeight + 8f);
                dustSpawnPosition.X += Main.rand.NextFloatDirection() * WidthFunction(1f);
                Dust bubble = Dust.NewDustPerfect(dustSpawnPosition, 267, Vector2.UnitY * -12f);
                bubble.noGravity = true;
                bubble.scale = 2.6f;
                bubble.color = ColorFunction(1f);
            }

            // As well as liquid disruption.
            WaterShaderData ripple = (WaterShaderData)Filters.Scene["WaterDistortion"].GetShader();
            Vector2 ripplePos = projectile.Center - Vector2.UnitY * (GeyserHeight + 45f);
            Color waveData = ColorFunction(1f);
            ripple.QueueRipple(ripplePos, waveData, Vector2.One * 75f, RippleShape.Square, -MathHelper.PiOver2);
        }

        internal Color ColorFunction(float completionRatio)
		{
            Color baseColor = ILEditingChanges.BlendLavaColors(Color.OrangeRed);
            return baseColor * projectile.Opacity;
		}

        internal float WidthFunction(float completionRatio) => MathHelper.Lerp(56f, 63f, (float)Math.Abs(Math.Cos(Main.GlobalTime * 2f))) * projectile.Opacity;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            float width = WidthFunction(0.6f);
            Vector2 start = projectile.Center;
            Vector2 end = projectile.Center - Vector2.UnitY * GeyserHeight;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, width, ref _);
        }

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (TentacleDrawer is null)
                TentacleDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["Infernum:WoFGeyserTexture"]);

            GameShaders.Misc["Infernum:WoFGeyserTexture"].UseSaturation(-1f);
            GameShaders.Misc["Infernum:WoFGeyserTexture"].UseColor(ILEditingChanges.BlendLavaColors(Color.Orange));
            GameShaders.Misc["Infernum:WoFGeyserTexture"].SetShaderTexture(ModContent.GetTexture("Terraria/Misc/Perlin"));

            List<Vector2> points = new List<Vector2>();
            for (int i = 0; i <= 8; i++)
                points.Add(Vector2.Lerp(projectile.Center, projectile.Center - Vector2.UnitY * GeyserHeight, i / 8f));
            TentacleDrawer.Draw(new BezierCurveCopy(points.ToArray()).GetPoints(20), Vector2.UnitX * 10f - Main.screenPosition, 35);

            GameShaders.Misc["Infernum:WoFGeyserTexture"].UseSaturation(1f);
            TentacleDrawer.Draw(new BezierCurveCopy(points.ToArray()).GetPoints(20), Vector2.UnitX * -10f - Main.screenPosition, 35);

            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)	
        {
			target.Calamity().lastProjectileHit = projectile;
		}
	}
}
