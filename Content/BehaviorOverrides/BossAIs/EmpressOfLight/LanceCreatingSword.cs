using CalamityMod.Events;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class LanceCreatingSword : ModProjectile, IPixelPrimitiveDrawer
    {
        public int SwordCount = 2;

        public int AttackDelay = 36;

        public float TelegraphInterpolant;

        public PrimitiveTrailCopy TrailDrawer;

        public NPC Owner
        {
            get
            {
                if (!Main.npc.IndexInRange((int)Projectile.ai[0]) || Main.npc[(int)Projectile.ai[0]].type != NPCID.HallowBoss || !Main.npc[(int)Projectile.ai[0]].active)
                    return null;

                if (Main.npc[(int)Projectile.ai[0]].ai[0] != (int)EmpressOfLightBehaviorOverride.EmpressOfLightAttackType.MajesticPierce)
                    return null;

                return Main.npc[(int)Projectile.ai[0]];
            }
        }

        public float HueOffset => (Projectile.ai[1] + Main.GlobalTimeWrappedHourly * 0.84f) % 1f;

        public Color MyColor
        {
            get
            {
                float hue = HueOffset;
                Color color = Main.hslToRgb(hue, 0.95f, 0.54f);
                if (EmpressOfLightBehaviorOverride.ShouldBeEnraged)
                    color = EmpressOfLightBehaviorOverride.GetDaytimeColor(hue);

                color.A /= 8;
                return color;
            }
        }

        public Vector2 HoverDestinationAboveOwner
        {
            get
            {
                Vector2 hoverDestination = Owner.Top - Vector2.UnitY.RotatedBy(MathHelper.Lerp(-0.94f, 0.94f, SwordIndex / (SwordCount - 1f))) * new Vector2(500f, 100f);
                hoverDestination.Y += (float)Math.Sin(MathHelper.TwoPi * Timer / 60f + MathHelper.PiOver2 * SwordIndex / SwordCount) * 24f - 40f;
                return hoverDestination;
            }
        }

        public Player Target => Main.player[Owner.target];

        public float Timer => Owner is null ? -AttackDelay : Owner.ai[1] - AttackDelay;

        public ref float SwordIndex => ref Projectile.localAI[1];

        public const float TelegraphLength = 3600f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Empress Blade");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 24;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 94;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 900;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(AttackDelay);
            writer.Write(SwordCount);
            writer.Write(SwordIndex);
            writer.Write(TelegraphInterpolant);
            writer.Write(Projectile.timeLeft);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            AttackDelay = reader.ReadInt32();
            SwordCount = reader.ReadInt32();
            SwordIndex = reader.ReadSingle();
            TelegraphInterpolant = reader.ReadSingle();
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

            // Reset things.
            TelegraphInterpolant = 0f;
            AttackTarget();
        }

        public void AttackTarget()
        {
            int lungeDelay = 35;
            float lungeSpeed = 60f;

            if (BossRushEvent.BossRushActive)
                lungeSpeed += 10f;

            // Aim at the target in anticipation of a lunge.
            if (Timer < lungeDelay)
            {
                float idealRotation = (Target.Center.Y > Projectile.Center.Y).ToDirectionInt() * MathHelper.PiOver2;
                Projectile.velocity = Vector2.Zero.MoveTowards(HoverDestinationAboveOwner - Projectile.Center, 30f);
                Projectile.rotation = Projectile.rotation.AngleLerp(idealRotation, 0.3f).AngleTowards(idealRotation, 0.3f);

                // Calculate the telegraph interpolant.
                TelegraphInterpolant = Utils.GetLerpValue(0f, lungeDelay - 6f, Timer, true);

                // Create dust along the telegraph line.
                for (int i = 0; i < 6; i++)
                {
                    Vector2 dustSpawnPosition = Projectile.Center + Projectile.rotation.ToRotationVector2() * Main.rand.NextFloat(TelegraphLength * 0.9f);
                    dustSpawnPosition += (Projectile.rotation + Main.rand.NextBool().ToDirectionInt() * MathHelper.PiOver2).ToRotationVector2() * 30f;

                    Dust rainbowSparkle = Dust.NewDustPerfect(dustSpawnPosition, 267);
                    rainbowSparkle.color = Main.hslToRgb(Main.rand.NextFloat(), 1f, 0.65f);
                    rainbowSparkle.color.A /= 3;
                    rainbowSparkle.velocity = Projectile.rotation.ToRotationVector2() * Main.rand.NextFloat(4f);
                    rainbowSparkle.scale = 0.8f;
                    rainbowSparkle.fadeIn = 0.8f;
                    rainbowSparkle.noGravity = true;
                }
            }

            // Lunge at the target.
            if (Timer == lungeDelay)
            {
                Projectile.oldPos = new Vector2[Projectile.oldPos.Length];
                Projectile.velocity = Projectile.rotation.ToRotationVector2() * lungeSpeed;
                Projectile.netUpdate = true;
            }

            // Define rotation and release lances.
            if (Timer >= lungeDelay)
            {
                Projectile.rotation = Projectile.velocity.ToRotation();

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float offsetAngle = MathHelper.TwoPi * Timer / (EmpressOfLightBehaviorOverride.ShouldBeEnraged ? 40f : 32f);
                    offsetAngle += 1.14f;
                    if (SwordIndex / (SwordCount - 1f) < 0.5f)
                        offsetAngle = -offsetAngle + MathHelper.Pi;

                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(lance =>
                    {
                        lance.MaxUpdates = 1;
                    });
                    Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<EtherealLance>(), EmpressOfLightBehaviorOverride.LanceDamage, 0f, -1, offsetAngle, (Timer - lungeDelay) / 75f % 1f);
                }
            }
        }

        public override bool? CanDamage() => Timer >= AttackDelay ? null : false;

        public Color ColorFunction(float completionRatio)
        {
            float opacity = Utils.GetLerpValue(17.5f, 23f, Projectile.velocity.Length(), true) * Utils.GetLerpValue(20f, 30f, Projectile.timeLeft, true);
            Color rainbow = Main.hslToRgb((completionRatio - Main.GlobalTimeWrappedHourly * 0.7f) % 1f, 1f, 0.5f);
            Color c = Color.Lerp(MyColor, rainbow, completionRatio) * (1f - completionRatio) * opacity;
            c.A = 0;
            return c;
        }

        public static float WidthFunction(float completionRatio)
        {
            float fade = (1f - completionRatio) * Utils.GetLerpValue(-0.03f, 0.1f, completionRatio, true);
            return MathHelper.SmoothStep(0f, 1f, fade) * 20f;
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

                float telegraphHue = (float)Math.Cos(MathHelper.TwoPi * TelegraphInterpolant) * 0.5f + 0.5f;
                float telegraphWidth = MathHelper.Lerp(0.2f, 1.2f, TelegraphInterpolant);
                float telegraphOpacity = (float)Math.Pow(TelegraphInterpolant, 1.7) * 0.7f;
                Vector2 telegraphScale = new(telegraphWidth, TelegraphLength / telegraphTexture.Height);
                Color telegraphColor = Main.hslToRgb(telegraphHue, 1f, 0.8f) * telegraphOpacity;
                Vector2 telegraphOrigin = telegraphTexture.Size() * new Vector2(0.5f, 0f);

                Main.spriteBatch.Draw(telegraphTexture, drawPosition, null, telegraphColor, Projectile.rotation - MathHelper.PiOver2, telegraphOrigin, telegraphScale, 0, 0f);
                Main.spriteBatch.Draw(telegraphTexture, drawPosition, null, Color.White * telegraphOpacity, Projectile.rotation - MathHelper.PiOver2, telegraphOrigin, telegraphScale * new Vector2(0.3f, 1f), 0, 0f);
                Main.spriteBatch.ResetBlendState();
            }

            float opacity = Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true);
            for (int i = 0; i < 6; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 6f;
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
