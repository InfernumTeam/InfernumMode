using CalamityMod;
using InfernumMode.Buffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Ravager
{
    public class GroundBloodCrystal : ModProjectile
    {
        public int TotalCrystals => (int)projectile.ai[0];

        public ref float Time => ref projectile.ai[1];

        public const float DisplacementBetweenCrystals = 56f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Blood Crystal");
            Main.projFrames[projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            projectile.width = 30;
            projectile.height = 30;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 420;
            projectile.penetrate = -1;
            projectile.Calamity().canBreakPlayerDefense = true;
            projectile.hide = true;
        }

        public override void AI()
        {
            projectile.Opacity = (float)CalamityUtils.Convert01To010(projectile.timeLeft / 420f) * 8f;
            if (projectile.Opacity > 1f)
                projectile.Opacity = 1f;
            Time++;

            // Play crystal sounds.
            if (Time % 8f == 7f)
                Main.PlaySound(SoundID.DD2_CrystalCartImpact, projectile.Center);
        }

        public override bool CanDamage() => projectile.Opacity > 0.75f;

        public override Color? GetAlpha(Color lightColor) => new Color(1f, 0.3f, 0.3f, 0.4f) * projectile.Opacity;

        public Vector2 GetCrystalPosition(int index)
        {
            float crystalInterpolant = index / (float)(TotalCrystals - 1f);
            float sine = (float)Math.Sin(Time / 9f + MathHelper.TwoPi * crystalInterpolant) * 0.5f + 0.5f;
            float verticalOffsetInterpolant = Utils.InverseLerp(0f, 0.3f, crystalInterpolant, true) * Utils.InverseLerp(1f, 0.7f, crystalInterpolant, true);
            Vector2 crystalPosition = projectile.Center + Vector2.UnitX * index * Math.Sign(projectile.velocity.X) * 36f;
            crystalPosition.Y -= sine * verticalOffsetInterpolant * 200f;
            return crystalPosition;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Color color = projectile.GetAlpha(lightColor);
            for (int i = 0; i < TotalCrystals; i++)
            {
                Vector2 drawPosition = GetCrystalPosition(i) - Main.screenPosition;
                spriteBatch.Draw(texture, drawPosition, null, color, 0f, texture.Size() * 0.5f, projectile.scale, 0, 0f);
            }
            return false;
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI)
        {
            drawCacheProjsBehindProjectiles.Add(index);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < TotalCrystals; i++)
            {
                Rectangle hitbox = Utils.CenteredRectangle(GetCrystalPosition(i), projectile.Size);
                if (targetHitbox.Intersects(hitbox))
                    return true;
            }
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<DarkFlames>(), 180);
    }
}
