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
        public ref float Timer => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Vortex of Flame");
        }
        public override void SetDefaults()
        {
            projectile.width = 408;
            projectile.height = 408;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.alpha = 255;
            projectile.timeLeft = Lifetime;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            projectile.rotation += MathHelper.ToRadians(14f);
            projectile.Opacity = Utils.InverseLerp(0f, 40f, Timer, true) * Utils.InverseLerp(0f, 40f, projectile.timeLeft, true);
            if (projectile.owner == Main.myPlayer)
            {
                Player player = Main.player[Player.FindClosest(projectile.Center, 1, 1)];

                int shootRate = projectile.timeLeft < 250 ? 80 : 125;
                if (Timer > 150f && Timer % shootRate == shootRate - 1f && projectile.timeLeft > 60f)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        float offsetAngle = MathHelper.TwoPi * i / 4f;
                        Utilities.NewProjectileBetter(projectile.Center, projectile.SafeDirectionTo(player.Center).RotatedBy(offsetAngle) * 7f, ProjectileID.CultistBossFireBall, 560, 0f, Main.myPlayer);
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
                    Dust dust = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(200f, 200f), DustID.Fire);
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

        public override bool CanDamage() => Timer > 60f && projectile.timeLeft > 60f;

        public override bool PreDrawExtras(SpriteBatch spriteBatch)
        {
            spriteBatch.SetBlendState(BlendState.Additive);

            Texture2D texture = ModContent.GetTexture(Texture);
            for (int j = 0; j < 16f; j++)
            {
                float angle = MathHelper.TwoPi / j * 16f;
                Vector2 offset = angle.ToRotationVector2() * 32f;
                Color drawColor = Color.White * projectile.Opacity * 0.08f;
                drawColor.A = 127;
                spriteBatch.Draw(texture, projectile.Center + offset - Main.screenPosition, null, drawColor, projectile.rotation, texture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);
            }

            spriteBatch.ResetBlendState();
            return false;
        }
    }
}