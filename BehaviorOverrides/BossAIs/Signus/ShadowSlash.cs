using CalamityMod;
using InfernumMode.ILEditingStuff;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Signus
{
    public class ShadowSlash : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shadow Slash");
        }

        public override void SetDefaults()
        {
            projectile.width = 500;
            projectile.height = 100;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 30;
            projectile.hide = true;
            projectile.Calamity().canBreakPlayerDefense = true;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.Opacity = projectile.timeLeft / 30f;
            projectile.scale = Utils.InverseLerp(30f, 25f, projectile.timeLeft, true);
            projectile.scale *= MathHelper.Lerp(0.7f, 1.1f, projectile.identity % 6f / 6f) * 0.5f;
            projectile.rotation = projectile.ai[0];
        }

        public override Color? GetAlpha(Color lightColor) => Color.DarkViolet * projectile.Opacity * 1.4f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            Vector2 start = projectile.Center - projectile.rotation.ToRotationVector2() * projectile.width * projectile.scale * 0.5f;
            Vector2 end = projectile.Center + projectile.rotation.ToRotationVector2() * projectile.width * projectile.scale * 0.5f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, projectile.height * 0.5f, ref _);
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI)
        {
            DrawBlackEffectHook.DrawCacheProjsOverSignusBlackening.Add(index);
        }
    }
}
