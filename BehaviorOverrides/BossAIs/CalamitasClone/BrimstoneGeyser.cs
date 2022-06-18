using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.CalamitasClone
{
    public class BrimstoneGeyser : ModProjectile
    {
        internal PrimitiveTrailCopy LavaDrawer;
        internal ref float Time => ref projectile.ai[0];
        internal ref float GeyserHeight => ref projectile.ai[1];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brimstone Lava");
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

            if (Time > 20f)
                GeyserHeight = MathHelper.Lerp(GeyserHeight, GeyserHeight * 1.45f, 0.2f) + 2f;
            else
                projectile.timeLeft++;

            if (Time == 0f)
            {
                Player closestPlayer = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
                Main.PlaySound(SoundID.Item73, closestPlayer.Center);
            }

            Time++;
        }

        internal Color ColorFunction(float completionRatio) => Color.Red * projectile.Opacity;

        internal float WidthFunction(float completionRatio) => MathHelper.Lerp(56f, 63f, (float)Math.Abs(Math.Cos(Main.GlobalTime * 2f))) * Utils.InverseLerp(20f, 270f, GeyserHeight, true) * projectile.Opacity;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            float width = WidthFunction(0.6f);
            Vector2 start = projectile.Center - Vector2.UnitY * GeyserHeight * 0.15f;
            Vector2 end = projectile.Center - Vector2.UnitY * GeyserHeight * 0.7f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, width, ref _);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (Time < 20f)
            {
                float telegraphFade = Utils.InverseLerp(0f, 8f, Time, true) * Utils.InverseLerp(20f, 10f, Time, true);
                float telegraphWidth = telegraphFade * 4f;
                Color telegraphColor = Color.Lerp(Color.Purple, Color.DarkRed, 0.375f) * telegraphFade;
                Vector2 start = projectile.Center - Vector2.UnitY * 2000f;
                Vector2 end = projectile.Center + Vector2.UnitY * 2000f;
                Utilities.DrawLineBetter(spriteBatch, start, end, telegraphColor, telegraphWidth);
            }

            if (LavaDrawer is null)
                LavaDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["Infernum:WoFGeyserTexture"]);

            GameShaders.Misc["Infernum:WoFGeyserTexture"].UseSaturation(-1f);
            GameShaders.Misc["Infernum:WoFGeyserTexture"].UseColor(Color.Orange);
            GameShaders.Misc["Infernum:WoFGeyserTexture"].SetShaderTexture(ModContent.GetTexture("Terraria/Misc/Perlin"));

            List<Vector2> points = new List<Vector2>();
            for (int i = 0; i < 25; i++)
                points.Add(Vector2.Lerp(projectile.Center, projectile.Center - Vector2.UnitY * GeyserHeight, i / 24f));
            LavaDrawer.Draw(points, Vector2.UnitX * 10f - Main.screenPosition, 35);

            GameShaders.Misc["Infernum:WoFGeyserTexture"].UseSaturation(1f);
            LavaDrawer.Draw(points, Vector2.UnitX * -10f - Main.screenPosition, 35);

            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
