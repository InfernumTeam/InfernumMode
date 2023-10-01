using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.EyeOfCthulhu
{
    public class SittingBlood : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Tooth Ball");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 330;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            if (Projectile.velocity.Y < 14f)
                Projectile.velocity.Y += 0.25f;
            Projectile.alpha = Utils.Clamp(Projectile.alpha - 36, 0, 255);

            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;
            if (Projectile.timeLeft < 60)
            {
                Projectile.scale *= 0.992f;
                Projectile.ExpandHitboxBy((int)Math.Ceiling(24 * Projectile.scale));
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity.X *= 0.94f;
            return false;
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            fallThrough = false;
            return base.TileCollideStyle(ref width, ref height, ref fallThrough, ref hitboxCenterFrac);
        }

        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {

        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;

            for (int i = 0; i < 9; i++)
            {
                Vector2 drawOffset = (TwoPi * i / 9f).ToRotationVector2() * 6f;
                Main.spriteBatch.Draw(texture, drawPosition + drawOffset, null, Projectile.GetAlpha(Color.Red) * 0.65f, Projectile.rotation, origin, Projectile.scale, 0, 0f);
            }
            Main.spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, 0, 0f);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            Player closetstPlayer = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (Main.netMode == NetmodeID.MultiplayerClient || Distance(closetstPlayer.Center.X, Projectile.Center.X) < 240f)
                return;

            for (int i = 0; i < 2; i++)
                Utilities.NewProjectileBetter(Projectile.Center, -Vector2.UnitY.RotatedByRandom(0.92f) * Main.rand.NextFloat(21f, 31f), ModContent.ProjectileType<EoCTooth2>(), EyeOfCthulhuBehaviorOverride.ToothDamage, 0f);
        }
    }
}
