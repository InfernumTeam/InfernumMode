using CalamityMod;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo
{
    public class ApolloRocketInfernum : ModProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy FlameTrailDrawer = null;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("High Explosive Plasma Rocket");
            Main.projFrames[Projectile.type] = 5;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            CooldownSlot = ImmunityCooldownID.Bosses;
            Projectile.timeLeft = 600;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
        }

        public override void AI()
        {
            if (Projectile.position.Y > Projectile.ai[1])
                Projectile.tileCollide = true;

            // Animation.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];

            // Rotation.
            Projectile.rotation = (float)Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X) + MathHelper.PiOver2;

            // Spawn effects.
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;

                float minDustSpeed = 1.8f;
                float maxDustSpeed = 2.8f;
                float angularVariance = 0.35f;

                for (int i = 0; i < 20; i++)
                {
                    float dustSpeed = Main.rand.NextFloat(minDustSpeed, maxDustSpeed);
                    Vector2 dustVel = new Vector2(dustSpeed, 0.0f).RotatedBy(Projectile.velocity.ToRotation());
                    dustVel = dustVel.RotatedBy(-angularVariance);
                    dustVel = dustVel.RotatedByRandom(2.0f * angularVariance);
                    int randomDustType = Main.rand.NextBool(2) ? 107 : 110;

                    Dust plasma = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, randomDustType, dustVel.X, dustVel.Y, 200, default, 1.7f);
                    plasma.position = Projectile.Center + Vector2.UnitY.RotatedByRandom(MathHelper.Pi) * (float)Main.rand.NextDouble() * Projectile.width / 2f;
                    plasma.noGravity = true;
                    plasma.velocity *= 3f;

                    plasma = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, randomDustType, dustVel.X, dustVel.Y, 100, default, 0.8f);
                    plasma.position = Projectile.Center + Vector2.UnitY.RotatedByRandom(MathHelper.Pi) * (float)Main.rand.NextDouble() * Projectile.width / 2f;
                    plasma.velocity *= 2f;
                    plasma.fadeIn = 1f;
                    plasma.color = Color.Green * 0.5f;
                    plasma.noGravity = true;
                }
                for (int i = 0; i < 10; i++)
                {
                    float dustSpeed = Main.rand.NextFloat(minDustSpeed, maxDustSpeed);
                    Vector2 dustVel = new Vector2(dustSpeed, 0.0f).RotatedBy(Projectile.velocity.ToRotation());
                    dustVel = dustVel.RotatedBy(-angularVariance);
                    dustVel = dustVel.RotatedByRandom(2.0f * angularVariance);
                    int randomDustType = Main.rand.NextBool(2) ? 107 : 110;

                    Dust plasma = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, randomDustType, dustVel.X, dustVel.Y, 0, default, 2f);
                    plasma.position = Projectile.Center + Vector2.UnitX.RotatedByRandom(MathHelper.Pi).RotatedBy(Projectile.velocity.ToRotation()) * Projectile.width / 3f;
                    plasma.noGravity = true;
                    plasma.velocity *= 0.5f;
                }
            }

            // Emit light.
            Lighting.AddLight(Projectile.Center, 0f, 0.6f, 0f);

            // Get a target and calculate distance from it
            int target = Player.FindClosest(Projectile.Center, 1, 1);
            Vector2 distanceFromTarget = Main.player[target].Center - Projectile.Center;

            // Set AI to stop homing, start accelerating
            float stopHomingDistance = 160f;
            if (distanceFromTarget.Length() < stopHomingDistance || Projectile.ai[0] == 1f || Projectile.timeLeft < 480)
            {
                Projectile.ai[0] = 1f;

                if (Projectile.velocity.Length() < 24f)
                    Projectile.velocity *= 1.05f;

                return;
            }

            // Home in on target
            float oldSpeed = Projectile.velocity.Length();
            float inertia = 8f;
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * oldSpeed;
            Projectile.velocity = (Projectile.velocity * inertia + distanceFromTarget) / (inertia + 1f);
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * oldSpeed;

            // Fly away from other rockets
            float pushForce = 0.07f;
            float pushDistance = 120f;
            for (int k = 0; k < Main.maxProjectiles; k++)
            {
                Projectile otherProj = Main.projectile[k];

                // Short circuits to make the loop as fast as possible.
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

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            SpriteEffects direction = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
                direction = SpriteEffects.FlipHorizontally;
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, frame, Color.White, Projectile.rotation, origin, Projectile.scale, direction, 0f);
            return false;
        }

        public static float FlameTrailWidthFunction(float completionRatio) => MathHelper.SmoothStep(27f, 8f, completionRatio);

        public static Color FlameTrailColorFunction(float completionRatio)
        {
            float trailOpacity = Utils.GetLerpValue(0.8f, 0.27f, completionRatio, true) * Utils.GetLerpValue(0f, 0.067f, completionRatio, true);
            Color startingColor = Color.Lerp(Color.LawnGreen, Color.White, 0.4f);
            Color middleColor = Color.Lerp(Color.DarkGreen, Color.Red, 0.2f);
            Color endColor = Color.Lerp(Color.DarkGreen, Color.Red, 0.67f);
            return CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * trailOpacity;
        }

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            // Initialize the flame trail drawer.
            FlameTrailDrawer ??= new(FlameTrailWidthFunction, FlameTrailColorFunction, null, true, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);
            Vector2 trailOffset = Projectile.Size * 0.5f - Projectile.velocity;
            FlameTrailDrawer.DrawPixelated(Projectile.oldPos, trailOffset - Main.screenPosition, 61);
        }

        public override void Kill(int timeLeft)
        {
            // Create a rocket explosion.
            int height = 90;
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = height;
            Projectile.Center = Projectile.position;
            Projectile.Damage();

            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

            for (int i = 0; i < 12; i++)
            {
                int randomDustType = Main.rand.NextBool(2) ? 107 : 110;
                Dust plasma = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, randomDustType, 0f, 0f, 100, default, 2f);
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
                Dust plasma = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, randomDustType, 0f, 0f, 100, default, 3f);
                plasma.noGravity = true;
                plasma.velocity *= 5f;

                plasma = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, randomDustType, 0f, 0f, 100, default, 2f);
                plasma.velocity *= 2f;
            }
        }
    }
}
