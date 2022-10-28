using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Sounds;
using InfernumMode.Items.Weapons.Melee;
using InfernumMode.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Projectiles.Melee
{
    public class MyrindaelSpinProjectile : ModProjectile
    {
        public float InitialDirection;

        public PrimitiveTrail PierceAfterimageDrawer = null;

        public float SpinCompletion => Utils.GetLerpValue(0f, Myrindael.SpinTime, Time, true);

        public Player Owner => Main.player[Projectile.owner];

        public ref float Time => ref Projectile.ai[0];

        public ref float SpinDirection => ref Projectile.ai[1];

        public override string Texture => "InfernumMode/Items/Weapons/Melee/Myrindael";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Myrindael");
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
                SoundEngine.PlaySound(InfernumSoundRegistry.VassalSlashSound, Projectile.Center);

                InitialDirection = Projectile.velocity.ToRotation();
                SpinDirection = Owner.direction;
                Projectile.netUpdate = true;
            }

            // Stick to the owner when spinning.
            if (Time < Myrindael.SpinTime)
            {
                Projectile.Center = Owner.MountedCenter;
                Projectile.velocity = Vector2.Zero;
                Projectile.rotation = (float)Math.Pow(SpinCompletion, 1.62) * MathHelper.Pi * SpinDirection * 6f + InitialDirection - MathHelper.PiOver4 + MathHelper.Pi;

                Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.Pi + MathHelper.PiOver4);
                return;
            }

            if (Main.myPlayer == Projectile.owner && Time == Myrindael.SpinTime)
            {
                Projectile.velocity = Projectile.SafeDirectionTo(Main.MouseWorld) * 15f;
                Projectile.netUpdate = true;
            }

            // Fade out.
            Projectile.Opacity = Utils.GetLerpValue(0f, 25f, Projectile.timeLeft, true);

            // Rotate based on direction.
            Projectile.rotation = Projectile.velocity.ToRotation();

            // Home in on targets.
            NPC potentialTarget = Projectile.Center.ClosestNPCAt(Myrindael.TargetHomeDistance);
            if (potentialTarget is not null && Projectile.timeLeft > 25)
            {
                float newSpeed = MathHelper.Clamp(Projectile.velocity.Length() * 1.032f, 6f, 42f);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(potentialTarget.Center) * newSpeed, 0.18f);
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * newSpeed;
            }
            if (Projectile.timeLeft <= 25)
                Projectile.velocity = Projectile.velocity.RotatedBy(MathHelper.Pi / 120f) * 0.9f;

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            // Create lightning from the sky.
            SoundEngine.PlaySound(CommonCalamitySounds.LargeWeaponFireSound with { Volume = 0.3f }, Projectile.Center);
            if (Main.myPlayer == Projectile.owner)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 lightningSpawnPosition = target.Center + new Vector2(Main.rand.NextFloatDirection() * 30f, -800f);
                    int lightning = Projectile.NewProjectile(Projectile.GetSource_FromThis(), lightningSpawnPosition, Vector2.UnitY * Main.rand.NextFloat(24f, 33f), ModContent.ProjectileType<MyrindaelLightning>(), Projectile.damage / 2, 0f, Projectile.owner);
                    if (Main.projectile.IndexInRange(lightning))
                    {
                        Main.projectile[lightning].ai[0] = Main.projectile[lightning].velocity.ToRotation();
                        Main.projectile[lightning].ai[1] = Main.rand.Next(100);
                    }
                }
            }

            Projectile.timeLeft = 25;
        }

        // Release sparks at nearby targets.
        public override void Kill(int timeLeft)
        {
            NPC potentialTarget = Projectile.Center.ClosestNPCAt(Myrindael.TargetHomeDistance);
            if (Main.myPlayer != Projectile.owner || potentialTarget is null)
                return;

            for (int i = 0; i < 3; i++)
            {
                Vector2 sparkVelocity = Projectile.SafeDirectionTo(potentialTarget.Center).RotatedBy(MathHelper.Lerp(-0.44f, 0.44f, i / 2f)) * 9f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, sparkVelocity, ModContent.ProjectileType<MyrindaelSpark>(), Projectile.damage / 3, Projectile.knockBack, Projectile.owner);
            }
        }

        public override Color? GetAlpha(Color lightColor) => lightColor * Projectile.Opacity;

        public float PierceWidthFunction(float completionRatio)
        {
            float width = Utils.GetLerpValue(0f, 0.1f, completionRatio, true) * Projectile.scale * Projectile.Opacity * 20f;
            return width;
        }

        public Color PierceColorFunction(float completionRatio) => Color.Lime * Projectile.Opacity;

        public void DrawTrail()
        {
            Main.spriteBatch.EnterShaderRegion();

            Color mainColor = CalamityUtils.MulticolorLerp(Main.GlobalTimeWrappedHourly * 2f % 1, Color.Cyan, Color.DeepSkyBlue, Color.Turquoise, Color.Blue);
            Color secondaryColor = CalamityUtils.MulticolorLerp((Main.GlobalTimeWrappedHourly * 2f + 0.2f) % 1, Color.Cyan, Color.DeepSkyBlue, Color.Turquoise, Color.Blue);

            mainColor = Color.Lerp(Color.White, mainColor, Projectile.Opacity * 0.6f + 0.4f);
            secondaryColor = Color.Lerp(Color.White, secondaryColor, Projectile.Opacity * 0.6f + 0.4f);

            // Initialize the trail drawer.
            PierceAfterimageDrawer ??= new(PierceWidthFunction, PierceColorFunction, null, GameShaders.Misc["CalamityMod:ExobladePierce"]);

            Vector2 trailOffset = Projectile.Size * 0.5f - Main.screenPosition + (Projectile.rotation - MathHelper.PiOver4).ToRotationVector2() * 90f;
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
            if (Time >= Myrindael.SpinTime)
                DrawTrail();

            // Draw the spin smear texture.
            else
            {
                Projectile.oldPos = new Vector2[Projectile.oldPos.Length];

                // Draw the spin smear texture.
                if (SpinCompletion is >= 0f and < 1f)
                {
                    Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/SemiCircularSmear").Value;

                    Main.spriteBatch.EnterShaderRegion(BlendState.Additive);

                    float rotation = Projectile.rotation - MathHelper.Pi / 5f + MathHelper.Pi;
                    Color smearColor = Color.Turquoise * CalamityUtils.Convert01To010(SpinCompletion) * 1.3f;
                    Vector2 smearOrigin = smear.Size() * 0.5f;

                    Main.EntitySpriteDraw(smear, Owner.Center - Main.screenPosition, null, smearColor, rotation, smearOrigin, Projectile.scale * 1.2f, 0, 0);
                    Main.spriteBatch.ExitShaderRegion();
                }
            }

            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = new(0, texture.Height);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            // Draw a back afterimage.
            Color spearAfterimageColor = new Color(0.73f, 0.93f, 0.96f, 0f) * Projectile.Opacity;
            for (int i = 0; i < 12; i++)
            {
                Vector2 spearOffset = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * (1f - Projectile.Opacity) * 12f;
                Main.EntitySpriteDraw(texture, drawPosition + spearOffset, null, spearAfterimageColor, Projectile.rotation, origin, Projectile.scale, 0, 0);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, origin, Projectile.scale, 0, 0);
            return false;
        }
    }
}
