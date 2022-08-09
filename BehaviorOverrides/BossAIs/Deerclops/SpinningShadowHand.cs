using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Deerclops
{
    public class SpinningShadowHand : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public ref float SpinTime => ref Projectile.ai[1];

        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.InsanityShadowHostile}";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Shadow Hand");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        public override void AI()
        {
            if (!NPC.AnyNPCs(NPCID.Deerclops))
                Projectile.Kill();

            if (SpinTime == 0f)
                SpinTime = 20f;

            // Fade in.
            Projectile.Opacity = Utils.GetLerpValue(0f, 16f, Time, true);

            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (Time <= SpinTime)
            {
                // Slow down and spin in place.
                float rotationOffset = MathHelper.TwoPi * ((Time / SpinTime + Projectile.identity / 8f) % 1f);
                rotationOffset = MathHelper.Lerp(rotationOffset, MathHelper.TwoPi, Utils.GetLerpValue(SpinTime - 16f, SpinTime, Time, true));
                Projectile.rotation = Projectile.AngleTo(target.Center) + rotationOffset;
                Projectile.velocity *= 0.98f;

                // Charge at the target.
                if (Time == SpinTime)
                {
                    SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown, Projectile.Center);
                    Projectile.velocity = Projectile.SafeDirectionTo(target.Center) * 8f;
                    Projectile.rotation = Projectile.velocity.ToRotation();
                    Projectile.netUpdate = true;
                }
            }
            else if (Projectile.velocity.Length() < 15.5f)
                Projectile.velocity *= 1.0175f;

            Time++;
        }

        public override bool? CanDamage() => Time >= 24f ? null : false;

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
                double spin = Main.GlobalTimeWrappedHourly * MathHelper.TwoPi / 24f + MathHelper.TwoPi * j / 4f;
                Main.EntitySpriteDraw(tex, drawPosition + offsetDirection.RotatedBy(spin) * 6f, null, backglowColor, rotation, origin, Projectile.scale, 0, 0);
            }
            Main.spriteBatch.Draw(tex, drawPosition, null, Projectile.GetAlpha(Color.Black), rotation, origin, Projectile.scale, 0, 0f);
            return false;
        }
    }
}
