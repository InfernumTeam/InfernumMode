using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.PlaguebringerGoliath
{
    public class HostilePlagueSeeker : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override string Texture => "CalamityMod/Projectiles/StarProj";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Plague Seeker");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 12;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            if (Time >= 5f)
            {
                Dust plague = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.TerraBlade, 0f, 0f, 100, default, 0.75f);
                plague.noGravity = true;
                plague.velocity = Vector2.Zero;
            }

            // If not close to death, home in on the closest player.
            if (Time >= 56f)
            {
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                if (!Projectile.WithinRange(target.Center, 50f))
                    Projectile.velocity = (Projectile.velocity * 69f + Projectile.SafeDirectionTo(target.Center) * 16f) / 70f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;
            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D boltTexture = ModContent.Request<Texture2D>(Texture).Value;
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float completionRatio = i / (float)Projectile.oldPos.Length;
                Color drawColor = Color.Lerp(lightColor, Color.Olive, 0.6f);
                drawColor = Color.Lerp(drawColor, Color.Lime, 0.425f);
                drawColor = Color.Lerp(drawColor, Color.Black, completionRatio);
                drawColor = Color.Lerp(drawColor, Color.Transparent, completionRatio);

                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(boltTexture, drawPosition, null, Projectile.GetAlpha(drawColor), Projectile.oldRot[i], boltTexture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
            }
            return false;
        }

        public override bool? CanDamage() => Projectile.Opacity >= 0.8f;
    }
}
