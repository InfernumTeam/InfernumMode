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
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 7;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0.55f, 0.25f, 0f);

            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            Projectile.velocity = (Projectile.velocity * 29f + Projectile.SafeDirectionTo(target.Center) * 16f) / 30f;
            Projectile.Opacity = Utils.GetLerpValue(0f, 45f, Projectile.timeLeft, true);
            if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.WithinRange(target.Center, 70f))
            {
                Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<EnergyBlast>(), 120, 0f);
                Projectile.Kill();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2[] baseOldPositions = Projectile.oldPos.Where(oldPos => oldPos != Vector2.Zero).ToArray();
            if (baseOldPositions.Length <= 2)
                return true;

            Texture2D projectileTexture = Utilities.ProjTexture(Projectile.type);
            Vector2 origin = projectileTexture.Size() * 0.5f;
            List<Vector2> adjustedOldPositions = new BezierCurve(baseOldPositions).GetPoints(40);
            for (int i = 0; i < adjustedOldPositions.Count; i++)
            {
                float completionRatio = i / (float)adjustedOldPositions.Count;
                float scale = Projectile.scale * (float)Math.Pow(MathHelper.Lerp(1f, 0.4f, completionRatio), 2D);
                Color drawColor = Color.Lerp(Color.Red, Color.Purple, completionRatio) * (1f - completionRatio) * Projectile.Opacity * 0.8f;
                Vector2 drawPosition = adjustedOldPositions[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.spriteBatch.Draw(projectileTexture, drawPosition, null, drawColor, Projectile.rotation, origin, scale, SpriteEffects.None, 0f);
            }
            return false;
        }
    }
}
