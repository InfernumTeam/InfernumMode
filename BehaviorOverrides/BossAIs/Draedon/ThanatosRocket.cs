using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
    public class ThanatosRocket : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Exothermal Heatseeking Missile");
            Main.projFrames[projectile.type] = 5;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = 28;
            projectile.height = 28;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 600;
            projectile.Calamity().canBreakPlayerDefense = true;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            // Handle frames.
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 5 % Main.projFrames[projectile.type];

            // Release spawn dust on the first frame.
            if (projectile.localAI[0] == 0f)
            {
                projectile.localAI[0] = 1f;
            }

            // Set AI to stop homing, start accelerating if the rocket has gotten close enough to the player.
            Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
            if (projectile.WithinRange(target.Center, 480f) || projectile.ai[0] == 1f || projectile.timeLeft < 360)
            {
                projectile.ai[0] = 1f;

                if (projectile.velocity.Length() < 32f)
                    projectile.velocity *= 1.05f;
                return;
            }

            // Home in on target.
            float oldSpeed = projectile.velocity.Length();
            float inertia = 6f;
            projectile.velocity = (projectile.velocity * inertia + projectile.SafeDirectionTo(target.Center) * oldSpeed) / (inertia + 1f);
            projectile.velocity.Normalize();
            projectile.velocity *= oldSpeed;
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // Fly away from other rockets
            float pushForce = 0.08f;
            float pushDistance = 120f;
            for (int k = 0; k < Main.maxProjectiles; k++)
            {
                Projectile otherProj = Main.projectile[k];

                if (!otherProj.active || k == projectile.whoAmI)
                    continue;

                // If the other projectile is indeed the same owned by the same player and they're too close, nudge them away.
                bool sameProjType = otherProj.type == projectile.type;
                float taxicabDist = Vector2.Distance(projectile.Center, otherProj.Center);
                if (sameProjType && taxicabDist < pushDistance)
                {
                    if (projectile.position.X < otherProj.position.X)
                        projectile.velocity.X -= pushForce;
                    else
                        projectile.velocity.X += pushForce;

                    if (projectile.position.Y < otherProj.position.Y)
                        projectile.velocity.Y -= pushForce;
                    else
                        projectile.velocity.Y += pushForce;
                }
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(BuffID.OnFire, 360);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(projectile, ProjectileID.Sets.TrailingMode[projectile.type], lightColor, 1);
            return false;
        }

        public override void PostDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Texture2D glowmaskTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Draedon/ThanatosRocketGlow");
            int height = texture.Height / Main.projFrames[projectile.type];
            int drawStart = height * projectile.frame;
            Vector2 origin = projectile.Size * 0.5f;
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (projectile.spriteDirection == -1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            spriteBatch.Draw(glowmaskTexture, projectile.Center - Main.screenPosition, new Rectangle(0, drawStart, texture.Width, height), Color.White, projectile.rotation, origin, projectile.scale, spriteEffects, 0f);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
