using CalamityMod;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Thanatos
{
    public class ExolaserBomb : ModProjectile
    {
        public int GrowTime;
        public PrimitiveTrailCopy FireDrawer;
        public ref float Time => ref projectile.ai[0];
        public ref float Radius => ref projectile.ai[1];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Exolaser Bomb");

        public override void SetDefaults()
        {
            projectile.width = 164;
            projectile.height = 164;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.timeLeft = 9000;
            projectile.scale = 0.2f;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            Radius = projectile.scale * 100f;

            if (!NPC.AnyNPCs(ModContent.NPCType<ThanatosHead>()))
                projectile.active = false;

            if (projectile.timeLeft < 60f)
            {
                projectile.scale = MathHelper.Lerp(projectile.scale, 0.015f, 0.06f);
                Main.LocalPlayer.Infernum().CurrentScreenShakePower = Utils.InverseLerp(18f, 8f, projectile.timeLeft, true) * 15f;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 18f);
                    Utilities.NewProjectileBetter(projectile.Center + sparkVelocity * 3f, sparkVelocity, ModContent.ProjectileType<ExolaserSpark>(), 500, 0f);
                }
            }
            else
                projectile.scale = MathHelper.Lerp(0.04f, 7.5f, MathHelper.Clamp(Time / GrowTime, 0f, 1f));

            if (projectile.velocity != Vector2.Zero)
            {
                if (projectile.timeLeft > 110)
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(Terraria.ModLoader.SoundType.Item, "Sounds/Item/CrystylCharge"), projectile.Center);
                    projectile.timeLeft = 110;
                }

                if (projectile.velocity.Length() < 24f)
                    projectile.velocity *= 1.024f;
            }

            Time++;
        }

        public float SunWidthFunction(float completionRatio) => Radius * (float)Math.Sin(MathHelper.Pi * completionRatio);

        public Color SunColorFunction(float completionRatio) => Color.Lerp(Color.Red, Color.Red, (float)Math.Sin(MathHelper.Pi * completionRatio) * 0.4f + 0.3f) * projectile.Opacity;

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (FireDrawer is null)
                FireDrawer = new PrimitiveTrailCopy(SunWidthFunction, SunColorFunction, null, true, GameShaders.Misc["Infernum:Fire"]);

            GameShaders.Misc["Infernum:Fire"].UseSaturation(0.45f);
            GameShaders.Misc["Infernum:Fire"].SetShaderTexture(ModContent.GetTexture("InfernumMode/ExtraTextures/CultistRayMap"));

            List<float> rotationPoints = new List<float>();
            List<Vector2> drawPoints = new List<Vector2>();

            for (float offsetAngle = -MathHelper.PiOver2; offsetAngle <= MathHelper.PiOver2; offsetAngle += MathHelper.Pi / 24f)
            {
                rotationPoints.Clear();
                drawPoints.Clear();

                float adjustedAngle = offsetAngle + CalamityUtils.PerlinNoise2D(offsetAngle, Main.GlobalTime * 0.06f, 3, 185) * 3f;
                Vector2 offsetDirection = adjustedAngle.ToRotationVector2();
                for (int i = 0; i < 16; i++)
                {
                    rotationPoints.Add(adjustedAngle);
                    drawPoints.Add(Vector2.Lerp(projectile.Center - offsetDirection * Radius / 2f, projectile.Center + offsetDirection * Radius / 2f, i / 16f));
                }

                FireDrawer.Draw(drawPoints, -Main.screenPosition, 30);
            }

            float giantTwinkleSize = Utils.InverseLerp(55f, 8f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 8f, projectile.timeLeft, true);
            if (giantTwinkleSize > 0f)
            {
                float twinkleScale = giantTwinkleSize * 4.75f;
                Texture2D twinkleTexture = ModContent.GetTexture("InfernumMode/ExtraTextures/LargeStar");
                Vector2 drawPosition = projectile.Center - Main.screenPosition;
                float secondaryTwinkleRotation = Main.GlobalTime * 7.13f;

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
            Utilities.CreateGenericDustExplosion(projectile.Center, 235, 105, 30f, 2.25f);
            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(Terraria.ModLoader.SoundType.Item, "Sounds/Item/TeslaCannonFire"), projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 120; i++)
            {
                Vector2 sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(4f, 34f);
                Utilities.NewProjectileBetter(projectile.Center + sparkVelocity * 3f, sparkVelocity, ModContent.ProjectileType<ExolaserSpark>(), 500, 0f);
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {

        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Utilities.CircularCollision(projectile.Center, targetHitbox, Radius * 0.85f);
    }
}
