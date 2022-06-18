using CalamityMod.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Twins
{
    public class HomingEnergyOrb : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Energy");
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 7;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 40;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 180;
        }

        public override void AI()
        {
            Lighting.AddLight(projectile.Center, 0.55f, 0.25f, 0f);

            Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
            projectile.velocity = (projectile.velocity * 29f + projectile.SafeDirectionTo(target.Center) * 16f) / 30f;
            projectile.Opacity = Utils.InverseLerp(0f, 45f, projectile.timeLeft, true);
            if (Main.netMode != NetmodeID.MultiplayerClient && projectile.WithinRange(target.Center, 70f))
            {
                Utilities.NewProjectileBetter(projectile.Center, Vector2.Zero, ModContent.ProjectileType<EnergyBlast>(), 120, 0f);
                projectile.Kill();
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2[] baseOldPositions = projectile.oldPos.Where(oldPos => oldPos != Vector2.Zero).ToArray();
            if (baseOldPositions.Length <= 2)
                return true;

            Texture2D projectileTexture = Main.projectileTexture[projectile.type];
            Vector2 origin = projectileTexture.Size() * 0.5f;
            List<Vector2> adjustedOldPositions = new BezierCurve(baseOldPositions).GetPoints(40);
            for (int i = 0; i < adjustedOldPositions.Count; i++)
            {
                float completionRatio = i / (float)adjustedOldPositions.Count;
                float scale = projectile.scale * (float)Math.Pow(MathHelper.Lerp(1f, 0.4f, completionRatio), 2D);
                Color drawColor = Color.Lerp(Color.Red, Color.Purple, completionRatio) * (1f - completionRatio) * projectile.Opacity * 0.8f;
                Vector2 drawPosition = adjustedOldPositions[i] + projectile.Size * 0.5f - Main.screenPosition;
                spriteBatch.Draw(projectileTexture, drawPosition, null, drawColor, projectile.rotation, origin, scale, SpriteEffects.None, 0f);
            }
            return false;
        }
    }
}
