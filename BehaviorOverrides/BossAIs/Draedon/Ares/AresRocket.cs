using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class AresRocket : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Exoplasmic Heatseeking Missile");
            Main.projFrames[Projectile.type] = 5;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
            Projectile.Calamity().canBreakPlayerDefense = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            // Handle frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // Set AI to stop homing, start accelerating if the rocket has gotten close enough to the player.
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (Projectile.WithinRange(target.Center, 480f) || Projectile.ai[0] == 1f || Projectile.timeLeft < 525)
            {
                Projectile.ai[0] = 1f;

                if (Projectile.velocity.Length() < 32f)
                    Projectile.velocity *= 1.05f;
                return;
            }

            // Home in on target.
            float oldSpeed = Projectile.velocity.Length();
            float inertia = 6f;
            Projectile.velocity = (Projectile.velocity * inertia + Projectile.SafeDirectionTo(target.Center) * oldSpeed) / (inertia + 1f);
            Projectile.velocity.Normalize();
            Projectile.velocity *= oldSpeed;

            // Fly away from other rockets
            float pushForce = 0.08f;
            float pushDistance = 120f;
            for (int k = 0; k < Main.maxProjectiles; k++)
            {
                Projectile otherProj = Main.projectile[k];

                if (!otherProj.active || k == Projectile.whoAmI)
                    continue;

                // If the other projectile is indeed the same owned by the same player and they're too close, nudge them away.
                bool sameProjType = otherProj.type == Projectile.type;
                float taxicabDist = Vector2.Distance(Projectile.Center, otherProj.Center);
                if (sameProjType && taxicabDist < pushDistance)
                {
                    if (Projectile.position.X < otherProj.position.X)
                        Projectile.velocity.X -= pushForce;
                    else
                        Projectile.velocity.X += pushForce;

                    if (Projectile.position.Y < otherProj.position.Y)
                        Projectile.velocity.Y -= pushForce;
                    else
                        Projectile.velocity.Y += pushForce;
                }
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(BuffID.OnFire, 360);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color rocketColor = new(185, 185, 185, 0);
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], rocketColor, 1);
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
