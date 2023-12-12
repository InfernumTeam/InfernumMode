using CalamityMod;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.EmpressOfLight.EmpressOfLightBehaviorOverride;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class EmpressSword : ModProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy TrailDrawer
        {
            get;
            set;
        }

        public int SwordCount
        {
            get;
            set;
        } = 4;

        public float TelegraphInterpolant
        {
            get;
            set;
        }

        public float Time
        {
            get;
            set;
        }

        public bool ShouldAttack
        {
            get;
            set;
        }

        public bool DontDealDamage
        {
            get;
            set;
        }

        public NPC Owner
        {
            get
            {
                if (!Main.npc.IndexInRange((int)Projectile.ai[0]) || Main.npc[(int)Projectile.ai[0]].type != NPCID.HallowBoss || !Main.npc[(int)Projectile.ai[0]].active)
                    return null;

                if (Main.npc[(int)Projectile.ai[0]].ai[0] != (int)EmpressOfLightAttackType.DanceOfSwords)
                    return null;

                return Main.npc[(int)Projectile.ai[0]];
            }
        }

        public Color MyColor
        {
            get
            {
                Color color = Main.hslToRgb(Projectile.ai[1] % 1f, 0.95f, 0.54f);
                if (ShouldBeEnraged)
                    color = GetDaytimeColor(Projectile.ai[1] % 1f);

                color.A /= 8;
                return color;
            }
        }

        public Vector2 HoverDestinationAboveOwner
        {
            get
            {
                Vector2 hoverDestination = Owner.Top - Vector2.UnitY.RotatedBy(Lerp(-0.74f, 0.74f, SwordIndex / (SwordCount - 1f))) * new Vector2(165f, 100f);
                hoverDestination.Y += Sin(TwoPi * Time / 60f + PiOver2 * SwordIndex / SwordCount) * 24f - 40f;
                return hoverDestination;
            }
        }

        public Player Target => Main.player[Owner.target];

        public ref float SwordIndex => ref Projectile.localAI[0];

        public const float TelegraphLength = 3600f;

        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.EmpressBlade}";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Empress Blade");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 24;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 86;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 7200;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Time);
            writer.Write(SwordCount);
            writer.Write(SwordIndex);
            writer.Write(Projectile.timeLeft);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Time = reader.ReadInt32();
            SwordCount = reader.ReadInt32();
            SwordIndex = reader.ReadSingle();
            Projectile.timeLeft = reader.ReadInt32();
        }

        public override void AI()
        {
            // Disappear if the owner is not present.
            if (Owner is null)
            {
                Projectile.Kill();
                return;
            }

            TelegraphInterpolant = Clamp(TelegraphInterpolant - 0.05f, 0f, 1f);
            DontDealDamage = false;

            if (!ShouldAttack)
            {
                HoverAboveEmpress();
                DontDealDamage = true;
                Time++;
            }
            else
                PerformAttackBehaviors();

            // Do not let the sword go out of bounds
            Projectile.Center = new(Clamp(Projectile.Center.X, 50f, Main.maxTilesX * 16f - 50f), Clamp(Projectile.Center.Y, 50f, Main.maxTilesY * 16f - 50f));

            // Constantly reset whether this blade should attack, in expectation that the empress will update this value herself.
            ShouldAttack = false;
        }

        public void HoverAboveEmpress()
        {
            Projectile.oldPos = new Vector2[Projectile.oldPos.Length];

            float idealRotation = -(Owner.Center - HoverDestinationAboveOwner).ToRotation();
            float hoverSpeed = Lerp(40f, 95f, Utils.GetLerpValue(100f, 750f, Projectile.Distance(HoverDestinationAboveOwner)));

            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Vector2.Zero.MoveTowards(HoverDestinationAboveOwner - Projectile.Center, hoverSpeed), 0.32f);
            Projectile.rotation = Projectile.rotation.AngleLerp(idealRotation, 0.03f);
        }

        public void PerformAttackBehaviors()
        {
            int hoverRedirectTime = 20;
            int chargeAnticipationTime = 33;
            int lanceShootDelay = 32;
            int perpendicularChargeAnticipationTime = 42;
            int perpendicularChargeTime = 42;
            float hoverOffsetAngle = Owner.Infernum().ExtraAI[1];
            float hoverOffset = 445f;
            float chargeSpeed = 64f;
            float lanceSpacing = 220f;

            if (InPhase3(Owner))
            {
                chargeSpeed += 6f;
                lanceSpacing -= 20f;
            }
            if (InPhase4(Owner))
            {
                hoverRedirectTime -= 5;
                chargeAnticipationTime -= 6;
                lanceShootDelay -= 6;
                perpendicularChargeAnticipationTime -= 8;
                perpendicularChargeTime -= 6;
            }

            if (ShouldBeEnraged)
            {
                hoverRedirectTime -= 9;
                chargeAnticipationTime -= 8;
                lanceShootDelay -= 8;
                perpendicularChargeAnticipationTime -= 8;
                perpendicularChargeTime -= 9;
                chargeSpeed += 23f;
                lanceSpacing -= 30f;
            }

            Vector2 hoverDestination = Target.Center + hoverOffsetAngle.ToRotationVector2() * hoverOffset;
            Vector2 hoverDestinationPerpendicular = Target.Center + (hoverOffsetAngle + PiOver2).ToRotationVector2() * hoverOffset;

            // Fly into position near the target.
            if (Time < hoverRedirectTime)
            {
                float hoverSpeedInterpolant = Lerp(0.03f, 0.25f, Time / hoverRedirectTime);
                Projectile.velocity *= 0.7f;
                Projectile.Center = Vector2.Lerp(Projectile.Center, hoverDestination, hoverSpeedInterpolant);
                Projectile.rotation = Projectile.rotation.AngleLerp(Projectile.AngleTo(Target.Center), hoverSpeedInterpolant * 2f);
                DontDealDamage = true;
                return;
            }

            // Move backwards in anticipation.
            if (Time < hoverRedirectTime + chargeAnticipationTime)
            {
                float anticipationInterpolant = Utils.GetLerpValue(hoverRedirectTime, hoverRedirectTime + chargeAnticipationTime, Time, true);
                Vector2 anticipationOffset = hoverOffsetAngle.ToRotationVector2() * Pow(anticipationInterpolant, 2f) * hoverOffset * 0.4f;

                Projectile.Center = hoverDestination + anticipationOffset;
                Projectile.velocity = Vector2.Zero;
                Projectile.rotation = Projectile.AngleTo(Target.Center);
                TelegraphInterpolant = Utils.GetLerpValue(0f, 0.7f, anticipationInterpolant, true) * Utils.GetLerpValue(1f, 0.85f, anticipationInterpolant, true);
                DontDealDamage = true;
                return;
            }

            // Charge at the target and release a wall of lances from behind.
            if (Time == hoverRedirectTime + chargeAnticipationTime)
            {
                Vector2 aimDirection = Projectile.SafeDirectionTo(Target.Center);

                CreateLancePatterns(lanceSpacing, lanceShootDelay, aimDirection);

                Projectile.oldPos = new Vector2[Projectile.oldPos.Length];
                Projectile.velocity = aimDirection * chargeSpeed;
                Projectile.netUpdate = true;

                SoundEngine.PlaySound(SoundID.Item72, Projectile.Center);
            }

            // Wait for the charge to end.
            if (Time < hoverRedirectTime + chargeAnticipationTime + lanceShootDelay)
                return;

            // Hover perpendicularly to the original direction.
            if (Time < hoverRedirectTime + chargeAnticipationTime + lanceShootDelay + perpendicularChargeAnticipationTime)
            {
                float hoverInterpolant = Utils.GetLerpValue(0f, perpendicularChargeAnticipationTime, Time - hoverRedirectTime - chargeAnticipationTime - lanceShootDelay, true);
                float hoverSpeedInterpolant = Lerp(0.03f, 0.25f, hoverInterpolant);
                Projectile.velocity *= 0.7f;
                Projectile.Center = Vector2.Lerp(Projectile.Center, hoverDestinationPerpendicular, hoverSpeedInterpolant);
                Projectile.rotation = Projectile.rotation.AngleLerp(Projectile.AngleTo(Target.Center), hoverSpeedInterpolant * 3f);
                DontDealDamage = true;
                return;
            }

            // Charge one last time.
            if (Time == hoverRedirectTime + chargeAnticipationTime + lanceShootDelay + perpendicularChargeAnticipationTime)
            {
                CreateLancePatterns(lanceSpacing, perpendicularChargeTime - 10, Projectile.SafeDirectionTo(Target.Center));

                Projectile.oldPos = new Vector2[Projectile.oldPos.Length];
                Projectile.velocity = Projectile.SafeDirectionTo(Target.Center) * chargeSpeed * 0.5f;
                Projectile.netUpdate = true;

                SoundEngine.PlaySound(SoundID.Item72, Projectile.Center);
            }
            if (Projectile.velocity.Length() < chargeSpeed)
            {
                Projectile.velocity *= 1.03f;
                Projectile.rotation = Projectile.velocity.ToRotation();
            }

            // Inform the empress that this blade is done being used once done charging.
            if (Main.netMode != NetmodeID.MultiplayerClient && Time >= hoverRedirectTime + chargeAnticipationTime + lanceShootDelay + perpendicularChargeAnticipationTime + perpendicularChargeTime)
            {
                Owner.ai[1] = 40f;
                Owner.Infernum().ExtraAI[3] = 1f;
                ShouldAttack = false;
                Time = 0f;
            }
        }

        public void CreateLancePatterns(float lanceSpacing, int lanceShootDelay, Vector2 aimDirection)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Make lances that are mostly horizontal in direction take a bit longer to fire.
            if (Math.Abs(Vector2.Dot(aimDirection, Vector2.UnitX)) > 0.74f)
                lanceShootDelay += 15;

            for (int i = 0; i < 10; i++)
            {
                float lanceHue = ((i + 5f) / 10f + Projectile.ai[1]) % 1f;
                float lanceOffsetDistance = (i - 5f) * lanceSpacing;
                Vector2 lanceOffset = aimDirection.RotatedBy(PiOver2).SafeNormalize(Vector2.UnitY) * lanceOffsetDistance - aimDirection * 876f;
                Vector2 lanceSpawnPosition = Target.Center + lanceOffset;

                ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(lance =>
                {
                    lance.MaxUpdates = 2;
                    lance.ModProjectile<EtherealLance>().Time = EtherealLance.FireDelay - lanceShootDelay * 2;
                    lance.ModProjectile<EtherealLance>().PlaySoundOnFiring = i == 5;
                });
                Utilities.NewProjectileBetter(lanceSpawnPosition, Vector2.Zero, ModContent.ProjectileType<EtherealLance>(), LanceDamage, 0f, -1, aimDirection.ToRotation(), lanceHue);
            }
        }

        public override bool? CanDamage() => Owner.Infernum().ExtraAI[0] == SwordIndex && !DontDealDamage ? null : false;

        public Color ColorFunction(float completionRatio)
        {
            if (Owner.Infernum().ExtraAI[0] != SwordIndex)
                return Color.Transparent;

            float speed = Vector2.Distance(Projectile.position, Projectile.oldPosition);
            float opacity = Utils.GetLerpValue(17.5f, 23f, speed, true) * Utils.GetLerpValue(20f, 30f, Projectile.timeLeft, true);
            Color rainbow = Main.hslToRgb((completionRatio - Main.GlobalTimeWrappedHourly * 0.7f) % 1f, 1f, 0.5f);
            Color c = Color.Lerp(MyColor, rainbow, completionRatio) * (1f - completionRatio) * opacity;
            c.A = 0;
            return c;
        }

        public static float WidthFunction(float completionRatio)
        {
            float fade = (1f - completionRatio) * Utils.GetLerpValue(-0.03f, 0.1f, completionRatio, true);
            return SmoothStep(0f, 1f, fade) * 20f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Texture2D telegraphTexture = InfernumTextureRegistry.BloomLine.Value;
            Vector2 origin = texture.Size() * 0.5f;

            // Draw a telegraph line if necessary.
            if (TelegraphInterpolant > 0f)
            {
                Main.spriteBatch.SetBlendState(BlendState.Additive);

                float telegraphHue = Cos(TwoPi * TelegraphInterpolant) * 0.5f + 0.5f;
                float telegraphWidth = Lerp(0.2f, 1.2f, TelegraphInterpolant);
                float telegraphOpacity = Pow(TelegraphInterpolant, 1.7f) * 0.7f;
                Vector2 telegraphScale = new(telegraphWidth, TelegraphLength / telegraphTexture.Height);
                Color telegraphColor = Main.hslToRgb(telegraphHue, 1f, 0.8f) * telegraphOpacity;
                Vector2 telegraphOrigin = telegraphTexture.Size() * new Vector2(0.5f, 0f);

                Main.spriteBatch.Draw(telegraphTexture, drawPosition, null, telegraphColor, Projectile.rotation - PiOver2, telegraphOrigin, telegraphScale, 0, 0f);
                Main.spriteBatch.Draw(telegraphTexture, drawPosition, null, Color.White * telegraphOpacity, Projectile.rotation - PiOver2, telegraphOrigin, telegraphScale * new Vector2(0.3f, 1f), 0, 0f);
                Main.spriteBatch.ResetBlendState();
            }

            float opacity = Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true);
            for (int i = 0; i < 6; i++)
            {
                Vector2 drawOffset = (TwoPi * i / 6f).ToRotationVector2() * 6f;
                Main.spriteBatch.Draw(texture, drawPosition + drawOffset, null, MyColor * opacity * 0.3f, Projectile.rotation, origin, Projectile.scale, 0, 0f);
            }
            Main.spriteBatch.Draw(texture, drawPosition, null, new Color(opacity, opacity, opacity, 0f), Projectile.rotation, origin, Projectile.scale, 0, 0f);

            return false;
        }

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            // Initialize the telegraph drawer.
            TrailDrawer ??= new(WidthFunction, ColorFunction, specialShader: InfernumEffectsRegistry.PrismaticRayVertexShader);

            // Prepare trail data.
            InfernumEffectsRegistry.PrismaticRayVertexShader.UseOpacity(0.2f);
            InfernumEffectsRegistry.PrismaticRayVertexShader.UseImage1("Images/Misc/Perlin");
            Main.instance.GraphicsDevice.Textures[2] = InfernumTextureRegistry.StreakSolid.Value;

            // Draw the afterimage trail.
            Main.spriteBatch.EnterShaderRegion();

            Vector2 afterimageOffset = Projectile.Size * 0.5f - Projectile.rotation.ToRotationVector2() * Projectile.width * 0.5f;
            TrailDrawer.DrawPixelated(Projectile.oldPos, afterimageOffset - Main.screenPosition, 67);
            Main.spriteBatch.ExitShaderRegion();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Projectile.RotatingHitboxCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.rotation.ToRotationVector2());
        }
    }
}
