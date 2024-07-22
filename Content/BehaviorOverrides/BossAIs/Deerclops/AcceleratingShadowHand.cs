using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Deerclops
{
    public class AcceleratingShadowHand : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.InsanityShadowHostile}";

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Shadow Hand");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            Projectile.Calamity().DealsDefenseDamage = true;
            
        }

        public override void AI()
        {
            if (!NPC.AnyNPCs(NPCID.Deerclops))
                Projectile.Kill();

            // Fade in.
            Projectile.Opacity = Utils.GetLerpValue(0f, 18f, Time, true) * Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);
            if (Projectile.velocity.Length() < 18f)
                Projectile.velocity *= 1.018f;
            Projectile.rotation = Projectile.velocity.ToRotation();

            Time++;
        }

        public override bool? CanDamage() => Projectile.Opacity > 0.9f ? null : false;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = tex.Size() * 0.5f;

            float rotation = Projectile.rotation;
            Color backglowColor = Color.Lerp(Color.Red, Color.White, 0.5f) * Projectile.Opacity * 0.5f;
            for (int j = 0; j < 4; j++)
            {
                Vector2 offsetDirection = rotation.ToRotationVector2();
                double spin = Main.GlobalTimeWrappedHourly * TwoPi / 24f + TwoPi * j / 4f;
                Main.EntitySpriteDraw(tex, drawPosition + offsetDirection.RotatedBy(spin) * 6f, null, backglowColor, rotation, origin, Projectile.scale, 0, 0);
            }
            Main.spriteBatch.Draw(tex, drawPosition, null, Projectile.GetAlpha(Color.Black), rotation, origin, Projectile.scale, 0, 0f);
            return false;
        }
    }
}
