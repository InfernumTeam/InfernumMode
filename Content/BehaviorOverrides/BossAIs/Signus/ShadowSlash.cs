using CalamityMod;
using InfernumMode.Core.ILEditingStuff;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Signus
{
    public class ShadowSlash : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shadow Slash");
        }

        public override void SetDefaults()
        {
            Projectile.width = 500;
            Projectile.height = 100;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 30;
            Projectile.hide = true;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.Opacity = Projectile.timeLeft / 30f;
            Projectile.scale = Utils.GetLerpValue(30f, 25f, Projectile.timeLeft, true);
            Projectile.scale *= MathHelper.Lerp(0.7f, 1.1f, Projectile.identity % 6f / 6f) * 0.5f;
            Projectile.rotation = Projectile.ai[0];
        }

        public override Color? GetAlpha(Color lightColor) => Color.DarkViolet * Projectile.Opacity * 1.4f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            Vector2 start = Projectile.Center - Projectile.rotation.ToRotationVector2() * Projectile.width * Projectile.scale * 0.5f;
            Vector2 end = Projectile.Center + Projectile.rotation.ToRotationVector2() * Projectile.width * Projectile.scale * 0.5f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, Projectile.height * 0.5f, ref _);
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI, List<int> overWiresUI)
        {
            DrawBlackEffectHook.DrawCacheProjsOverSignusBlackening.Add(index);
        }
    }
}
