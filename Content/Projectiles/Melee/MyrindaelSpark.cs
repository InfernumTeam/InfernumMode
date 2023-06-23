using CalamityMod;
using InfernumMode.Content.Items.Weapons.Melee;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Melee
{
    public class MyrindaelSpark : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Myrindael Spark");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 18;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 360;
            Projectile.Opacity = 0f;
            Projectile.friendly = true;
            Projectile.penetrate = 2;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
            Projectile.hide = true;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Projectile.penetrate < 2)
            {
                if (Projectile.timeLeft > 30)
                    Projectile.timeLeft = 30;
                Projectile.damage = 0;
                Projectile.velocity *= 0.93f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;
            Projectile.Opacity = Utils.GetLerpValue(360f, 345f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true);

            NPC potentialTarget = Projectile.Center.ClosestNPCAt(Myrindael.TargetHomeDistance);
            if (potentialTarget is not null && Projectile.timeLeft > 30)
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(potentialTarget.Center) * 29f, 0.06f);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255, 56) * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() * 0.5f;

            for (int i = 0; i < 7; i++)
            {
                Vector2 drawOffset = -Projectile.velocity.SafeNormalize(Vector2.Zero) * i * 3f;
                Vector2 afterimageDrawPosition = Projectile.Center + drawOffset - Main.screenPosition;
                Color backAfterimageColor = Projectile.GetAlpha(lightColor) * ((7f - i) / 7f);
                Main.spriteBatch.Draw(texture, afterimageDrawPosition, null, backAfterimageColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }

            Color frontAfterimageColor = new Color(0.23f, 0.93f, 0.96f, 0f) * Projectile.Opacity * 0.6f;
            for (int i = 0; i < 8; i++)
            {
                Vector2 drawOffset = (TwoPi * i / 8f + Projectile.rotation - PiOver2).ToRotationVector2() * 6f;
                Vector2 afterimageDrawPosition = Projectile.Center + drawOffset - Main.screenPosition;
                Main.spriteBatch.Draw(texture, afterimageDrawPosition, null, frontAfterimageColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }
            return false;
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI, List<int> overWiresUI)
        {
            drawCacheProjsBehindProjectiles.Add(index);
        }
    }
}
