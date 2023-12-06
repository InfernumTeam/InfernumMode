using CalamityMod;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Primitives;
using InfernumMode.Content.Items.Weapons.Melee;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Melee
{
    public class MyrindaelSpinProjectile : ModProjectile
    {
        public float InitialDirection;

        public PrimitiveTrailCopy PierceAfterimageDrawer;

        public float SpinCompletion => Utils.GetLerpValue(0f, Myrindael.SpinTime, Time, true);

        public Player Owner => Main.player[Projectile.owner];

        public ref float Time => ref Projectile.ai[0];

        public ref float SpinDirection => ref Projectile.ai[1];

        public override string Texture => "InfernumMode/Content/Items/Weapons/Melee/Myrindael";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Myrindael");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 50;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 64;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = Myrindael.SpinTime + 240;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            // Die if no longer holding the left click button or otherwise cannot use the item.
            if ((!Owner.channel || Owner.dead || !Owner.active || Owner.noItems) && SpinCompletion < 1f)
            {
                Projectile.Kill();
                return;
            }

            Time++;
            if (Time == 1f)
            {
                InitialDirection = Projectile.velocity.ToRotation();
                SpinDirection = Owner.direction;
                Projectile.netUpdate = true;
            }

            // Stick to the owner when spinning.
            if (Time < Myrindael.SpinTime)
            {
                Projectile.Center = Owner.MountedCenter;
                Projectile.velocity = Vector2.Zero;
                Projectile.rotation = Pow(SpinCompletion, 1.62f) * Pi * SpinDirection * 6f + InitialDirection - PiOver4 + Pi;

                // Spin the player's front arm.
                Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - Pi + PiOver4);

                AdjustPlayerValues();
                return;
            }

            if (Time == Myrindael.SpinTime)
            {
                // Reset the trail points.
                for (int i = 0; i < Projectile.oldPos.Length; i++)
                    Projectile.oldPos[i] = Vector2.Zero;

                // Play a throw sound.
                SoundEngine.PlaySound(InfernumSoundRegistry.MyrindaelThrowSound, Projectile.Center);

                if (Main.myPlayer == Projectile.owner)
                {
                    Projectile.velocity = Projectile.SafeDirectionTo(Main.MouseWorld) * 24f;
                    Projectile.netUpdate = true;
                }

                // Create a bunch of electricity.
                for (int i = 0; i < 20; i++)
                {
                    Dust electricity = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), 226);
                    electricity.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.34f) * Main.rand.NextFloat(3f, 14f);
                    electricity.noGravity = true;
                }
            }

            // Fade out.
            Projectile.Opacity = Utils.GetLerpValue(0f, 25f, Projectile.timeLeft, true);

            // Rotate based on direction.
            Projectile.rotation = Projectile.velocity.ToRotation();

            // Home in on targets.
            NPC potentialTarget = Projectile.Center.ClosestNPCAt(Myrindael.TargetHomeDistance);
            if (potentialTarget is not null && Projectile.timeLeft > 25)
            {
                float newSpeed = Clamp(Projectile.velocity.Length() * 1.032f, 6f, 42f);

                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(potentialTarget.Center) * newSpeed, 0.24f).RotateTowards(Projectile.AngleTo(potentialTarget.Center), 0.1f);
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * newSpeed;
            }
            if (Projectile.timeLeft <= 25)
                Projectile.velocity = Projectile.velocity.RotatedBy(Pi / 120f) * 0.9f;

            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;
        }

        public void AdjustPlayerValues()
        {
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Time < Myrindael.SpinTime)
                return;

            // Create lightning from the sky.
            SoundEngine.PlaySound(InfernumSoundRegistry.MyrindaelHitSound, Projectile.Center);
            SoundEngine.PlaySound(InfernumSoundRegistry.MyrindaelLightningSound with { Volume = 0.06f }, Projectile.Center);
            if (Main.myPlayer == Projectile.owner)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 lightningSpawnPosition = target.Center + new Vector2(Main.rand.NextFloatDirection() * 30f, -800f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), lightningSpawnPosition, Vector2.UnitY * Main.rand.NextFloat(24f, 33f), ModContent.ProjectileType<MyrindaelLightning>(), Projectile.damage / 2, 0f, Projectile.owner, PiOver2, Main.rand.Next(100));
                }
            }

            Projectile.timeLeft = 25;
        }

        // Release sparks at nearby targets.
        public override void OnKill(int timeLeft)
        {
            if (Time < Myrindael.SpinTime)
                return;

            NPC potentialTarget = Projectile.Center.ClosestNPCAt(Myrindael.TargetHomeDistance);
            if (Main.myPlayer != Projectile.owner || potentialTarget is null)
                return;

            for (int i = 0; i < 2; i++)
            {
                Vector2 sparkVelocity = Projectile.SafeDirectionTo(potentialTarget.Center).RotatedBy(Lerp(-0.44f, 0.44f, i / 2f)) * 9f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, sparkVelocity, ModContent.ProjectileType<MyrindaelSpark>(), Projectile.damage / 3, Projectile.knockBack, Projectile.owner);
            }
        }

        public override Color? GetAlpha(Color lightColor) => lightColor * Projectile.Opacity;

        public float PierceWidthFunction(float completionRatio)
        {
            float width = Projectile.scale * Projectile.Opacity * 30f;
            if (SpinCompletion < 1f)
                width *= 1.35f;

            return width;
        }

        public Color PierceColorFunction(float completionRatio) => Color.Lime * Pow(Utils.GetLerpValue(0f, 0.1f, completionRatio, true), 2.4f) * Projectile.Opacity;

        public void DrawTrail()
        {
            Main.spriteBatch.EnterShaderRegion();

            Color mainColor = CalamityUtils.MulticolorLerp(Main.GlobalTimeWrappedHourly * 2f % 1, Color.Cyan, Color.DeepSkyBlue, Color.Turquoise, Color.Blue);
            Color secondaryColor = CalamityUtils.MulticolorLerp((Main.GlobalTimeWrappedHourly * 2f + 0.2f) % 1, Color.Cyan, Color.DeepSkyBlue, Color.Turquoise, Color.Blue);

            mainColor = Color.Lerp(Color.White, mainColor, Projectile.Opacity * 0.6f + 0.4f);
            secondaryColor = Color.Lerp(Color.White, secondaryColor, Projectile.Opacity * 0.6f + 0.4f);

            // Initialize the trail drawer.
            PierceAfterimageDrawer ??= new(PierceWidthFunction, PierceColorFunction, null, true, GameShaders.Misc["CalamityMod:ExobladePierce"]);

            Vector2 trailOffset = Projectile.Size * 0.5f - Main.screenPosition + (Projectile.rotation - PiOver4).ToRotationVector2() * (SpinCompletion >= 1f ? 58f : 90f);
            GameShaders.Misc["CalamityMod:ExobladePierce"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/EternityStreak"));
            GameShaders.Misc["CalamityMod:ExobladePierce"].UseImage2("Images/Extra_189");
            GameShaders.Misc["CalamityMod:ExobladePierce"].UseColor(mainColor);
            GameShaders.Misc["CalamityMod:ExobladePierce"].UseSecondaryColor(secondaryColor);
            GameShaders.Misc["CalamityMod:ExobladePierce"].Apply();
            PierceAfterimageDrawer.Draw(Projectile.oldPos.Take(12), trailOffset, 53);

            Main.spriteBatch.ExitShaderRegion();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Draw the trail.
            if (SpinCompletion < 1f)
            {
                for (int i = 0; i < (int)Math.Min(Time, 12f); i++)
                {
                    float localRotation = Projectile.oldRot[i];
                    if (i == 0)
                        localRotation = Projectile.rotation + Pi * SpinDirection * 0.075f;

                    Projectile.oldPos[i] = Projectile.position + (localRotation - PiOver4).ToRotationVector2() * 70f - (Projectile.rotation - PiOver4).ToRotationVector2() * 90f;
                }
            }
            else
                DrawTrail();

            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = new(0, texture.Height);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            // Draw a back afterimage.
            Color spearAfterimageColor = new Color(0.73f, 0.93f, 0.96f, 0f) * Projectile.Opacity;
            for (int i = 0; i < 12; i++)
            {
                Vector2 spearOffset = (TwoPi * i / 12f).ToRotationVector2() * (1f - Projectile.Opacity) * 12f;
                Main.EntitySpriteDraw(texture, drawPosition + spearOffset, null, spearAfterimageColor, Projectile.rotation, origin, Projectile.scale, 0, 0);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, origin, Projectile.scale, 0, 0);
            if (SpinCompletion < 1f)
                DrawTrail();
            return false;
        }
    }
}
