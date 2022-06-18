using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo
{
    public class ApolloRocketInfernum : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("High Explosive Plasma Rocket");
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
            cooldownSlot = 1;
            projectile.timeLeft = 600;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(projectile.localAI[0]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            projectile.localAI[0] = reader.ReadSingle();
        }

        public override void AI()
        {
            if (projectile.position.Y > projectile.ai[1])
                projectile.tileCollide = true;

            // Animation.
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 5 % Main.projFrames[projectile.type];

            // Rotation.
            projectile.rotation = (float)Math.Atan2(projectile.velocity.Y, projectile.velocity.X) + MathHelper.PiOver2;

            // Spawn effects.
            if (projectile.localAI[0] == 0f)
            {
                projectile.localAI[0] = 1f;

                float minDustSpeed = 1.8f;
                float maxDustSpeed = 2.8f;
                float angularVariance = 0.35f;

                for (int i = 0; i < 20; i++)
                {
                    float dustSpeed = Main.rand.NextFloat(minDustSpeed, maxDustSpeed);
                    Vector2 dustVel = new Vector2(dustSpeed, 0.0f).RotatedBy(projectile.velocity.ToRotation());
                    dustVel = dustVel.RotatedBy(-angularVariance);
                    dustVel = dustVel.RotatedByRandom(2.0f * angularVariance);
                    int randomDustType = Main.rand.NextBool(2) ? 107 : 110;

                    Dust plasma = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, randomDustType, dustVel.X, dustVel.Y, 200, default, 1.7f);
                    plasma.position = projectile.Center + Vector2.UnitY.RotatedByRandom(MathHelper.Pi) * (float)Main.rand.NextDouble() * projectile.width / 2f;
                    plasma.noGravity = true;
                    plasma.velocity *= 3f;

                    plasma = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, randomDustType, dustVel.X, dustVel.Y, 100, default, 0.8f);
                    plasma.position = projectile.Center + Vector2.UnitY.RotatedByRandom(MathHelper.Pi) * (float)Main.rand.NextDouble() * projectile.width / 2f;
                    plasma.velocity *= 2f;
                    plasma.fadeIn = 1f;
                    plasma.color = Color.Green * 0.5f;
                    plasma.noGravity = true;
                }
                for (int i = 0; i < 10; i++)
                {
                    float dustSpeed = Main.rand.NextFloat(minDustSpeed, maxDustSpeed);
                    Vector2 dustVel = new Vector2(dustSpeed, 0.0f).RotatedBy(projectile.velocity.ToRotation());
                    dustVel = dustVel.RotatedBy(-angularVariance);
                    dustVel = dustVel.RotatedByRandom(2.0f * angularVariance);
                    int randomDustType = Main.rand.NextBool(2) ? 107 : 110;

                    Dust plasma = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, randomDustType, dustVel.X, dustVel.Y, 0, default, 2f);
                    plasma.position = projectile.Center + Vector2.UnitX.RotatedByRandom(MathHelper.Pi).RotatedBy(projectile.velocity.ToRotation()) * projectile.width / 3f;
                    plasma.noGravity = true;
                    plasma.velocity *= 0.5f;
                }
            }

            // Emit light.
            Lighting.AddLight(projectile.Center, 0f, 0.6f, 0f);

            // Get a target and calculate distance from it
            int target = Player.FindClosest(projectile.Center, 1, 1);
            Vector2 distanceFromTarget = Main.player[target].Center - projectile.Center;

            // Set AI to stop homing, start accelerating
            float stopHomingDistance = 160f;
            if (distanceFromTarget.Length() < stopHomingDistance || projectile.ai[0] == 1f || projectile.timeLeft < 480)
            {
                projectile.ai[0] = 1f;

                if (projectile.velocity.Length() < 24f)
                    projectile.velocity *= 1.05f;

                return;
            }

            // Home in on target
            float oldSpeed = projectile.velocity.Length();
            float inertia = 8f;
            projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitY) * oldSpeed;
            projectile.velocity = (projectile.velocity * inertia + distanceFromTarget) / (inertia + 1f);
            projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitY) * oldSpeed;

            // Fly away from other rockets
            float pushForce = 0.07f;
            float pushDistance = 120f;
            for (int k = 0; k < Main.maxProjectiles; k++)
            {
                Projectile otherProj = Main.projectile[k];

                // Short circuits to make the loop as fast as possible.
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
            target.AddBuff(BuffID.CursedInferno, 180);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            lightColor = Color.Lerp(lightColor, Color.Lime, 0.75f);
            lightColor.A = 0;
            CalamityUtils.DrawAfterimagesCentered(projectile, ProjectileID.Sets.TrailingMode[projectile.type], lightColor, 1);
            return false;
        }

        public override void PostDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Rectangle frame = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            SpriteEffects direction = SpriteEffects.None;
            if (projectile.spriteDirection == -1)
                direction = SpriteEffects.FlipHorizontally;
            Main.spriteBatch.Draw(texture, projectile.Center - Main.screenPosition, frame, Color.White, projectile.rotation, origin, projectile.scale, direction, 0f);
        }

        public override void Kill(int timeLeft)
        {
            // Rocket explosion.
            int height = 90;
            projectile.position = projectile.Center;
            projectile.width = projectile.height = height;
            projectile.Center = projectile.position;
            projectile.Damage();

            Main.PlaySound(SoundID.Item14, projectile.Center);

            for (int i = 0; i < 12; i++)
            {
                int randomDustType = Main.rand.NextBool(2) ? 107 : 110;
                Dust plasma = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, randomDustType, 0f, 0f, 100, default, 2f);
                plasma.velocity *= 3f;
                if (Main.rand.NextBool(2))
                {
                    plasma.scale = 0.5f;
                    plasma.fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
            }
            for (int i = 0; i < 15; i++)
            {
                int randomDustType = Main.rand.NextBool(2) ? 107 : 110;
                Dust plasma = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, randomDustType, 0f, 0f, 100, default, 3f);
                plasma.noGravity = true;
                plasma.velocity *= 5f;

                plasma = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, randomDustType, 0f, 0f, 100, default, 2f);
                plasma.velocity *= 2f;
            }
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
