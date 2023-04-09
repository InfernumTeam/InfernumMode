using CalamityMod;
using CalamityMod.Items.Tools;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Thanatos
{
    public class ExolaserBomb : ModProjectile
    {
        public int GrowTime;

        public PrimitiveTrailCopy FireDrawer;

        public ref float Time => ref Projectile.ai[0];

        public ref float Radius => ref Projectile.ai[1];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Exolaser Bomb");

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
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(GrowTime);

        public override void ReceiveExtraAI(BinaryReader reader) => GrowTime = reader.ReadInt32();

        public override void AI()
        {
            Radius = Projectile.scale * 100f;

            if (!NPC.AnyNPCs(ModContent.NPCType<ThanatosHead>()))
                Projectile.active = false;

            NPC thanatos = Main.npc[CalamityGlobalNPC.draedonExoMechWorm];
            if (thanatos.ai[0] != (int)ThanatosHeadBehaviorOverride.ThanatosHeadAttackType.ExoBomb)
                Projectile.active = false;

            if (Projectile.timeLeft < 60f)
            {
                Projectile.scale = MathHelper.Lerp(Projectile.scale, 0.015f, 0.06f);
                Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = Utils.GetLerpValue(18f, 8f, Projectile.timeLeft, true) * 15f;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 18f);
                    Utilities.NewProjectileBetter(Projectile.Center + sparkVelocity * 3f, sparkVelocity, ModContent.ProjectileType<ExolaserSpark>(), DraedonBehaviorOverride.NormalShotDamage, 0f);
                }
            }
            else
                Projectile.scale = MathHelper.Lerp(0.04f, 7.5f, MathHelper.Clamp(Time / GrowTime, 0f, 1f));

            if (Projectile.velocity != Vector2.Zero)
            {
                if (Projectile.timeLeft > 110)
                {
                    SoundEngine.PlaySound(CrystylCrusher.ChargeSound, Projectile.Center);
                    Projectile.timeLeft = 110;
                }

                if (Projectile.velocity.Length() < 24f)
                    Projectile.velocity *= 1.024f;
            }

            Time++;
        }

        public float SunWidthFunction(float completionRatio) => Radius * (float)Math.Sin(MathHelper.Pi * completionRatio);

        public Color SunColorFunction(float completionRatio) => Color.Lerp(Color.Red, Color.Red, (float)Math.Sin(MathHelper.Pi * completionRatio) * 0.4f + 0.3f) * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            FireDrawer ??= new PrimitiveTrailCopy(SunWidthFunction, SunColorFunction, null, true, InfernumEffectsRegistry.FireVertexShader);

            InfernumEffectsRegistry.FireVertexShader.UseSaturation(0.45f);
            InfernumEffectsRegistry.FireVertexShader.SetShaderTexture(InfernumTextureRegistry.CultistRayMap);

            List<Vector2> drawPoints = new();

            for (float offsetAngle = -MathHelper.PiOver2; offsetAngle <= MathHelper.PiOver2; offsetAngle += MathHelper.Pi / 24f)
            {
                drawPoints.Clear();

                float adjustedAngle = offsetAngle + CalamityUtils.PerlinNoise2D(offsetAngle, Main.GlobalTimeWrappedHourly * 0.06f, 3, 185) * 3f;
                Vector2 offsetDirection = adjustedAngle.ToRotationVector2();
                for (int i = 0; i < 16; i++)
                    drawPoints.Add(Vector2.Lerp(Projectile.Center - offsetDirection * Radius / 2f, Projectile.Center + offsetDirection * Radius / 2f, i / 16f));

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

            for (int i = 0; i < 120; i++)
            {
                Vector2 sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(4f, 34f);
                Utilities.NewProjectileBetter(Projectile.Center + sparkVelocity * 3f, sparkVelocity, ModContent.ProjectileType<ExolaserSpark>(), DraedonBehaviorOverride.NormalShotDamage, 0f);
            }
        }

        public override bool? CanDamage() => Projectile.velocity != Vector2.Zero ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Utilities.CircularCollision(Projectile.Center, targetHitbox, Radius * 0.85f);
    }
}
