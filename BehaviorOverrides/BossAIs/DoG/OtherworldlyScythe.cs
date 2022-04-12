using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public class OtherworldlyScythe : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Scythe");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 150;
            Projectile.alpha = 100;
            Projectile.penetrate = -1;
            Projectile.Calamity().canBreakPlayerDefense = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.rotation += 0.6f * Projectile.direction;
            if (Projectile.localAI[0] == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item73, Projectile.position);
                Projectile.localAI[0] = 1f;
            }
            Player closest = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];

            if (!Projectile.WithinRange(closest.Center, 200f))
                Projectile.velocity = (Projectile.velocity * 40f + Projectile.SafeDirectionTo(closest.Center) * 19f) / 41f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1);
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = Projectile;

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(InfernumMode.CalamityMod.Find<ModBuff>("GodSlayerInferno").Type, 240);
        }
    }
}
