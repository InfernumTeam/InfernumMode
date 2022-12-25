using CalamityMod;
using CalamityMod.Events;
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

namespace InfernumMode.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class EmpressSword : ModProjectile
    {
        public int SwordCount;
        public int TotalSwordsThatShouldAttack;
        public float TelegraphInterpolant;
        public PrimitiveTrail TrailDrawer;

        public NPC Owner
        {
            get
            {
                if (!Main.npc.IndexInRange((int)Projectile.ai[0]) || Main.npc[(int)Projectile.ai[0]].type != NPCID.HallowBoss || !Main.npc[(int)Projectile.ai[0]].active)
                    return null;

                if (Main.npc[(int)Projectile.ai[0]].ai[0] != (int)EmpressOfLightBehaviorOverride.EmpressOfLightAttackType.DanceOfSwords)
                    return null;

                return Main.npc[(int)Projectile.ai[0]];
            }
        }

        public Color MyColor
        {
            get
            {
                Color color = Main.hslToRgb(Projectile.ai[1] % 1f, 0.95f, 0.54f);
                if (EmpressOfLightBehaviorOverride.ShouldBeEnraged)
                    color = EmpressOfLightBehaviorOverride.GetDaytimeColor(Projectile.ai[1] % 1f);

                color.A /= 8;
                return color;
            }
        }

        public Vector2 HoverDestinationAboveOwner
        {
            get
            {
                Vector2 hoverDestination = Owner.Top - Vector2.UnitY.RotatedBy(MathHelper.Lerp(-0.94f, 0.94f, SwordIndex / (SwordCount - 1f))) * new Vector2(200f, 100f);
                hoverDestination.Y += (float)Math.Sin(MathHelper.TwoPi * Timer / 60f + MathHelper.PiOver2 * SwordIndex / SwordCount) * 24f - 40f;
                return hoverDestination;
            }
        }

        public Player Target => Main.player[Owner.target];

        public float Timer => Owner is null ? -AttackDelay : Owner.Infernum().ExtraAI[0] - AttackDelay;

        public bool ShouldAttack
        {
            get
            {
                if (Timer <= 0f || Projectile.timeLeft <= 30)
                    return false;

                int attackCycle = (int)(Timer / AttackTimePerSword);
                float cycleCompletion = (attackCycle + SwordIndex) / SwordCount % 1f;
                float swordRatio = 1f / TotalSwordsThatShouldAttack;
                return cycleCompletion % swordRatio < 0.01f;
            }
        }

        public ref float AttackTimePerSword => ref Projectile.localAI[0];

        public ref float SwordIndex => ref Projectile.localAI[1];

        public const int AttackDelay = 45;

        public const float TelegraphWidth = 3600f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Empress Blade");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 24;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 94;
            Projectile.height = 30;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 900;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(SwordCount);
            writer.Write(AttackTimePerSword);
            writer.Write(SwordIndex);
            writer.Write(TelegraphInterpolant);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            SwordCount = reader.ReadInt32();
            AttackTimePerSword = reader.ReadSingle();
            SwordIndex = reader.ReadSingle();
            TelegraphInterpolant = reader.ReadSingle();
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

            if (!ShouldAttack)
            {
                HoverAboveOwner();
                return;
            }
            AttackTarget();
        }

        public void HoverAboveOwner()
        {
            float idealRotation = -(Owner.Center - HoverDestinationAboveOwner).ToRotation();
            float hoverSpeed = MathHelper.Lerp(25f, 65f, Utils.GetLerpValue(100f, 750f, Projectile.Distance(HoverDestinationAboveOwner)));

            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Vector2.Zero.MoveTowards(HoverDestinationAboveOwner - Projectile.Center, hoverSpeed), 0.1f);
            Projectile.rotation = Projectile.rotation.AngleLerp(idealRotation, 0.03f);
        }

        public void AttackTarget()
        {
            int lungeDelay = 35;
            float lungeSpeed = 24.25f;
            float lungeAcceleration = 1.01f;
            float wrappedAttackTimer = Timer % AttackTimePerSword;

            if (BossRushEvent.BossRushActive)
            {
                lungeSpeed += 10f;
                lungeAcceleration *= 1.25f;
            }

            // Aim at the target in anticipation of a lunge.
            if (wrappedAttackTimer < lungeDelay)
            {
                float idealRotation = Projectile.AngleTo(Target.Center + Target.velocity * 27f);
                Projectile.velocity = Vector2.Zero.MoveTowards(HoverDestinationAboveOwner - Projectile.Center, 30f);
                Projectile.rotation = Projectile.rotation.AngleLerp(idealRotation, 0.15f).AngleTowards(idealRotation, 0.15f);

                // Calculate the telegraph interpolant.
                TelegraphInterpolant = Utils.GetLerpValue(0f, lungeDelay - 6f, wrappedAttackTimer, true);

                // Create dust along the telegraph line.
                for (int i = 0; i < 6; i++)
                {
                    Vector2 dustSpawnPosition = Projectile.Center + Projectile.rotation.ToRotationVector2() * Main.rand.NextFloat(TelegraphWidth * 0.9f);
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
            if (wrappedAttackTimer == lungeDelay)
            {
                SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown, Projectile.Center);
                SoundEngine.PlaySound(SoundID.Item28, Projectile.Center);
                Projectile.oldPos = new Vector2[Projectile.oldPos.Length];
                Projectile.velocity = Projectile.rotation.ToRotationVector2() * lungeSpeed;
                Projectile.netUpdate = true;
            }

            // Define rotation.
            if (wrappedAttackTimer >= lungeDelay)
                Projectile.rotation = Projectile.velocity.ToRotation();

            // Accelerate after lunging.
            if (wrappedAttackTimer > lungeDelay && wrappedAttackTimer <= AttackTimePerSword * 0.5f)
                Projectile.velocity *= lungeAcceleration;

            // Arc towards the target after lunging for a sufficient amount of time.
            if (wrappedAttackTimer > AttackTimePerSword * 0.5f)
            {
                float angularTurnSpeed = 0.092f;

                // Rotate back towards the target if sufficiently far away from them.
                if (!Projectile.WithinRange(Target.Center, 270f))
                    Projectile.velocity = Projectile.velocity.RotateTowards(Projectile.AngleTo(Target.Center), angularTurnSpeed);

                // Slow down if very fast.
                if (Projectile.velocity.Length() > 20f)
                    Projectile.velocity *= 0.95f;

                // If not super close to the target but the target is very much in the line of sight of the summon, charge.
                if (!Projectile.WithinRange(Target.Center, 150f) && Projectile.velocity.AngleBetween(Projectile.SafeDirectionTo(Target.Center)) < 0.21f && Projectile.velocity.Length() < 23f)
                {
                    Projectile.velocity = Projectile.SafeDirectionTo(Target.Center) * lungeSpeed;
                    Projectile.netUpdate = true;
                }
            }
        }

        public override bool? CanDamage() => ShouldAttack ? null : false;

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
            if (TrailDrawer is null)
                TrailDrawer = new PrimitiveTrail(WidthFunction, ColorFunction, specialShader: GameShaders.Misc["Infernum:PrismaticRay"]);

            GameShaders.Misc["Infernum:PrismaticRay"].UseOpacity(0.2f);
            GameShaders.Misc["Infernum:PrismaticRay"].UseImage1("Images/Misc/Perlin");
            Main.instance.GraphicsDevice.Textures[2] = InfernumTextureRegistry.StreakSolid.Value;

            if (TelegraphInterpolant > 0f)
            {
                Main.spriteBatch.SetBlendState(BlendState.Additive);

                float telegraphHue = (float)Math.Cos(MathHelper.TwoPi * TelegraphInterpolant) * 0.5f + 0.5f;
                float telegraphWidth = MathHelper.Lerp(0.2f, 1.2f, TelegraphInterpolant);
                float telegraphOpacity = (float)Math.Pow(TelegraphInterpolant, 1.7) * 0.7f;
                Vector2 telegraphScale = new(telegraphWidth, TelegraphWidth / telegraphTexture.Height);
                Color telegraphColor = Main.hslToRgb(telegraphHue, 1f, 0.8f) * telegraphOpacity;
                Vector2 telegraphOrigin = telegraphTexture.Size() * new Vector2(0.5f, 0f);

                Main.spriteBatch.Draw(telegraphTexture, drawPosition, null, telegraphColor, Projectile.rotation - MathHelper.PiOver2, telegraphOrigin, telegraphScale, 0, 0f);
                Main.spriteBatch.Draw(telegraphTexture, drawPosition, null, Color.White * telegraphOpacity, Projectile.rotation - MathHelper.PiOver2, telegraphOrigin, telegraphScale * new Vector2(0.3f, 1f), 0, 0f);
                Main.spriteBatch.ResetBlendState();
            }

            // Draw the afterimage trail.
            Main.spriteBatch.EnterShaderRegion();

            Vector2 afterimageOffset = Projectile.Size * 0.5f - Projectile.rotation.ToRotationVector2() * Projectile.width * 0.5f;
            TrailDrawer.Draw(Projectile.oldPos, afterimageOffset - Main.screenPosition, 67);
            Main.spriteBatch.ExitShaderRegion();

            float opacity = Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true);
            for (int i = 0; i < 6; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 6f;
                Main.spriteBatch.Draw(texture, drawPosition + drawOffset, null, MyColor * opacity * 0.3f, Projectile.rotation, origin, Projectile.scale, 0, 0f);
            }
            Main.spriteBatch.Draw(texture, drawPosition, null, new Color(opacity, opacity, opacity, 0f), Projectile.rotation, origin, Projectile.scale, 0, 0f);

            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Utilities.RotatingHitboxCollision(Projectile, targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.rotation.ToRotationVector2());
        }
    }
}
