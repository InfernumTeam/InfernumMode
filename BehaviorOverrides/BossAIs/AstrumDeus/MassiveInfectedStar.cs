using CalamityMod;
using CalamityMod.Events;
using CalamityMod.Items.Tools;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.NPCs.AstrumDeus;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumDeus
{
    public class MassiveInfectedStar : ModProjectile
    {
        public int GrowTime;
        
        public PrimitiveTrailCopy FireDrawer;

        public ref float Time => ref Projectile.ai[0];

        public ref float Radius => ref Projectile.ai[1];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Consumed Star");

        public override void SetDefaults()
        {
            Projectile.width = 164;
            Projectile.height = 164;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 9000;
            Projectile.scale = 0.2f;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        public override void AI()
        {
            Radius = Projectile.scale * 100f;

            // Disappear if Deus is not present.
            if (!NPC.AnyNPCs(ModContent.NPCType<AstrumDeusHead>()))
                Projectile.active = false;

            // Fade out once ready.
            if (Projectile.timeLeft < 60f)
            {
                Projectile.scale = MathHelper.Lerp(Projectile.scale, 0.015f, 0.06f);
                Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = Utils.GetLerpValue(18f, 8f, Projectile.timeLeft, true) * 15f;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2.4f, 10f);
                    if (BossRushEvent.BossRushActive)
                        sparkVelocity *= 1.6f;

                    Utilities.NewProjectileBetter(Projectile.Center + sparkVelocity * 3f, sparkVelocity, ModContent.ProjectileType<AstralPlasmaSpark>(), 200, 0f);
                }
            }
            else
                Projectile.scale = MathHelper.Lerp(0.04f, 5.1f, MathHelper.Clamp(Time / GrowTime, 0f, 1f));

            if (Projectile.velocity != Vector2.Zero)
            {
                if (Projectile.timeLeft > 110)
                {
                    SoundEngine.PlaySound(CrystylCrusher.ChargeSound, Projectile.Center);
                    Projectile.timeLeft = 110;
                }

                if (Projectile.velocity.Length() < 23f)
                    Projectile.velocity *= 1.017f;
            }

            Time++;
        }

        public float SunWidthFunction(float completionRatio) => Radius * (float)Math.Sin(MathHelper.Pi * completionRatio);

        public Color SunColorFunction(float completionRatio) => Color.Lerp(Color.Red, Color.Orange, (float)Math.Sin(MathHelper.Pi * completionRatio) * 0.45f + 0.25f) * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            if (FireDrawer is null)
                FireDrawer = new PrimitiveTrailCopy(SunWidthFunction, SunColorFunction, null, true, InfernumEffectsRegistry.FireVertexShader);

            InfernumEffectsRegistry.FireVertexShader.UseSaturation(0.45f);
            InfernumEffectsRegistry.FireVertexShader.SetShaderTexture(InfernumTextureRegistry.CultistRayMap);

            List<float> rotationPoints = new();
            List<Vector2> drawPoints = new();

            for (float offsetAngle = -MathHelper.PiOver2; offsetAngle <= MathHelper.PiOver2; offsetAngle += MathHelper.Pi / 24f)
            {
                rotationPoints.Clear();
                drawPoints.Clear();

                float adjustedAngle = offsetAngle + CalamityUtils.PerlinNoise2D(offsetAngle, Main.GlobalTimeWrappedHourly * 0.06f, 3, 185) * 3f;
                Vector2 offsetDirection = adjustedAngle.ToRotationVector2();
                for (int i = 0; i < 16; i++)
                {
                    rotationPoints.Add(adjustedAngle);
                    drawPoints.Add(Vector2.Lerp(Projectile.Center - offsetDirection * Radius / 2f, Projectile.Center + offsetDirection * Radius / 2f, i / 16f));
                }

                FireDrawer.Draw(drawPoints, -Main.screenPosition, 30);
            }

            float giantTwinkleSize = Utils.GetLerpValue(55f, 8f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 8f, Projectile.timeLeft, true);
            if (giantTwinkleSize > 0f)
            {
                float twinkleScale = giantTwinkleSize * 4.75f;
                Texture2D twinkleTexture = InfernumTextureRegistry.LargeStar.Value;
                Vector2 drawPosition = Projectile.Center - Main.screenPosition;
                float secondaryTwinkleRotation = Main.GlobalTimeWrappedHourly * 7.13f;

                Main.spriteBatch.SetBlendState(BlendState.Additive);

                for (int i = 0; i < 2; i++)
                {
                    Main.spriteBatch.Draw(twinkleTexture, drawPosition, null, Color.White, 0f, twinkleTexture.Size() * 0.5f, twinkleScale * new Vector2(1f, 1.85f), SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(twinkleTexture, drawPosition, null, Color.White, secondaryTwinkleRotation, twinkleTexture.Size() * 0.5f, twinkleScale * new Vector2(1.3f, 1f), SpriteEffects.None, 0f);
                }
                Main.spriteBatch.ResetBlendState();
            }

            return false;
        }

        public override void Kill(int timeLeft)
        {
            Utilities.CreateGenericDustExplosion(Projectile.Center, 235, 105, 30f, 2.25f);
            SoundEngine.PlaySound(TeslaCannon.FireSound, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 45; i++)
            {
                Vector2 sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(4f, 14f);
                Utilities.NewProjectileBetter(Projectile.Center + sparkVelocity * 3f, sparkVelocity, ModContent.ProjectileType<AstralShot2>(), 200, 0f);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Utilities.CircularCollision(Projectile.Center, targetHitbox, Radius * 0.8f);
    }
}
