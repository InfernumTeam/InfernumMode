using CalamityMod;
using CalamityMod.DataStructures;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Generic
{
    public class PortalLensFlare : ModProjectile, IAdditiveDrawer
    {
        public const int Lifetime = 76;

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/LargeStar";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Lens Flare");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 6;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            float scaleInterpolant = Pow(CalamityUtils.Convert01To010(Projectile.timeLeft / (float)Lifetime), 4.93f);
            Projectile.scale = scaleInterpolant * 1.67f;

            WorldSaveSystem.HasOpenedLostColosseumPortal = true;
            WorldSaveSystem.LostColosseumPortalAnimationTimer = 0;
        }

        public void AdditiveDraw(SpriteBatch spriteBatch)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            for (float scale = 1f; scale > 0.3f; scale -= 0.1f)
            {
                Color c = Color.Lerp(Projectile.GetAlpha(Color.White), Color.White, 1f - scale);
                spriteBatch.Draw(texture, drawPosition, null, c, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * scale, 0, 0f);
                spriteBatch.Draw(texture, drawPosition, null, c, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * new Vector2(4f, 0.2f) * scale, 0, 0f);
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.Orange;

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
