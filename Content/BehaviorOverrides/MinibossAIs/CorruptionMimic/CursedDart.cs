using InfernumMode.Content.BehaviorOverrides.BossAIs.EoW;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.MinibossAIs.CorruptionMimic
{
    public class CursedDart : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Cursed Flame Dart");
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 18;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 300;
            Projectile.penetrate = -1;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Time++;

            Projectile.tileCollide = Time >= 105f;
            Projectile.velocity.Y = Clamp(Projectile.velocity.Y + 0.36f, -32f, 9f);
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;
            Projectile.Opacity = Utils.GetLerpValue(0f, 12f, Time, true);

            // Fire flames downward.
            if (Main.netMode != NetmodeID.MultiplayerClient && Time >= 12f && Time % 18f == 17f)
                Utilities.NewProjectileBetter(Projectile.Center, Vector2.UnitY * 5.6f, ModContent.ProjectileType<CursedBullet>(), 115, 0f);

            float dustCreationChance = Utils.GetLerpValue(0f, 12f, Time, true);
            for (int i = 0; i < 2; i++)
            {
                if (Main.rand.NextFloat() > dustCreationChance)
                    continue;

                Dust fire = Dust.NewDustDirect(Projectile.TopLeft, Projectile.width, Projectile.height, 75);
                fire.velocity = Main.rand.NextVector2Circular(1.2f, 1.2f);
                fire.noGravity = true;
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool? CanDamage() => Time >= 12f;

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 3);
            return false;
        }
    }
}
