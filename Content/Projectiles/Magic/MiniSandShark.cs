using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using CalamityMod;

namespace InfernumMode.Content.Projectiles.Magic
{
    public class MiniSandShark : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Mini Sand Shark");

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.minion = true;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.Opacity = 0f;
            Projectile.MaxUpdates = 2;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI()
        {
            // Fade in.
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.15f, 0f, 1f);

            // Play a sound on the first frame.
            if (Projectile.ai[1] == 0f)
            {
                Projectile.ai[1] = 1f;
                SoundEngine.PlaySound(SoundID.NPCHit13, Projectile.position);
            }

            Projectile.spriteDirection = (Projectile.velocity.X > 0f).ToDirectionInt();
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.spriteDirection == -1)
                Projectile.rotation += MathHelper.Pi;

            // Home in on targets.
            CalamityUtils.HomeInOnNPC(Projectile, true, 800f, 10f, 20f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteEffects spriteEffects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), null, Projectile.GetAlpha(lightColor), Projectile.rotation, texture.Size() / 2f, Projectile.scale, spriteEffects, 0);
            return false;
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            // Create two lingering sand skulls.
            if (Main.myPlayer != Projectile.owner)
                return;

            if (Main.rand.NextBool())
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Main.rand.NextVector2Circular(4f, 4f), ModContent.ProjectileType<LingeringSandSkull>(), Projectile.damage / 3, 0f, Projectile.owner);
        }

        public override void Kill(int timeLeft)
        {
            if (Projectile.penetrate >= 1)
                return;

            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 64;
            Projectile.position.X = Projectile.position.X - Projectile.width / 2;
            Projectile.position.Y = Projectile.position.Y - Projectile.height / 2;
            Projectile.maxPenetrate = -1;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.damage /= 2;
            Projectile.Damage();

            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);

            // Create a bunch of sand.
            for (int i = 0; i < 30; i++)
            {
                Dust sand = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(30f, 30f), 32);
                sand.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.61f) * Main.rand.NextFloat(3f, 8f);
                sand.noGravity = Main.rand.NextBool();
            }
        }
    }
}
