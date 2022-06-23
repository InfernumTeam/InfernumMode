using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Yharon
{
    public class VortexOfFlame : ModProjectile
    {
        public const int Lifetime = 600;
        public const int AuraCount = 4;
        public ref float Timer => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Vortex of Flame");
        }
        public override void SetDefaults()
        {
            Projectile.width = 408;
            Projectile.height = 408;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.timeLeft = Lifetime;
            Projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            Projectile.rotation += MathHelper.ToRadians(14f);
            Projectile.Opacity = Utils.GetLerpValue(0f, 40f, Timer, true) * Utils.GetLerpValue(0f, 40f, Projectile.timeLeft, true);
            if (Projectile.owner == Main.myPlayer)
            {
                Player player = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];

                int shootRate = Projectile.timeLeft < 250 ? 80 : 125;
                if (Timer > 150f && Timer % shootRate == shootRate - 1f && Projectile.timeLeft > 60f)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        float offsetAngle = MathHelper.TwoPi * i / 4f;
                        Utilities.NewProjectileBetter(Projectile.Center, Projectile.SafeDirectionTo(player.Center).RotatedBy(offsetAngle) * 7f, ProjectileID.CultistBossFireBall, 560, 0f, Main.myPlayer);
                    }
                }
            }

            Timer++;
        }

        public override void Kill(int timeLeft)
        {
            if (!Main.dedServ)
            {
                for (int i = 0; i < 200; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(200f, 200f), 6);
                    dust.velocity = Main.rand.NextVector2Circular(15f, 15f);
                    dust.fadeIn = 1.4f;
                    dust.scale = 1.6f;
                    dust.noGravity = true;
                }
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<LethalLavaBurn>(), 600);
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Timer > 60f && Projectile.timeLeft > 60f;

        public override bool PreDrawExtras()
        {
            spriteBatch.SetBlendState(BlendState.Additive);

            Texture2D texture = ModContent.GetTexture(Texture);
            for (int j = 0; j < 16f; j++)
            {
                float angle = MathHelper.TwoPi / j * 16f;
                Vector2 offset = angle.ToRotationVector2() * 32f;
                Color drawColor = Color.White * Projectile.Opacity * 0.08f;
                drawColor.A = 127;
                spriteBatch.Draw(texture, Projectile.Center + offset - Main.screenPosition, null, drawColor, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            }

            spriteBatch.ResetBlendState();
            return false;
        }
    }
}