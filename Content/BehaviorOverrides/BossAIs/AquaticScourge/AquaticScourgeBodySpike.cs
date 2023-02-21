using CalamityMod;
using CalamityMod.DataStructures;
using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AquaticScourge
{
    public class AquaticScourgeBodySpike : ModProjectile, IAdditiveDrawer
    {
        public ref float AuraRadius => ref Projectile.ai[0];

        public ref float Time => ref Projectile.ai[1];

        public static float AcidWaterAccelerationFactor => 5f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Irradiated Spike");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 2;
            ProjectileID.Sets.CanDistortWater[Type] = false;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 200;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.timeLeft);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.timeLeft = reader.ReadInt32();
        }

        public override void AI()
        {
            // Make the bomb radius fade away if the projectile itself is fading away.
            if (Projectile.Infernum().FadeAwayTimer >= 1)
                AuraRadius *= 0.9f;

            // Make the sulphuric water effects go up far more quickly when inside the area of the pulse.
            AquaticScourgeHeadBehaviorOverride.ApplySulphuricPoisoningBoostToPlayersInArea(Projectile.Center, AuraRadius * 0.6f, AcidWaterAccelerationFactor);

            // Home at first.
            if (Time < 60f)
            {
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 12f, 0.1f);
                Projectile.velocity = Projectile.velocity.ClampMagnitude(4f, 12f);
            }

            // Accelerate after homing for long enough.
            else if (Projectile.velocity.Length() < 26f)
                Projectile.velocity *= 1.025f;

            // Prevent spike clumping behavior.
            float pushForce = 0.85f;
            for (int k = 0; k < Main.maxProjectiles; k++)
            {
                Projectile otherProj = Main.projectile[k];

                // Short circuits to make the loop as fast as possible
                if (!otherProj.active || otherProj.type != Projectile.type || k == Projectile.whoAmI)
                    continue;

                // If the other projectile is indeed the same owned by the same player and they're too close, nudge them away.
                bool sameProjType = otherProj.type == Projectile.type;
                float taxicabDistance = Math.Abs(Projectile.position.X - otherProj.position.X) + Math.Abs(Projectile.position.Y - otherProj.position.Y);
                if (sameProjType && taxicabDistance < Projectile.width)
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

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.Pi + MathHelper.PiOver4;

            Lighting.AddLight(Projectile.Center, Color.Yellow.ToVector3() * 0.5f);
            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Color.Lerp(lightColor, Color.White, 0.8f);
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type]);
            Projectile.DrawProjectileWithBackglowTemp(Color.White with { A = 0 } * 0.75f, lightColor, Projectile.Opacity * 5f);
            return false;
        }

        public void AdditiveDraw(SpriteBatch spriteBatch)
        {
            float telegraphInterpolant = Utils.GetLerpValue(0f, 35f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 45f, Time, true);
            float circleFadeinInterpolant = Utils.GetLerpValue(0f, 0.15f, telegraphInterpolant, true);
            float colorPulse = (float)Math.Cos(Main.GlobalTimeWrappedHourly * 6.1f + Projectile.identity) * 0.5f + 0.5f;
            float fadePulse = (Main.GlobalTimeWrappedHourly * 0.5f + Projectile.identity * 0.2721f) % 1f;
            if (telegraphInterpolant > 0f)
            {
                Texture2D explosionTelegraphTexture = InfernumTextureRegistry.DistortedBloomRing.Value;
                Vector2 scale = Vector2.One * AuraRadius / explosionTelegraphTexture.Size() * Projectile.Opacity * (fadePulse * 0.5f + 1.2f);
                float telegraphRotation = MathHelper.TwoPi * Projectile.identity / 13f % 1f + Main.GlobalTimeWrappedHourly * 1.427f;
                Color telegraphColor = Color.Lerp(Color.Lime, Color.Olive, colorPulse) * circleFadeinInterpolant * 0.3f;
                telegraphColor *= Utils.GetLerpValue(0f, 0.08f, fadePulse, true) * Utils.GetLerpValue(1f, 0.3f, fadePulse, true);

                spriteBatch.Draw(explosionTelegraphTexture, Projectile.Center - Main.screenPosition, null, telegraphColor, telegraphRotation, explosionTelegraphTexture.Size() * 0.5f, scale, 0, 0f);
                spriteBatch.Draw(explosionTelegraphTexture, Projectile.Center - Main.screenPosition, null, telegraphColor * 1.5f, -telegraphRotation, explosionTelegraphTexture.Size() * 0.5f, scale * 0.95f, 0, 0f);
            }
        }
    }
}
