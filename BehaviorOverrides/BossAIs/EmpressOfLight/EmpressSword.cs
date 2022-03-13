using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Linq;
using Terraria;
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
                if (!Main.npc.IndexInRange((int)projectile.ai[0]) || Main.npc[(int)projectile.ai[0]].type != ModContent.NPCType<EmpressOfLightNPC>() || !Main.npc[(int)projectile.ai[0]].active)
                    return null;

                return Main.npc[(int)projectile.ai[0]];
            }
        }

        public Color MyColor
        {
            get
            {
                Color color = Main.hslToRgb(projectile.ai[1] % 1f, 0.95f, 0.54f);
                if (EmpressOfLightNPC.ShouldBeEnraged)
                    color = Main.OurFavoriteColor * 1.35f;

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
                if (Timer <= 0f || projectile.timeLeft <= 30)
                    return false;

                int attackCycle = (int)(Timer / AttackTimePerSword);
                float cycleCompletion = ((attackCycle + SwordIndex) / (float)SwordCount) % 1f;
                float swordRatio = 1f / TotalSwordsThatShouldAttack;
                return cycleCompletion % swordRatio < 0.01f;
            }
        }

        public ref float AttackTimePerSword => ref projectile.localAI[0];

        public ref float SwordIndex => ref projectile.localAI[1];

        public const int AttackDelay = 45;

        public const float TelegraphWidth = 3600f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Empress Blade");
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 24;
        }

        public override void SetDefaults()
        {
            projectile.width = 94;
            projectile.height = 30;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.timeLeft = 900;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.netImportant = true;
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
                projectile.Kill();
                return;
            }

            // Reset things.
            TelegraphInterpolant = 0f;

            if (!ShouldAttack)
            {
                HoveAboveOwner();
                return;
            }
            AttackTarget();
        }

        public void HoveAboveOwner()
        {
            float idealRotation = -(Owner.Center - HoverDestinationAboveOwner).ToRotation();
            float hoverSpeed = MathHelper.Lerp(25f, 65f, Utils.InverseLerp(100f, 750f, projectile.Distance(HoverDestinationAboveOwner)));

            projectile.velocity = Vector2.Lerp(projectile.velocity, Vector2.Zero.MoveTowards(HoverDestinationAboveOwner - projectile.Center, hoverSpeed), 0.1f);
            projectile.rotation = projectile.rotation.AngleLerp(idealRotation, 0.06f);
        }

        public void AttackTarget()
        {
            int lungeDelay = 35;
            float lungeSpeed = 30f;
            float lungeAcceleration = 1.01f;
            float wrappedAttackTimer = Timer % AttackTimePerSword;

            // Aim at the target in anticipation of a lunge.
            if (wrappedAttackTimer < lungeDelay)
            {
                float idealRotation = projectile.AngleTo(Target.Center + Target.velocity * 20f);
                projectile.velocity = Vector2.Zero.MoveTowards(HoverDestinationAboveOwner - projectile.Center, 30f);
                projectile.rotation = projectile.rotation.AngleLerp(idealRotation, 0.15f).AngleTowards(idealRotation, 0.15f);

                // Calculate the telegraph interpolant.
                TelegraphInterpolant = Utils.InverseLerp(0f, lungeDelay - 6f, wrappedAttackTimer, true);

                // Create dust along the telegraph line.
                for (int i = 0; i < 6; i++)
                {
                    Vector2 dustSpawnPosition = projectile.Center + projectile.rotation.ToRotationVector2() * Main.rand.NextFloat(TelegraphWidth * 0.9f);
                    dustSpawnPosition += (projectile.rotation + Main.rand.NextBool().ToDirectionInt() * MathHelper.PiOver2).ToRotationVector2() * 30f;

                    Dust rainbowSparkle = Dust.NewDustPerfect(dustSpawnPosition, 267);
                    rainbowSparkle.color = Main.hslToRgb(Main.rand.NextFloat(), 1f, 0.65f);
                    rainbowSparkle.color.A /= 3;
                    rainbowSparkle.velocity = projectile.rotation.ToRotationVector2() * Main.rand.NextFloat(4f);
                    rainbowSparkle.scale = 0.8f;
                    rainbowSparkle.fadeIn = 0.8f;
                    rainbowSparkle.noGravity = true;
                }
            }

            // Lunge at the target.
            if (wrappedAttackTimer == lungeDelay)
            {
                Main.PlaySound(SoundID.DD2_WyvernDiveDown, projectile.Center);
                Main.PlaySound(SoundID.Item28, projectile.Center);
                projectile.oldPos = new Vector2[projectile.oldPos.Length];
                projectile.velocity = projectile.rotation.ToRotationVector2() * lungeSpeed;
                projectile.netUpdate = true;
            }

            // Define rotation.
            if (wrappedAttackTimer >= lungeDelay)
                projectile.rotation = projectile.velocity.ToRotation();

            // Accelerate after lunging.
            if (wrappedAttackTimer > lungeDelay && wrappedAttackTimer <= AttackTimePerSword * 0.5f)
                projectile.velocity *= lungeAcceleration;

            // Arc towards the target after lunging for a sufficient amount of time.
            if (wrappedAttackTimer > AttackTimePerSword * 0.5f)
            {
                float angularTurnSpeed = 0.12f;

                // Rotate back towards the target if sufficiently far away from them.
                if (!projectile.WithinRange(Target.Center, 270f))
                    projectile.velocity = projectile.velocity.RotateTowards(projectile.AngleTo(Target.Center), angularTurnSpeed);

                // Slow down if very fast.
                if (projectile.velocity.Length() > 20f)
                    projectile.velocity *= 0.95f;

                // If not super close to the target but the target is very much in the line of sight of the summon, charge.
                if (!projectile.WithinRange(Target.Center, 150f) && projectile.velocity.AngleBetween(projectile.SafeDirectionTo(Target.Center)) < 0.21f && projectile.velocity.Length() < 23f)
                {
                    projectile.velocity = projectile.SafeDirectionTo(Target.Center) * lungeSpeed;
                    projectile.netUpdate = true;
                }
            }
        }

        public override bool CanDamage() => ShouldAttack;

        public Color ColorFunction(float completionRatio)
        {
            float opacity = Utils.InverseLerp(17.5f, 23f, projectile.velocity.Length(), true) * Utils.InverseLerp(20f, 30f, projectile.timeLeft, true);
            Color rainbow = Main.hslToRgb((completionRatio - Main.GlobalTime * 0.7f) % 1f, 1f, 0.5f);
            Color c = Color.Lerp(MyColor, rainbow, completionRatio) * (1f - completionRatio) * opacity;
            c.A = 0;
            return c;
        }

        public float WidthFunction(float completionRatio)
        {
            float fade = (1f - completionRatio) * Utils.InverseLerp(-0.03f, 0.1f, completionRatio, true);
            return MathHelper.SmoothStep(0f, 1f, fade) * 20f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Texture2D texture = Main.projectileTexture[projectile.type];
            Texture2D telegraphTexture = ModContent.GetTexture("InfernumMode/ExtraTextures/BloomLine");
            Vector2 origin = texture.Size() * 0.5f;
            if (TrailDrawer is null)
                TrailDrawer = new PrimitiveTrail(WidthFunction, ColorFunction, specialShader: GameShaders.Misc["Infernum:PrismaticRay"]);

            GameShaders.Misc["Infernum:PrismaticRay"].UseOpacity(0.2f);
            GameShaders.Misc["Infernum:PrismaticRay"].UseImage("Images/Misc/Perlin");
            Main.instance.GraphicsDevice.Textures[2] = ModContent.GetTexture("InfernumMode/ExtraTextures/PrismaticLaserbeamStreak");

            if (TelegraphInterpolant > 0f)
            {
                spriteBatch.SetBlendState(BlendState.Additive);

                float telegraphHue = (float)Math.Cos(MathHelper.TwoPi * TelegraphInterpolant) * 0.5f + 0.5f;
                float telegraphWidth = MathHelper.Lerp(0.2f, 1.2f, TelegraphInterpolant);
                float telegraphOpacity = (float)Math.Pow(TelegraphInterpolant, 1.7);
                Vector2 telegraphScale = new Vector2(telegraphWidth, TelegraphWidth / telegraphTexture.Height);
                Color telegraphColor = Main.hslToRgb(telegraphHue, 1f, 0.8f) * telegraphOpacity;
                Vector2 telegraphOrigin = telegraphTexture.Size() * new Vector2(0.5f, 0f);

                spriteBatch.Draw(telegraphTexture, drawPosition, null, telegraphColor, projectile.rotation - MathHelper.PiOver2, telegraphOrigin, telegraphScale, 0, 0f);
                spriteBatch.Draw(telegraphTexture, drawPosition, null, Color.White * telegraphOpacity, projectile.rotation - MathHelper.PiOver2, telegraphOrigin, telegraphScale * new Vector2(0.3f, 1f), 0, 0f);
                spriteBatch.ResetBlendState();
            }

            // Draw the afterimage trail.
            spriteBatch.EnterShaderRegion();

            Vector2 afterimageOffset = projectile.Size * 0.5f - projectile.rotation.ToRotationVector2() * projectile.width * 0.5f;
            TrailDrawer.Draw(projectile.oldPos, afterimageOffset - Main.screenPosition, 67);
            spriteBatch.ExitShaderRegion();

            float opacity = Utils.InverseLerp(0f, 30f, projectile.timeLeft, true);
            for (int i = 0; i < 6; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 6f;
                spriteBatch.Draw(texture, drawPosition + drawOffset, null, MyColor * opacity * 0.3f, projectile.rotation, origin, projectile.scale, 0, 0f);
            }
            spriteBatch.Draw(texture, drawPosition, null, new Color(opacity, opacity, opacity, 0f), projectile.rotation, origin, projectile.scale, 0, 0f);

            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Utilities.RotatingHitboxCollision(projectile, targetHitbox.TopLeft(), targetHitbox.Size(), projectile.rotation.ToRotationVector2());
        }
    }
}
